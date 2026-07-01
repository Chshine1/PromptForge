using System.Collections.Immutable;
using System.Reflection;
using PromptForge.Abstractions;
using PromptForge.Abstractions.Attributes;
using PromptForge.Abstractions.Attributes.Hints;
using PromptForge.Abstractions.Attributes.Types;
using PromptForge.Abstractions.Model;

namespace PromptForge.Core;

public class TypeMetadataBuilder
{
    public readonly Dictionary<Type, ITypeDefinition> ClrToTypeDefinitions = new();

    private static readonly HashSet<Type> numericTypes =
    [
        typeof(byte), typeof(sbyte), typeof(short), typeof(ushort),
        typeof(int), typeof(uint), typeof(long), typeof(ulong),
        typeof(float), typeof(double), typeof(decimal)
    ];

    private static bool IsNumeric(Type type) => numericTypes.Contains(type);

    private static SimpleType GetSimpleType(Type type)
    {
        if (type == typeof(string)) return new SimpleType(type, "string");
        if (type == typeof(bool)) return new SimpleType(type, "boolean");
        return IsNumeric(type) ? new SimpleType(type, "number") : new SimpleType(type, type.Name);
    }

    public ITypeDefinition FromClrType(Type clrType)
    {
        if (ClrToTypeDefinitions.TryGetValue(clrType, out var definition)) return definition;

        if (clrType.IsPrimitive || clrType == typeof(string) || clrType == typeof(decimal) ||
            clrType == typeof(DateTime) ||
            clrType == typeof(Guid))
        {
            var td = GetSimpleType(clrType);
            ClrToTypeDefinitions[clrType] = td;
            return td;
        }

        var typeHints = HintAttributes.CollectFromType(clrType);
        var hint = typeHints.BuildHint(HintTarget.TypeAndProperty);

        var interfaces = (clrType.IsInterface ? clrType.GetInterfaces().Concat([clrType]) : clrType.GetInterfaces())
            .Where(i =>
                i.IsGenericType
                && (i.GetGenericTypeDefinition() == typeof(IDictionary<,>)
                    || i.GetGenericTypeDefinition() == typeof(IReadOnlyDictionary<,>)
                    || i.GetGenericTypeDefinition() == typeof(IEnumerable<>)))
            .ToImmutableArray();

        var dictInterface = interfaces.FirstOrDefault(i =>
            i.GetGenericTypeDefinition() == typeof(IDictionary<,>) ||
            i.GetGenericTypeDefinition() == typeof(IReadOnlyDictionary<,>));

        if (dictInterface is not null)
        {
            var args = dictInterface.GetGenericArguments();
            if (args.Length != 2 || args[0] != typeof(string)) throw new InvalidOperationException("");

            var valueType = FromClrType(args[1]);
            var valueHint = typeHints.BuildHint(HintTarget.MapValue);

            var td = new MapType(clrType, valueType, valueHint);
            ClrToTypeDefinitions[clrType] = td;
            return td;
        }

        var enumInterface = interfaces.FirstOrDefault(i =>
            i.GetGenericTypeDefinition() == typeof(IEnumerable<>));

        if (enumInterface is not null)
        {
            var args = enumInterface.GetGenericArguments();
            if (args.Length != 1) throw new InvalidOperationException();

            var elementType = FromClrType(args[0]);
            var elementHint = typeHints.BuildHint(HintTarget.ArrayElement);

            var td = new ArrayType(clrType, elementType, elementHint);
            ClrToTypeDefinitions[clrType] = td;
            return td;
        }

        var objectAttr = clrType.GetCustomAttribute<ObjectTypeAttribute>();
        if (objectAttr is not null)
        {
            var properties = clrType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.CanRead)
                .Where(p => p.GetCustomAttribute<PromptIgnoreAttribute>() == null)
                .Select(p =>
                {
                    var propHints = HintAttributes.CollectFromMember(p);
                    var propHint = propHints.BuildHint(HintTarget.TypeAndProperty);
                    var propTypeDef = FromClrType(p.PropertyType);

                    switch (propTypeDef)
                    {
                        case MapType mapType:
                            mapType.ValueHint = propHints.BuildHint(HintTarget.MapValue);
                            break;
                        case ArrayType arrayType:
                            arrayType.ElementHint = propHints.BuildHint(HintTarget.ArrayElement);
                            break;
                    }

                    return new PropertyDefinition(p.Name, propTypeDef, propHint);
                })
                .ToImmutableArray();

            return new ObjectType(clrType, objectAttr.Name ?? clrType.Name, properties, hint);
        }

        var simpleTypeAttr = clrType.GetCustomAttribute<SimpleTypeAttribute>();

        var std = simpleTypeAttr is not null
            ? new SimpleType(clrType, simpleTypeAttr.Name ?? clrType.Name)
            : throw new InvalidOperationException();
        ClrToTypeDefinitions[clrType] = std;
        return std;
    }

    private readonly struct HintAttributes
    {
        private readonly ImmutableArray<InputHintAttribute> _inputHints;
        private readonly ImmutableArray<OutputHintAttribute> _outputHints;
        private readonly ImmutableArray<FormatHintAttribute> _formatHints;

        private HintAttributes(
            ImmutableArray<InputHintAttribute> inputHints,
            ImmutableArray<OutputHintAttribute> outputHints,
            ImmutableArray<FormatHintAttribute> formatHints)
        {
            _inputHints = inputHints;
            _outputHints = outputHints;
            _formatHints = formatHints;
        }

        public static HintAttributes CollectFromType(Type type) => new(
            [..type.GetCustomAttributes<InputHintAttribute>()],
            [..type.GetCustomAttributes<OutputHintAttribute>()],
            [..type.GetCustomAttributes<FormatHintAttribute>()]);

        public static HintAttributes CollectFromMember(MemberInfo member) => new(
            [..member.GetCustomAttributes<InputHintAttribute>()],
            [..member.GetCustomAttributes<OutputHintAttribute>()],
            [..member.GetCustomAttributes<FormatHintAttribute>()]);

        public PromptHint? BuildHint(HintTarget target)
        {
            var input = _inputHints.FirstOrDefault(a => a.Target == target);
            var output = _outputHints.FirstOrDefault(a => a.Target == target);
            var format = _formatHints.FirstOrDefault(a => a.Target == target);

            PromptHint? hint = null;
            if (input is not null)
                hint = new PromptHint(semantic: input.Semantic);
            if (output is not null)
            {
                if (hint is null) hint = new PromptHint(purpose: output.Purpose, constraint: output.Constraint);
                else
                {
                    hint.Purpose = output.Purpose;
                    hint.Constraint = output.Constraint;
                }
            }

            if (format is null) return hint;

            if (hint is null) hint = new PromptHint(format: format.Format);
            else
            {
                hint.Format = format.Format;
            }

            return hint;
        }
    }
}
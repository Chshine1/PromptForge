using System.Collections.Immutable;
using System.Reflection;
using PromptForge.Core.Model;

namespace PromptForge.Core;

public static class TypeMetadataBuilder
{
    private static readonly HashSet<Type> numericTypes =
    [
        typeof(byte), typeof(sbyte), typeof(short), typeof(ushort),
        typeof(int), typeof(uint), typeof(long), typeof(ulong),
        typeof(float), typeof(double), typeof(decimal)
    ];

    private static bool IsNumeric(Type type) => numericTypes.Contains(type);

    private static SimpleType GetSimpleType(Type type)
    {
        if (type == typeof(string)) return new SimpleType("string");
        if (type == typeof(bool)) return new SimpleType("boolean");
        return IsNumeric(type) ? new SimpleType("number") : new SimpleType(type.Name);
    }

    public static ITypeDefinition FromType(Type type)
    {
        if (type.IsPrimitive || type == typeof(string) || type == typeof(decimal) || type == typeof(DateTime) ||
            type == typeof(Guid))
            return GetSimpleType(type);

        var typeHints = HintAttributes.CollectFromType(type);
        var hint = typeHints.BuildHint(HintTarget.TypeAndProperty);

        var interfaces = (type.IsInterface ? type.GetInterfaces().Concat([type]) : type.GetInterfaces())
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

            var valueType = FromType(args[1]);
            var valueHint = typeHints.BuildHint(HintTarget.MapValue);
            return new MapType(valueType, valueHint);
        }

        var enumInterface = interfaces.FirstOrDefault(i =>
            i.GetGenericTypeDefinition() == typeof(IEnumerable<>));

        if (enumInterface is not null)
        {
            var args = enumInterface.GetGenericArguments();
            if (args.Length != 1) throw new InvalidOperationException();

            var elementType = FromType(args[0]);
            var elementHint = typeHints.BuildHint(HintTarget.ArrayElement);
            return new ArrayType(elementType, elementHint);
        }

        var objectAttr = type.GetCustomAttribute<ObjectTypeAttribute>();
        if (objectAttr is not null)
        {
            var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.CanRead)
                .Where(p => p.GetCustomAttribute<PromptIgnoreAttribute>() == null)
                .Select(p =>
                {
                    var propHints = HintAttributes.CollectFromMember(p);
                    var propHint = propHints.BuildHint(HintTarget.TypeAndProperty);
                    var propTypeDef = FromType(p.PropertyType);

                    propTypeDef = propTypeDef switch
                    {
                        MapType mapType => mapType with
                        {
                            ValueHint = propHints.BuildHint(HintTarget.MapValue)
                        },
                        ArrayType arrayType => arrayType with
                        {
                            ElementHint = propHints.BuildHint(HintTarget.ArrayElement)
                        },
                        _ => propTypeDef
                    };

                    return new PropertyDefinition(p.Name, propTypeDef, propHint);
                })
                .ToImmutableArray();

            return new ObjectType(objectAttr.Name ?? type.Name, properties, hint);
        }

        var simpleTypeAttr = type.GetCustomAttribute<SimpleTypeAttribute>();
        return simpleTypeAttr is not null
            ? new SimpleType(simpleTypeAttr.Name ?? type.Name)
            : throw new InvalidOperationException();
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
                hint = new PromptHint(Semantic: input.Semantic);
            if (output is not null)
                hint = hint is null
                    ? new PromptHint(Purpose: output.Purpose, Constraint: output.Constraint)
                    : hint with { Purpose = output.Purpose, Constraint = output.Constraint };
            if (format is not null)
                hint = hint is null
                    ? new PromptHint(Format: format.Format)
                    : hint with { Format = format.Format };
            return hint;
        }
    }
}
using System.Collections.Immutable;
using System.Reflection;
using PromptForge.Abstractions;
using PromptForge.Abstractions.Attributes;
using PromptForge.Abstractions.Attributes.Hints;
using PromptForge.Abstractions.Attributes.Types;
using PromptForge.Abstractions.Model;

namespace PromptForge.Core;

public readonly struct RegisterResult
{
    public required ITypeDefinition Type { get; init; }
    public required ImmutableHashSet<Type> TypeOccurrences { get; init; }
}

public static class TypeMetadataBuilder
{
    private static readonly Dictionary<Type, RegisterResult> clrToTypeDefinitions = new();

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

    public static ITypeDefinition GetDefinitionFromClrType(Type clrType)
    {
        return clrToTypeDefinitions[clrType].Type;
    }

    public static RegisterResult RegisterClrType(Type clrType)
    {
        if (clrToTypeDefinitions.TryGetValue(clrType, out var definition)) return definition;

        if (clrType.IsPrimitive || clrType == typeof(string) || clrType == typeof(decimal) ||
            clrType == typeof(DateTime) ||
            clrType == typeof(Guid))
        {
            var result = new RegisterResult
            {
                Type = GetSimpleType(clrType),
                TypeOccurrences = [clrType]
            };
            return clrToTypeDefinitions[clrType] = result;
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

            var occurrences = RegisterClrType(args[1]).TypeOccurrences;
            var valueHint = typeHints.BuildHint(HintTarget.MapValue);

            var result = new RegisterResult
            {
                Type = new MapType(clrType, args[1], valueHint),
                TypeOccurrences = occurrences.Append(clrType).ToImmutableHashSet()
            };
            return clrToTypeDefinitions[clrType] = result;
        }

        var enumInterface = interfaces.FirstOrDefault(i =>
            i.GetGenericTypeDefinition() == typeof(IEnumerable<>));

        if (enumInterface is not null)
        {
            var args = enumInterface.GetGenericArguments();
            if (args.Length != 1) throw new InvalidOperationException();

            var occurrences = RegisterClrType(args[0]).TypeOccurrences;
            var elementHint = typeHints.BuildHint(HintTarget.ArrayElement);

            var result = new RegisterResult
            {
                Type = new ArrayType(clrType, args[0], elementHint),
                TypeOccurrences = occurrences.Append(clrType).ToImmutableHashSet()
            };
            return clrToTypeDefinitions[clrType] = result;
        }

        var objectAttr = clrType.GetCustomAttribute<ObjectTypeAttribute>();
        if (objectAttr is not null)
        {
            List<Type> occurrences = [clrType];
            var properties = clrType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.CanRead)
                .Where(p => p.GetCustomAttribute<PromptIgnoreAttribute>() == null)
                .Select(p =>
                {
                    var propHints = HintAttributes.CollectFromMember(p);
                    var propHint = propHints.BuildHint(HintTarget.TypeAndProperty);
                    var propTypeDef = RegisterClrType(p.PropertyType);
                    occurrences.AddRange(propTypeDef.TypeOccurrences);

                    switch (propTypeDef.Type)
                    {
                        case MapType mapType:
                            // TODO: mapType.ValueHint = propHints.BuildHint(HintTarget.MapValue);
                            break;
                        case ArrayType arrayType:
                            // TODO: arrayType.ElementHint = propHints.BuildHint(HintTarget.ArrayElement);
                            break;
                    }

                    return new PropertyDefinition(p.Name, p.PropertyType, propHint);
                })
                .ToImmutableArray();

            var result = new RegisterResult
            {
                Type = new ObjectType(clrType, objectAttr.Name ?? clrType.Name, properties, hint),
                TypeOccurrences = occurrences.ToImmutableHashSet()
            };
            return clrToTypeDefinitions[clrType] = result;
        }

        var simpleTypeAttr = clrType.GetCustomAttribute<SimpleTypeAttribute>();

        var final = new RegisterResult
        {
            Type = simpleTypeAttr is not null
                ? new SimpleType(clrType, simpleTypeAttr.Name ?? clrType.Name)
                : throw new InvalidOperationException(),
            TypeOccurrences = [clrType]
        };
        return clrToTypeDefinitions[clrType] = final;
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
            {
                if (hint is null) hint = new PromptHint(Purpose: output.Purpose, Constraint: output.Constraint);
                else
                {
                    hint = hint with
                    {
                        Purpose = output.Purpose,
                        Constraint = output.Constraint
                    };
                }
            }

            if (format is null) return hint;

            if (hint is null) hint = new PromptHint(Format: format.Format);
            else
            {
                hint = hint with
                {
                    Format = format.Format
                };
            }

            return hint;
        }
    }
}
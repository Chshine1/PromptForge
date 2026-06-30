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

        var inputHintAttribute = type.GetCustomAttribute<InputHintAttribute>();
        var outputHintAttribute = type.GetCustomAttribute<OutputHintAttribute>();
        var formatHintAttribute = type.GetCustomAttribute<FormatHintAttribute>();

        var hint = BuildHint(inputHintAttribute, outputHintAttribute, formatHintAttribute);

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
            return new MapType(valueType);
        }

        var enumInterface = interfaces.FirstOrDefault(i =>
            i.GetGenericTypeDefinition() == typeof(IEnumerable<>));

        if (enumInterface is not null)
        {
            var args = enumInterface.GetGenericArguments();
            return args.Length != 1 ? throw new InvalidOperationException() : new ArrayType(FromType(args[0]));
        }

        var objectAttr = type.GetCustomAttribute<ObjectTypeAttribute>();
        if (objectAttr is not null)
        {
            var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.CanRead)
                .Where(p => p.GetCustomAttribute<PropertyIgnoreAttribute>() == null)
                .Select(p =>
                {
                    var propInputHintAttribute = type.GetCustomAttribute<InputHintAttribute>();
                    var propOutputHintAttribute = type.GetCustomAttribute<OutputHintAttribute>();
                    var propFormatHintAttribute = type.GetCustomAttribute<FormatHintAttribute>();

                    var propHint = BuildHint(propInputHintAttribute, propOutputHintAttribute, propFormatHintAttribute);
                    var propTypeDef = FromType(p.PropertyType);
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

    private static PromptHint? BuildHint(InputHintAttribute? inputHintAttribute,
        OutputHintAttribute? outputHintAttribute, FormatHintAttribute? formatHintAttribute)
    {
        PromptHint? hint = null;
        if (inputHintAttribute is not null)
        {
            hint = new PromptHint(Semantic: inputHintAttribute.Semantic);
        }

        if (outputHintAttribute is not null)
        {
            hint = hint is null
                ? new PromptHint(Purpose: outputHintAttribute.Purpose, Constraint: outputHintAttribute.Constraint)
                : hint with { Purpose = outputHintAttribute.Purpose, Constraint = outputHintAttribute.Constraint };
        }

        if (formatHintAttribute is not null)
        {
            hint = hint is null
                ? new PromptHint(Format: formatHintAttribute.Format)
                : hint with { Format = formatHintAttribute.Format };
        }

        return hint;
    }
}
using System.Reflection;
using PromptForge.Abstractions.Attributes.Types;
using PromptForge.Abstractions.Model;

namespace PromptForge.Core.Metadata.Registry;

public static partial class TypeMetadataRegistry
{
    private static readonly HashSet<Type> numericTypes =
    [
        typeof(byte), typeof(sbyte), typeof(short), typeof(ushort),
        typeof(int), typeof(uint), typeof(long), typeof(ulong),
        typeof(float), typeof(double), typeof(decimal)
    ];

    private static bool TryBuildSimpleResult(
        Type clrType,
        PromptHint? typeHint,
        out RegisterResult result)
    {
        var simpleTypeAttr = clrType.GetCustomAttribute<SimpleTypeAttribute>();
        if (simpleTypeAttr is not null)
        {
            result = new RegisterResult
            {
                Type = new SimpleType(simpleTypeAttr.Name ?? clrType.Name, typeHint),
                TypeOccurrences = [clrType]
            };
            return true;
        }

        if (IsPrimitiveLike(clrType))
        {
            result = new RegisterResult
            {
                Type = GetSimpleType(clrType, typeHint),
                TypeOccurrences = [clrType]
            };
            return true;
        }

        result = default;
        return false;
    }

    private static bool IsNumeric(Type type)
    {
        return numericTypes.Contains(type);
    }

    private static SimpleType GetSimpleType(Type type, PromptHint? typeHint)
    {
        if (type == typeof(string)) return new SimpleType("string", typeHint);
        if (type == typeof(bool)) return new SimpleType("boolean", typeHint);
        return IsNumeric(type) ? new SimpleType("number", typeHint) : new SimpleType(type.Name, typeHint);
    }

    private static bool IsPrimitiveLike(Type type)
    {
        return type.IsPrimitive
               || type == typeof(string)
               || type == typeof(decimal)
               || type == typeof(DateTime)
               || type == typeof(Guid);
    }
}
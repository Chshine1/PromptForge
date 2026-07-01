using System.Collections.Immutable;
using System.Reflection;
using PromptForge.Abstractions.Attributes;
using PromptForge.Abstractions.Attributes.Types;
using PromptForge.Abstractions.Enums;
using PromptForge.Abstractions.Model;

namespace PromptForge.Core.Metadata.Registry;

public static partial class TypeMetadataRegistry
{
    private static bool TryBuildObjectResult(
        Type clrType,
        PromptHint? typeHint,
        out RegisterResult result)
    {
        var objectAttr = clrType.GetCustomAttribute<ObjectTypeAttribute>();
        if (objectAttr is null)
        {
            result = default;
            return false;
        }

        var occurrences = new List<Type> { clrType };
        var properties = clrType
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead)
            .Where(p => p.GetCustomAttribute<PromptIgnoreAttribute>() == null)
            .Select(p =>
            {
                var propHints = HintAttributes.CollectFromMember(p);
                var propHint = propHints.BuildHint(HintTarget.TypeAndProperty);
                var propResult = RegisterClrType(p.PropertyType);
                occurrences.AddRange(propResult.TypeOccurrences);

                switch (propResult.Type)
                {
                    case MapType mapType:
                    {
                        var newMapType = mapType with { ValueHint = propHints.BuildHint(HintTarget.MapValue) };
                        var updatedResult = propResult with { Type = newMapType };
                        clrToTypeDefinitions[p.PropertyType] = updatedResult;
                        break;
                    }
                    case ArrayType arrayType:
                    {
                        var newArrayType = arrayType with
                        {
                            ElementHint = propHints.BuildHint(HintTarget.ArrayElement)
                        };
                        var updatedResult = propResult with { Type = newArrayType };
                        clrToTypeDefinitions[p.PropertyType] = updatedResult;
                        break;
                    }
                }

                return new PropertyDefinition(p.Name, p.PropertyType, propHint);
            })
            .ToImmutableArray();

        result = new RegisterResult
        {
            Type = new ObjectType(objectAttr.Name ?? clrType.Name, properties, typeHint),
            TypeOccurrences = occurrences.ToImmutableHashSet()
        };
        return true;
    }
}
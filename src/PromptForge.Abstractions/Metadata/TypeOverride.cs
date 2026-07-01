using System.Runtime.CompilerServices;
using PromptForge.Abstractions.Model;

namespace PromptForge.Abstractions.Metadata;

public record TypeOverride(
    PromptHint? Hint = null,
    Dictionary<string, PropertyOverride>? Properties = null,
    PromptHint? ArrayElementHint = null,
    PromptHint? MapValueHint = null)
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Dictionary<string, PropertyOverride> MergeProperties(Dictionary<string, PropertyOverride> properties,
        Dictionary<string, PropertyOverride> otherProperties)
    {
        var mergedProperties = new Dictionary<string, PropertyOverride>();
        var keys = properties.Keys.Concat(otherProperties.Keys).Distinct().ToArray();
        foreach (var key in keys)
        {
            if (properties.TryGetValue(key, out var propertyOverride))
            {
                mergedProperties[key] = propertyOverride.WithOther(otherProperties.GetValueOrDefault(key));
                continue;
            }

            mergedProperties[key] = otherProperties[key];
        }

        return mergedProperties;
    }

    public TypeOverride WithOther(TypeOverride? other)
    {
        Dictionary<string, PropertyOverride>? mergedProperties;
        var otherProperties = other?.Properties;
        if (Properties is not null)
        {
            mergedProperties = otherProperties is not null ? MergeProperties(Properties, otherProperties) : Properties;
        }
        else
        {
            mergedProperties = other?.Properties;
        }

        return other is null
            ? this
            : new TypeOverride(
                Hint: Hint is not null ? Hint.WithOther(other.Hint) : other.Hint,
                Properties: mergedProperties,
                ArrayElementHint: ArrayElementHint is not null
                    ? ArrayElementHint.WithOther(other.ArrayElementHint)
                    : other.ArrayElementHint,
                MapValueHint: MapValueHint is not null ? MapValueHint.WithOther(other.MapValueHint) : other.MapValueHint
            );
    }
}
using System.Collections.Immutable;
using PromptForge.Abstractions;
using PromptForge.Abstractions.Model;

namespace PromptForge.Core.Metadata;

public class MetadataScope(ImmutableDictionary<Type, ITypeDefinition> typeDefinitions) : IMetadataScope
{
    public ITypeDefinition? this[Type type] => typeDefinitions.GetValueOrDefault(type);
}

public class MetadataScopeBuilder(IEnumerable<Type> scopeTypes) : IMetadataScopeBuilder
{
    private readonly ImmutableDictionary<Type, ITypeDefinition> _typeDefinitions = scopeTypes
        .Select(t => new KeyValuePair<Type, ITypeDefinition>(t, TypeMetadataBuilder.GetDefinitionFromClrType(t)))
        .ToImmutableDictionary();

    private readonly Dictionary<Type, TypeOverride> _typeOverrides = new();

    public void OverrideType(Type type, TypeOverride newTypeOverride)
    {
        if (_typeOverrides.TryGetValue(type, out var existing)) newTypeOverride = existing.WithOther(newTypeOverride);
        _typeOverrides[type] = newTypeOverride;
    }

    public void OverrideProperty(Type type, string propertyName, PropertyOverride newPropertyOverride)
    {
        var newTypeOverride = new TypeOverride(Properties: new Dictionary<string, PropertyOverride>
            { [propertyName] = newPropertyOverride });
        if (_typeOverrides.TryGetValue(type, out var existingType))
        {
            newTypeOverride = existingType.WithOther(newTypeOverride);
        }

        _typeOverrides[type] = newTypeOverride;
    }

    public IMetadataScope Build()
    {
        Dictionary<Type, ITypeDefinition> overridenTypes = new();
        foreach (var (type, typeDefinition) in _typeDefinitions)
        {
            if (!_typeOverrides.TryGetValue(type, out var typeOverride))
            {
                overridenTypes[type] = typeDefinition;
                continue;
            }
            
            overridenTypes[type] = typeDefinition.OverrideWith(typeOverride);
        }
        return new MetadataScope(overridenTypes.ToImmutableDictionary());
    }
}
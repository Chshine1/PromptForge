using System.Collections.Immutable;
using System.Reflection;
using PromptForge.Abstractions.Metadata;
using PromptForge.Abstractions.Model;
using PromptForge.Abstractions.Serialization;
using PromptForge.Core.Metadata.Registry;

namespace PromptForge.Core.Metadata;

public class MetadataScopeBuilder(IEnumerable<Type> scopeTypes) : IMetadataScopeBuilder
{
    private readonly Dictionary<Type, SerializationConfig> _serializationConfigs = new();

    private readonly ImmutableDictionary<Type, ITypeDefinition> _typeDefinitions = scopeTypes
        .Select(t => new KeyValuePair<Type, ITypeDefinition>(t, TypeMetadataRegistry.GetDefinitionFromClrType(t)))
        .ToImmutableDictionary();

    private readonly Dictionary<Type, TypeOverride> _typeOverrides = new();

    public void SetTypeSerializer<T>(Func<T, ISerializer, string> serializer)
    {
        var config = _serializationConfigs.GetValueOrDefault(typeof(T));
        if (config == null)
        {
            config = new SerializationConfig();
            _serializationConfigs[typeof(T)] = config;
        }

        config.TypeSerializer = (o, s) => serializer((T)o, s);
    }

    public void SetTypeDeserializer<T>(Func<string, ISerializer, T> deserializer) where T : notnull
    {
        var config = _serializationConfigs.GetValueOrDefault(typeof(T));
        if (config == null)
        {
            config = new SerializationConfig();
            _serializationConfigs[typeof(T)] = config;
        }

        config.TypeDeserializer = (str, s) => deserializer(str, s);
    }

    public void SetPropertySerializer<T, TProperty>(string propertyName,
        Func<TProperty?, ISerializer, string> serializer) where TProperty : notnull, new()
    {
        var config = _serializationConfigs.GetValueOrDefault(typeof(T));
        if (config == null)
        {
            config = new SerializationConfig();
            _serializationConfigs[typeof(T)] = config;
        }

        config.PropertySerializers[propertyName] = (o, s) => serializer((TProperty?)o, s);
    }

    public void SetPropertyDeserializer<T, TProperty>(string propertyName,
        Func<string, ISerializer, TProperty?> deserializer) where TProperty : notnull, new()
    {
        var config = _serializationConfigs.GetValueOrDefault(typeof(T));
        if (config == null)
        {
            config = new SerializationConfig();
            _serializationConfigs[typeof(T)] = config;
        }

        config.PropertyDeserializers[propertyName] = (str, s) => deserializer(str, s);
    }

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
            newTypeOverride = existingType.WithOther(newTypeOverride);

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

        return new MetadataScope(overridenTypes
            .Join(
                _serializationConfigs,
                kvp => kvp.Key,
                kvp => kvp.Key,
                (kvp1, kvp2) =>
                {
                    if (kvp1.Value is ObjectType objectType)
                        kvp2.Value.IgnoredProperties.AddRange(kvp2.Key
                            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                            .Select(p => p.Name)
                            .ExceptBy(
                                objectType.Properties.Select(p => p.Name),
                                p => p
                            )
                        );
                    return new KeyValuePair<Type, TypeMetadata>(kvp1.Key, new TypeMetadata(kvp1.Value, kvp2.Value));
                }
            )
            .ToImmutableDictionary());
    }
}
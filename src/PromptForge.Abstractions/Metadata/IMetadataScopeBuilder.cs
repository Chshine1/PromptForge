using PromptForge.Abstractions.Serialization;

namespace PromptForge.Abstractions.Metadata;

public interface IMetadataScopeBuilder
{
    void SetTypeSerializer<T>(Func<T, ISerializer, string> serializer);

    void SetTypeDeserializer<T>(Func<string, ISerializer, T> deserializer) where T : notnull;

    void SetPropertySerializer<T, TProperty>(string propertyName, Func<TProperty?, ISerializer, string> serializer)
        where TProperty : notnull, new();

    void SetPropertyDeserializer<T, TProperty>(string propertyName, Func<string, ISerializer, TProperty?> deserializer)
        where TProperty : notnull, new();

    void OverrideType(Type type, TypeOverride newTypeOverride);

    void OverrideProperty(Type type, string propertyName, PropertyOverride newPropertyOverride);

    IMetadataScope Build();
}
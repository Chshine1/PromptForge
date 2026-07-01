namespace PromptForge.Abstractions.Metadata;

public class SerializationConfig
{
    public Func<object, ISerializer, string>? TypeSerializer { get; set; }
    public Dictionary<string, Func<object, ISerializer, string>> PropertySerializers { get; init; } = new();

    public Func<string, ISerializer, object>? TypeDeserializer { get; set; }
    public Dictionary<string, Func<string, ISerializer, object>> PropertyDeserializers { get; init; } = new();
}

public interface IMetadataScopeBuilder
{
    void SetTypeSerializer<T>(Func<T, ISerializer, string> serializer);
    
    void SetTypeDeserializer<T>(Func<string, ISerializer, T> deserializer) where T : notnull;

    void SetPropertySerializer<T, TProperty>(string propertyName, Func<TProperty, ISerializer, string> serializer);

    void SetPropertyDeserializer<T, TProperty>(string propertyName, Func<string, ISerializer, TProperty> deserializer)
        where TProperty : notnull;

    void OverrideType(Type type, TypeOverride newTypeOverride);
    
    void OverrideProperty(Type type, string propertyName, PropertyOverride newPropertyOverride);
    
    IMetadataScope Build();
}
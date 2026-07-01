namespace PromptForge.Abstractions.Serialization;

public class SerializationConfig
{
    public List<string> IgnoredProperties { get; init; } = [];
    
    public Func<object, ISerializer, string>? TypeSerializer { get; set; }
    public Dictionary<string, Func<object?, ISerializer, string>> PropertySerializers { get; init; } = new();

    public Func<string, ISerializer, object>? TypeDeserializer { get; set; }
    public Dictionary<string, Func<string, ISerializer, object?>> PropertyDeserializers { get; init; } = new();
}
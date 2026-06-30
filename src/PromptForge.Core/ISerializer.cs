using System.Reflection;
using System.Text.Json;

namespace PromptForge.Core;

public class SerializeConfiguration
{
    public IEnumerable<string> IgnoredProperties { get; init; } = [];
    public Func<object, ISerializer, string>? TypeSerializer { get; init; }
    public Dictionary<string, Func<object, ISerializer, string>> PropertySerializers { get; init; } = [];
}

public class DeserializeConfiguration
{
    public Func<string, ISerializer, object?>? TypeDeserializer { get; init; }
    public Dictionary<string, Func<string, ISerializer, object?>> PropertyDeserializers { get; init; } = [];
}

public interface ISerializer
{
    string Serialize<T>(T value) where T : notnull;
    T Deserialize<T>(string value) where T : new();
}

public class Serializer(
    Dictionary<Type, SerializeConfiguration> serializers,
    Dictionary<Type, DeserializeConfiguration> deserializers) : ISerializer
{
    public string Serialize<T>(T value) where T : notnull
    {
        if (!serializers.TryGetValue(typeof(T), out var config)) return JsonSerializer.Serialize(value);
        if (config.TypeSerializer != null) return config.TypeSerializer(value, this);

        var dict = new Dictionary<string, object?>();
        var properties = typeof(T)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead)
            .ExceptBy(
                config.IgnoredProperties,
                p => p.Name
            );

        foreach (var prop in properties)
        {
            var propValue = prop.GetValue(value);

            if (config.PropertySerializers.TryGetValue(prop.Name, out var propertySerializer))
            {
                dict[prop.Name] = propertySerializer(propValue, this);
            }
            else
            {
                dict[prop.Name] = propValue;
            }
        }

        return JsonSerializer.Serialize(dict);
    }

    public T Deserialize<T>(string json) where T : new()
    {
        if (!deserializers.TryGetValue(typeof(T), out var config))
            return JsonSerializer.Deserialize<T>(json)!;

        if (config.TypeDeserializer != null)
            return (T)config.TypeDeserializer(json, this);

        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;

        if (root.ValueKind != JsonValueKind.Object)
            throw new JsonException("JSON root must be an object for partial deserialization.");

        var obj = Activator.CreateInstance<T>();

        var properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanWrite);

        JsonNamingPolicy? namingPolicy = null;

        foreach (var prop in properties)
        {
            var jsonPropName = namingPolicy?.ConvertName(prop.Name) ?? prop.Name;

            if (!root.TryGetProperty(jsonPropName, out var jsonElement))
                continue;

            object? value;
            if (config.PropertyDeserializers.TryGetValue(prop.Name, out var propertyDeserializer))
            {
                var rawJson = jsonElement.GetRawText();
                value = propertyDeserializer(rawJson, this);
            }
            else
            {
                value = JsonSerializer.Deserialize(jsonElement.GetRawText(), prop.PropertyType);
            }

            prop.SetValue(obj, value);
        }

        return obj;
    }
}
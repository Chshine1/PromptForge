using System.Reflection;
using System.Text.Json;
using PromptForge.Abstractions;
using PromptForge.Abstractions.Metadata;

namespace PromptForge.Core;

public class Serializer(Dictionary<Type, SerializationConfig> configs) : ISerializer
{
    public string Serialize<T>(T value) where T : notnull
    {
        if (!configs.TryGetValue(typeof(T), out var config)) return JsonSerializer.Serialize(value);

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

            if (propValue is not null && config.PropertySerializers.TryGetValue(prop.Name, out var propertySerializer))
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

    public T? Deserialize<T>(string json) where T : new()
    {
        if (!configs.TryGetValue(typeof(T), out var config)) return JsonSerializer.Deserialize<T>(json);

        if (config.TypeDeserializer != null) return (T)config.TypeDeserializer(json, this);

        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;

        if (root.ValueKind != JsonValueKind.Object)
            throw new JsonException("JSON root must be an object for partial deserialization.");

        var obj = Activator.CreateInstance<T>();

        var properties = typeof(T)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanWrite);

        foreach (var prop in properties)
        {
            if (!root.TryGetProperty(prop.Name, out var jsonElement)) continue;

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
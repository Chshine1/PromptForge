using System.Collections.Immutable;
using System.Reflection;
using System.Text.Json;
using PromptForge.Abstractions.Serialization;

namespace PromptForge.Core.Serialization;

public class Serializer(ImmutableDictionary<Type, SerializationConfig> configs) : ISerializer
{
    public string SerializePropertyValue(object owner, string propertyName, Type ownerType)
    {
        var prop = ownerType.GetProperty(propertyName,
            BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
        if (prop is null)
            throw new ArgumentException($"Property '{propertyName}' not found on type '{ownerType.Name}'.");

        var value = prop.GetValue(owner);

        if (configs.TryGetValue(ownerType, out var ownerConfig) &&
            ownerConfig.PropertySerializers.TryGetValue(propertyName, out var propSerializer))
        {
            value = propSerializer(value, this);
        }

        return ConvertToTemplateString(value, prop.PropertyType);
    }

    private string ConvertToTemplateString(object? value, Type valueType)
    {
        if (value == null)
            return string.Empty;

        if (configs.TryGetValue(valueType, out var valueConfig) && valueConfig.TypeSerializer != null)
            return valueConfig.TypeSerializer(value, this);

        if (value is string s)
            return s;

        var json = JsonSerializer.Serialize(value, valueType);
        if (valueType == typeof(string) || valueType == typeof(char))
        {
            return json is ['"', _, ..] && json[^1] == '"'
                ? json[1..^1]
                : json;
        }

        return json;
    }

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

    public T? Deserialize<T>(string json) where T : notnull
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
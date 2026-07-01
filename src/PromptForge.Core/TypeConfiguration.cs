using System.Linq.Expressions;
using PromptForge.Abstractions;
using PromptForge.Core.Model;

namespace PromptForge.Core;

public interface ITypeConfiguration
{
    Type ClrType { get; }
    string? Format { get; }
    SerializeConfiguration? GetSerializeConfiguration();
    DeserializeConfiguration? GetDeserializeConfiguration();
    void OverrideType(ITypeDefinition typeDefinition);
}

public class TypeConfiguration<T> : ITypeConfiguration
{
    public Type ClrType { get; } = typeof(T);
    public string? Format { get; private set; }
    private Func<object, ISerializer, string>? _serializer;
    private Func<string, ISerializer, object?>? _deserializer;
    private readonly Dictionary<string, IPropertyConfiguration> _properties = [];

    public SerializeConfiguration? GetSerializeConfiguration()
    {
        var propertySerializers = _properties
            .Where(kvp => kvp.Value.Serializer != null)
            .Select(kvp => (kvp.Key, kvp.Value.Serializer!))
            .ToDictionary();

        if (_serializer is null && propertySerializers.Count == 0) return null;
        return new SerializeConfiguration
        {
            IgnoredProperties = [],
            PropertySerializers = propertySerializers,
            TypeSerializer = _serializer
        };
    }

    public DeserializeConfiguration? GetDeserializeConfiguration()
    {
        var propertyDeserializers = _properties
            .Where(kvp => kvp.Value.Deserializer != null)
            .Select(kvp => (kvp.Key, kvp.Value.Deserializer!))
            .ToDictionary();

        if (_deserializer is null && propertyDeserializers.Count == 0) return null;
        return new DeserializeConfiguration
        {
            PropertyDeserializers = propertyDeserializers,
            TypeDeserializer = _deserializer
        };
    }

    public void OverrideType(ITypeDefinition typeDefinition)
    {
        if (!string.IsNullOrWhiteSpace(Format))
        {
            if (typeDefinition.Hint is not null) typeDefinition.Hint.Format = Format;
            else typeDefinition.Hint = new PromptHint(format: Format);
        }

        if (typeDefinition is not ObjectType objectDefinition) return;

        var pairs = objectDefinition.Properties.Join(
            _properties,
            prop => prop.Name,
            kvp => kvp.Key,
            (prop, kvp) => (prop, kvp.Value)
        );

        foreach (var (property, configuration) in pairs)
        {
            configuration.OverrideProperty(property);
        }
    }

    public TypeConfiguration<T> WithSerialization(Func<T, ISerializer, string> serializer, string? format = null)
    {
        _serializer = (o, s) => serializer((T)o, s);
        Format = format ?? Format;
        return this;
    }

    public TypeConfiguration<T> WithDeserialization(Func<string, ISerializer, T> deserializer, string? format = null)
    {
        _deserializer = (str, s) => deserializer(str, s);
        Format = format ?? Format;
        return this;
    }

    public TypeConfiguration<T> ForProperty<TProp>(Expression<Func<T, TProp>> selector,
        Action<PropertyConfiguration<TProp>> configure)
    {
        if (selector is not MemberExpression memberSelector) throw new ArgumentException("", nameof(selector));

        PropertyConfiguration<TProp> typedConfig;
        if (_properties.TryGetValue(memberSelector.Member.Name, out var config))
        {
            typedConfig = (PropertyConfiguration<TProp>)config;
        }
        else
        {
            typedConfig = new PropertyConfiguration<TProp>(memberSelector);
            _properties.Add(memberSelector.Member.Name, typedConfig);
        }

        configure(typedConfig);
        return this;
    }
}

public interface IPropertyConfiguration
{
    MemberExpression Property { get; }
    string? Format { get; }
    Func<object, ISerializer, string>? Serializer { get; }
    Func<string, ISerializer, object?>? Deserializer { get; }
    void OverrideProperty(PropertyDefinition propertyDefinition);
}

public class PropertyConfiguration<TProperty>(MemberExpression property) : IPropertyConfiguration
{
    public MemberExpression Property { get; } = property;
    public string? Format { get; private set; }
    public Func<object, ISerializer, string>? Serializer { get; private set; }
    public Func<string, ISerializer, object?>? Deserializer { get; private set; }

    public void OverrideProperty(PropertyDefinition propertyDefinition)
    {
        if (string.IsNullOrWhiteSpace(Format)) return;

        if (propertyDefinition.Hint is not null) propertyDefinition.Hint.Format = Format;
        else propertyDefinition.Hint = new PromptHint(format: Format);
    }

    public PropertyConfiguration<TProperty> WithSerialization(Func<TProperty, ISerializer, string> serializer,
        string? format = null)
    {
        Serializer = (o, s) => serializer((TProperty)o, s);
        Format = format;
        return this;
    }

    public PropertyConfiguration<TProperty> WithDeserialization(Func<string, ISerializer, TProperty> deserializer,
        string? format = null)
    {
        Deserializer = (str, s) => deserializer(str, s);
        Format = format ?? Format;
        return this;
    }
}
using System.Linq.Expressions;
using PromptForge.Abstractions;
using PromptForge.Abstractions.Model;

namespace PromptForge.Core;

public interface ITypeConfiguration
{
    Type ClrType { get; }
    SerializeConfiguration? GetSerializeConfiguration();
    DeserializeConfiguration? GetDeserializeConfiguration();
}

public class TypeConfiguration<T>(IMetadataScopeBuilder scopeBuilder) : ITypeConfiguration
{
    public Type ClrType { get; } = typeof(T);
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

    public TypeConfiguration<T> WithSerialization(Func<T, ISerializer, string> serializer, string? format = null)
    {
        _serializer = (o, s) => serializer((T)o, s);
        scopeBuilder.OverrideType(typeof(T), new TypeOverride(Hint: new PromptHint(Format: format)));
        return this;
    }

    public TypeConfiguration<T> WithDeserialization(Func<string, ISerializer, T> deserializer, string? format = null)
    {
        _deserializer = (str, s) => deserializer(str, s);
        scopeBuilder.OverrideType(typeof(T), new TypeOverride(Hint: new PromptHint(Format: format)));
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
            typedConfig = new PropertyConfiguration<TProp>(memberSelector, scopeBuilder);
            _properties.Add(memberSelector.Member.Name, typedConfig);
        }

        configure(typedConfig);
        return this;
    }
}

public interface IPropertyConfiguration
{
    MemberExpression Property { get; }
    Func<object, ISerializer, string>? Serializer { get; }
    Func<string, ISerializer, object?>? Deserializer { get; }
}

public class PropertyConfiguration<TProperty>(MemberExpression property, IMetadataScopeBuilder scopeBuilder)
    : IPropertyConfiguration
{
    public MemberExpression Property { get; } = property;
    public Func<object, ISerializer, string>? Serializer { get; private set; }
    public Func<string, ISerializer, object?>? Deserializer { get; private set; }

    public PropertyConfiguration<TProperty> WithSerialization(Func<TProperty, ISerializer, string> serializer,
        string? format = null)
    {
        Serializer = (o, s) => serializer((TProperty)o, s);
        scopeBuilder.OverrideProperty(Property.Type, Property.Member.Name,
            new PropertyOverride(Hint: new PromptHint(Format: format)));
        return this;
    }

    public PropertyConfiguration<TProperty> WithDeserialization(Func<string, ISerializer, TProperty> deserializer,
        string? format = null)
    {
        Deserializer = (str, s) => deserializer(str, s);
        scopeBuilder.OverrideProperty(Property.Type, Property.Member.Name,
            new PropertyOverride(Hint: new PromptHint(Format: format)));
        return this;
    }
}
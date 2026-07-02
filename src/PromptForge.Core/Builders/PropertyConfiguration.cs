using JetBrains.Annotations;
using PromptForge.Abstractions.Metadata;
using PromptForge.Abstractions.Model;
using PromptForge.Abstractions.Serialization;

namespace PromptForge.Core.Builders;

public interface IPropertyConfiguration;

[UsedImplicitly(ImplicitUseKindFlags.Access, ImplicitUseTargetFlags.Members)]
public class PropertyConfiguration<T, TProperty>(string propertyName, IMetadataScopeBuilder scopeBuilder)
    : IPropertyConfiguration where TProperty : notnull, new()
{
    public PropertyConfiguration<T, TProperty> WithSerialization(Func<TProperty?, ISerializer, string> serializer,
        string? format = null)
    {
        scopeBuilder.SetPropertySerializer<T, TProperty>(propertyName, serializer);
        scopeBuilder.OverrideProperty(typeof(T), propertyName,
            new PropertyOverride(new PromptHint(Format: format)));
        return this;
    }

    public PropertyConfiguration<T, TProperty> WithDeserialization(Func<string, ISerializer, TProperty> deserializer,
        string? format = null)
    {
        scopeBuilder.SetPropertyDeserializer<T, TProperty>(propertyName, deserializer);
        scopeBuilder.OverrideProperty(typeof(T), propertyName,
            new PropertyOverride(new PromptHint(Format: format)));
        return this;
    }
}
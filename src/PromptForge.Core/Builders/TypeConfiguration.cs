using System.Linq.Expressions;
using JetBrains.Annotations;
using PromptForge.Abstractions;
using PromptForge.Abstractions.Metadata;
using PromptForge.Abstractions.Model;

namespace PromptForge.Core.Builders;

public interface ITypeConfiguration;

[UsedImplicitly(ImplicitUseKindFlags.Access, ImplicitUseTargetFlags.Members)]
public class TypeConfiguration<T>(IMetadataScopeBuilder scopeBuilder) : ITypeConfiguration where T : notnull
{
    private readonly Dictionary<string, IPropertyConfiguration> _properties = [];

    public TypeConfiguration<T> WithSerialization(Func<T, ISerializer, string> serializer, string? format = null)
    {
        scopeBuilder.SetTypeSerializer(serializer);
        scopeBuilder.OverrideType(typeof(T), new TypeOverride(Hint: new PromptHint(Format: format)));
        return this;
    }

    public TypeConfiguration<T> WithDeserialization(Func<string, ISerializer, T> deserializer, string? format = null)
    {
        scopeBuilder.SetTypeDeserializer(deserializer);
        scopeBuilder.OverrideType(typeof(T), new TypeOverride(Hint: new PromptHint(Format: format)));
        return this;
    }

    public TypeConfiguration<T> ForProperty<TProp>(
        Expression<Func<T, TProp>> selector,
        Action<PropertyConfiguration<T, TProp>> configure) where TProp : notnull
    {
        if (selector is not MemberExpression memberSelector) throw new ArgumentException("", nameof(selector));

        PropertyConfiguration<T, TProp> typedConfig;
        if (_properties.TryGetValue(memberSelector.Member.Name, out var config))
        {
            typedConfig = (PropertyConfiguration<T, TProp>)config;
        }
        else
        {
            typedConfig = new PropertyConfiguration<T, TProp>(memberSelector.Member.Name, scopeBuilder);
            _properties.Add(memberSelector.Member.Name, typedConfig);
        }

        configure(typedConfig);
        return this;
    }
}
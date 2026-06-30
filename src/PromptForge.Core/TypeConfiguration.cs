using System.Linq.Expressions;

namespace PromptForge.Core;

public interface ITypeConfiguration
{
    Type ClrType { get; }
    string? Format { get; }
    Func<object, string>? CustomSerializer { get; }
}

public class TypeConfiguration<T> : ITypeConfiguration
{
    public Type ClrType { get; } = typeof(T);
    public string? Format { get; private set; }
    public Func<object, string>? CustomSerializer { get; private set; }
    private readonly Dictionary<string, IPropertyConfiguration> _properties = [];

    public TypeConfiguration<T> WithSerialization(Func<T, string> serializer, string? format = null)
    {
        CustomSerializer = o => serializer((T)o);
        Format = format;
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
    Func<object, string>? CustomSerializer { get; }
}

public class PropertyConfiguration<TProperty>(MemberExpression property) : IPropertyConfiguration
{
    public MemberExpression Property { get; } = property;
    public string? Format { get; private set; }
    public Func<object, string>? CustomSerializer { get; private set; }

    public PropertyConfiguration<TProperty> WithSerialization(Func<TProperty, string> serializer, string? format = null)
    {
        CustomSerializer = o => serializer((TProperty)o);
        Format = format;
        return this;
    }
}
namespace PromptForge.Abstractions.Model;

public interface ITypeDefinition
{
    string Name { get; }
    PromptHint? Hint { get; }
    public ITypeDefinition OverrideWith(TypeOverride @override);
}

public record SimpleType(string Name, PromptHint? Hint = null) : ITypeDefinition
{
    public ITypeDefinition OverrideWith(TypeOverride @override)
    {
        return this with { Hint = Hint is not null ? Hint.WithOther(@override.Hint) : @override.Hint };
    }
}

public record ObjectType(string Name, IEnumerable<PropertyDefinition> Properties, PromptHint? Hint = null)
    : ITypeDefinition
{
    public ITypeDefinition OverrideWith(TypeOverride @override)
    {
        return this with
        {
            Properties = Properties.Select(p => @override.Properties?.TryGetValue(p.Name, out var propertyOverride) is true ? p.OverrideWith(propertyOverride) : p),
            Hint = Hint is not null ? Hint.WithOther(@override.Hint) : @override.Hint
        };
    }
}

public record ArrayType(
    Type ElementType,
    PromptHint? Hint = null,
    PromptHint? ElementHint = null) : ITypeDefinition
{
    public string Name => "Array";
    public ITypeDefinition OverrideWith(TypeOverride @override)
    {
        return this with
        {
            Hint = Hint is not null ? Hint.WithOther(@override.Hint) : @override.Hint,
            ElementHint = ElementHint is not null ? ElementHint.WithOther(@override.ArrayElementHint) : @override.ArrayElementHint
        };
    }
}

public record MapType(Type ValueType, PromptHint? Hint = null, PromptHint? ValueHint = null)
    : ITypeDefinition
{
    public string Name => "Map";
    public ITypeDefinition OverrideWith(TypeOverride @override)
    {
        return this with
        {
            Hint = Hint is not null ? Hint.WithOther(@override.Hint) : @override.Hint,
            ValueHint = ValueHint is not null ? ValueHint.WithOther(@override.ArrayElementHint) : @override.ArrayElementHint
        };
    }
}
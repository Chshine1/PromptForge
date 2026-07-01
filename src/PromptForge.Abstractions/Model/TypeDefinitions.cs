namespace PromptForge.Abstractions.Model;

public interface ITypeDefinition
{
    Type ClrType { get; }
    string Name { get; }
    PromptHint? Hint { get; }
    public ITypeDefinition OverrideWith(TypeOverride @override);
}

public record SimpleType(Type ClrType, string Name, PromptHint? Hint = null) : ITypeDefinition
{
    public ITypeDefinition OverrideWith(TypeOverride @override)
    {
        return this with { Hint = Hint is not null ? Hint.WithOther(@override.Hint) : @override.Hint };
    }
}

public record ObjectType(Type ClrType, string Name, IEnumerable<PropertyDefinition> Properties, PromptHint? Hint = null)
    : ITypeDefinition
{
    public ITypeDefinition OverrideWith(TypeOverride @override)
    {
        // TODO
        return this with { Hint = Hint is not null ? Hint.WithOther(@override.Hint) : @override.Hint };
    }
}

public record ArrayType(
    Type ClrType,
    Type ElementType,
    PromptHint? Hint = null,
    PromptHint? ElementHint = null) : ITypeDefinition
{
    public string Name => "Array";
    public ITypeDefinition OverrideWith(TypeOverride @override)
    {
        throw new NotImplementedException();
    }
}

public record MapType(Type ClrType, Type ValueType, PromptHint? Hint = null, PromptHint? ValueHint = null)
    : ITypeDefinition
{
    public string Name => "Map";
    public ITypeDefinition OverrideWith(TypeOverride @override)
    {
        throw new NotImplementedException();
    }
}
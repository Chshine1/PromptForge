namespace PromptForge.Core.Model;

public record SimpleType(string Name, PromptHint? Hint = null) : ITypeDefinition;

public record ObjectType(string Name, IEnumerable<PropertyDefinition> Properties, PromptHint? Hint = null) : ITypeDefinition;

public record ArrayType(ITypeDefinition ElementType, PromptHint? ElementHint = null) : ITypeDefinition
{
    public string Name => "Array";
    public PromptHint? Hint => null;
}

public record MapType(ITypeDefinition ValueType, PromptHint? ValueHint = null) : ITypeDefinition
{
    public string Name => "Map";
    public PromptHint? Hint => null;
}
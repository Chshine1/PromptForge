namespace PromptForge.Core.Model;

public record SimpleType(string Name, PromptHint? Hint) : ITypeDefinition;

public record ObjectType(string Name, PromptHint? Hint, IEnumerable<PropertyDefinition> Properties) : ITypeDefinition;

public record ArrayType(ITypeDefinition ElementType) : ITypeDefinition
{
    public string Name => "Array";
    public PromptHint? Hint => null;
}

public record MapType(ITypeDefinition ValueType) : ITypeDefinition
{
    public string Name => "Map";
    public PromptHint? Hint => null;
}
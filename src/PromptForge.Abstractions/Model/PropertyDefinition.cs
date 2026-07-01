namespace PromptForge.Abstractions.Model;

public class PropertyDefinition(
    string name,
    ITypeDefinition typeDefinition,
    PromptHint? hint = null)
{
    public string Name { get; set; } = name;
    public ITypeDefinition TypeDefinition { get; set; } = typeDefinition;
    public PromptHint? Hint { get; set; } = hint;
}
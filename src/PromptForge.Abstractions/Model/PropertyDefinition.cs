namespace PromptForge.Abstractions.Model;

public class PropertyDefinition(
    string name,
    Type typeDefinition,
    PromptHint? hint = null)
{
    public string Name { get; set; } = name;
    public Type TypeDefinition { get; set; } = typeDefinition;
    public PromptHint? Hint { get; set; } = hint;
}
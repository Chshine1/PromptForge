namespace PromptForge.Core.Model;

public record PropertyDefinition(
    string Name,
    ITypeDefinition TypeDefinition,
    PromptHint? Hint);
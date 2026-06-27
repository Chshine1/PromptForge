namespace PromptForge.Core.Model;

public interface ITypeDefinition
{
    string Name { get; }
    PromptHint? Hint { get; }
}
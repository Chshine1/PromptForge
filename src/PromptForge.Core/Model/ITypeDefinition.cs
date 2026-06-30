namespace PromptForge.Core.Model;

public interface ITypeDefinition
{
    Type ClrType { get; }
    string Name { get; set; }
    PromptHint? Hint { get; set; }
}
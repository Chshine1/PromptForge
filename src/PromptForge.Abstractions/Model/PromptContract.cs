namespace PromptForge.Abstractions.Model;

public record PromptContract(
    string PromptTemplate,
    ObjectType InputType,
    ITypeDefinition OutputType);
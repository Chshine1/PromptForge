namespace PromptForge.Core.Model;

public record PromptContract(
    string PromptTemplate,
    ObjectType InputType,
    ITypeDefinition OutputType);
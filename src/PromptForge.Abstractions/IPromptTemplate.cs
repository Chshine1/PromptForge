namespace PromptForge.Abstractions;

public interface IPromptTemplate<in TInput>
{
    string Resolve(TInput data);
}
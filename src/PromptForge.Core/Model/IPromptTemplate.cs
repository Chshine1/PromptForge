namespace PromptForge.Core.Model;

public interface IPromptTemplate<in TInput>
{
    string Resolve(TInput data);
}
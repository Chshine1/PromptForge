using PromptForge.Abstractions.Model;

namespace PromptForge.Abstractions;

public interface IPromptCompiler
{
    IPromptTemplate<TIn> Compile<TIn, TOut>(string template, IMetadataScope scope);
}
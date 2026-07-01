using PromptForge.Abstractions.Metadata;

namespace PromptForge.Abstractions;

public interface IPromptCompiler
{
    IPromptTemplate<TIn> Compile<TIn, TOut>(string template, IMetadataScope scope);
}
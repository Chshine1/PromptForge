using PromptForge.Abstractions.Metadata;

namespace PromptForge.Abstractions;

public interface IPromptCompiler
{
    IPromptPipeline<TIn, TOut> Compile<TIn, TOut>(ILlmInvoker llmInvoker, string template, IMetadataScope scope) where TIn : notnull where TOut : notnull;
}
namespace PromptForge.Abstractions;

public interface IPromptPipeline<in TIn, TOut>
{
    Task<TOut?> RunAsync(TIn input, CancellationToken ct = default);
}
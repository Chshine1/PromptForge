namespace PromptForge.Abstractions;

public interface ILlmInvoker
{
    Task<string> InvokeAsync(string prompt, CancellationToken cancellationToken = default);
}
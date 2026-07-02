using System.Text;
using PromptForge.Abstractions;
using PromptForge.Abstractions.Serialization;

namespace PromptForge.Core;

internal class PromptPipeline<TIn, TOut>(
    List<string> staticParts,
    List<Func<TIn, string>> valueGetters,
    ILlmInvoker llm,
    ISerializer serializer)
    : IPromptPipeline<TIn, TOut> where TIn : notnull where TOut : notnull
{
    private readonly string[] _staticParts = [.. staticParts];
    private readonly Func<TIn, string>[] _valueGetters = [.. valueGetters];

    public async Task<TOut?> RunAsync(TIn input, CancellationToken ct = default)
    {
        var sb = new StringBuilder();
        for (var i = 0; i < _valueGetters.Length; i++)
        {
            sb.Append(_staticParts[i]);
            sb.Append(_valueGetters[i](input));
        }

        sb.Append(_staticParts[^1]);
        var promptText = sb.ToString();

        var response = await llm.InvokeAsync(promptText, ct).ConfigureAwait(false);
        return serializer.Deserialize<TOut>(response);
    }
}
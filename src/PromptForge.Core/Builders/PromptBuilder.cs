using PromptForge.Abstractions;
using PromptForge.Abstractions.Metadata;

namespace PromptForge.Core.Builders;

public class PromptBuilder<TIn, TOut>(IPromptCompiler compiler, IMetadataScopeBuilder scopeBuilder)
    where TIn : notnull where TOut : notnull
{
    private readonly Dictionary<Type, ITypeConfiguration> _types = [];
    private ILlmInvoker? _llmInvoker;
    private string? _template;

    public PromptBuilder<TIn, TOut> WithLlmInvoker(ILlmInvoker llmInvoker)
    {
        _llmInvoker = llmInvoker;
        return this;
    }

    public PromptBuilder<TIn, TOut> WithTemplate(string template)
    {
        _template = template;
        return this;
    }

    public PromptBuilder<TIn, TOut> WithType<T>(Action<TypeConfiguration<T>> configure) where T : notnull
    {
        TypeConfiguration<T> typedConfig;

        if (_types.TryGetValue(typeof(T), out var config))
        {
            typedConfig = (TypeConfiguration<T>)config;
        }
        else
        {
            typedConfig = new TypeConfiguration<T>(scopeBuilder);
            _types.Add(typeof(T), typedConfig);
        }

        configure(typedConfig);
        return this;
    }

    public IPromptPipeline<TIn, TOut> Build()
    {
        return _llmInvoker != null && _template != null
            ? compiler.Compile<TIn, TOut>(_llmInvoker, _template, scopeBuilder.Build())
            : throw new InvalidOperationException("Template not set.");
    }
}
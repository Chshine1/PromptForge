using PromptForge.Abstractions;
using PromptForge.Core.Metadata;

namespace PromptForge.Core;

public class PromptBuilderFactory(IPromptCompiler compiler)
{
    public PromptBuilder<TInput, TOutput> Create<TInput, TOutput>()
    {
        TypeMetadataBuilder.RegisterClrType(typeof(TInput));
        TypeMetadataBuilder.RegisterClrType(typeof(TOutput));

        var scopeBuilder = new MetadataScopeBuilder(
            TypeMetadataBuilder.RegisterClrType(typeof(TInput)).TypeOccurrences
                .Concat(TypeMetadataBuilder.RegisterClrType(typeof(TOutput)).TypeOccurrences)
        );
        return new PromptBuilder<TInput, TOutput>(compiler, scopeBuilder);
    }
}

public class PromptBuilder<TInput, TOutput>(IPromptCompiler compiler, IMetadataScopeBuilder scopeBuilder)
{
    private string? _template;
    private readonly Dictionary<Type, ITypeConfiguration> _types = [];

    public PromptBuilder<TInput, TOutput> WithTemplate(string template)
    {
        _template = template;
        return this;
    }

    public PromptBuilder<TInput, TOutput> WithType<T>(Action<TypeConfiguration<T>> configure)
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

    public IPromptTemplate<TInput> Build()
    {
        return _template != null
            ? compiler.Compile<TInput, TOutput>(_template, scopeBuilder.Build())
            : throw new InvalidOperationException("Template not set.");
    }
}
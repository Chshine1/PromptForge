using PromptForge.Abstractions;
using PromptForge.Abstractions.Model;
using PromptForge.Core.Metadata;

namespace PromptForge.Core;

public class PromptBuilder<TInput, TOutput>(IPromptCompiler compiler)
{
    private string? _template;
    private readonly Dictionary<Type, ITypeConfiguration> _types = [];

    private readonly IMetadataScopeBuilder _scopeBuilder = new MetadataScopeBuilder(
        TypeMetadataBuilder.RegisterClrType(typeof(TInput)).TypeOccurrences
            .Concat(TypeMetadataBuilder.RegisterClrType(typeof(TOutput)).TypeOccurrences)
    );

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
            typedConfig = new TypeConfiguration<T>(_scopeBuilder);
            _types.Add(typeof(T), typedConfig);
        }

        configure(typedConfig);
        return this;
    }

    public IPromptTemplate<TInput> Build()
    {
        if (_template == null) throw new InvalidOperationException("Template not set.");

        var inputDef = TypeMetadataBuilder.RegisterClrType(typeof(TInput));
        var outputDef = TypeMetadataBuilder.RegisterClrType(typeof(TOutput));

        if (inputDef.Type is not ObjectType objInputDef)
            throw new InvalidOperationException("Input type not supported.");

        var contract = new PromptContract(_template, objInputDef, outputDef.Type);

        return compiler.Compile<TInput>(contract, _scopeBuilder.Build());
    }
}
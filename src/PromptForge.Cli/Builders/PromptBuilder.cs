using PromptForge.Compilation;
using PromptForge.Core;
using PromptForge.Core.Model;

namespace PromptForge.Cli.Builders;

public class PromptBuilder<TInput, TOutput>(IPromptCompiler compiler)
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
            typedConfig = new TypeConfiguration<T>();
            _types.Add(typeof(T), typedConfig);
        }

        configure(typedConfig);
        return this;
    }

    public IPromptTemplate<TInput> Build()
    {
        if (_template == null) throw new InvalidOperationException("Template not set.");

        var builder = new TypeMetadataBuilder();
        var inputDef = builder.FromClrType(typeof(TInput));
        var outputDef = builder.FromClrType(typeof(TOutput));

        var pairs = builder.ClrToTypeDefinitions
            .Join(_types,
                kvp => kvp.Key,
                kvp => kvp.Key,
                (kvp1, kvp2) => (kvp1.Value, kvp2.Value));

        if (inputDef is not ObjectType objInputDef) throw new InvalidOperationException("Input type not supported.");

        foreach (var (definition, configuration) in pairs)
        {
            configuration.OverrideType(definition);
        }

        var contract = new PromptContract(_template, objInputDef, outputDef);

        compiler.SetSerialization(new Serializer(
            _types
                .Select(kvp => (kvp.Key, kvp.Value.GetSerializeConfiguration()))
                .Where(kvp => kvp.Item2 is not null).ToDictionary()!,
            _types
                .Select(kvp => (kvp.Key, kvp.Value.GetDeserializeConfiguration()))
                .Where(kvp => kvp.Item2 is not null).ToDictionary()!));

        return compiler.Compile<TInput>(contract);
    }
}
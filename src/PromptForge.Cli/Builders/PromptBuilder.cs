using PromptForge.Compilation;
using PromptForge.Core;
using PromptForge.Core.Model;

namespace PromptForge.Cli.Builders;

public class PromptBuilder(IPromptCompiler compiler)
{
    private string? _template;
    private Type? _inputType;
    private Type? _outputType;
    private readonly Dictionary<Type, ITypeConfiguration> _types = [];

    public PromptBuilder WithTemplate(string template)
    {
        _template = template;
        return this;
    }

    public PromptBuilder WithInput<TInput>()
    {
        _inputType = typeof(TInput);
        return this;
    }

    public PromptBuilder WithOutput<TOutput>()
    {
        _outputType = typeof(TOutput);
        return this;
    }

    public PromptBuilder WithType<T>(Action<TypeConfiguration<T>> configure)
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

    public IFillable<TInput> Build<TInput>()
    {
        if (_template == null) throw new InvalidOperationException("Template not set.");
        if (_inputType == null) throw new InvalidOperationException("Input type not set.");
        if (_outputType == null) throw new InvalidOperationException("Output type not set.");
        if (_inputType != typeof(TInput))
            throw new InvalidOperationException("The requested TInput does not match the configured input type.");

        var builder = new TypeMetadataBuilder();
        var inputDef = builder.FromClrType(_inputType);
        var outputDef = builder.FromClrType(_outputType);

        if (inputDef is not ObjectType objInputDef) throw new InvalidOperationException("Input type not supported.");

        var contract = new PromptContract(_template, objInputDef, outputDef);

        return compiler.Compile<TInput>(contract);
    }
}
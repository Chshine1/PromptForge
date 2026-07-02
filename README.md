# PromptForge

A type‑first prompt engineering framework for .NET. Define your LLM inputs and outputs as strongly typed C# objects, and
let PromptForge handle serialization, template rendering, schema injection, and response parsing.

## Quick Start

1. Install the NuGet package (coming soon) or reference the project.
2. Define your input/output types:

```csharp
public record MyInput(string Question);
public record MyOutput(string Answer);
```

3. Build a pipeline and run it:

```csharp
var pipeline = new PromptBuilderFactory(compiler)
    .Create<MyInput, MyOutput>()
    .WithLlmInvoker(new MyLlmInvoker())
    .WithTemplate("Answer the following: {{Question}}")
    .WithOutputDeserializer(s => new MyOutput(s))
    .Build();

MyOutput result = await pipeline.RunAsync(new MyInput("What is PromptForge?"));
Console.WriteLine(result.Answer);
```

See `src/PromptForge.Samples` for more complete examples.

## Status

This project is in early development (v0.0.1). APIs may change. Contributions are very welcome –
see [CONTRIBUTING.md](CONTRIBUTING.md).

## Why PromptForge?

- **Type safety**: Eliminate brittle string concatenation and manual JSON parsing.
- **Declarative schema injection**: Automatically describe input/output structures in your prompts.
- **Extensible**: Custom serializers, middlewares, and plugins.
- **Native .NET feel**: Designed with dependency injection, expression trees, and fluent APIs.

## License

MIT (see [LICENSE](LICENSE))
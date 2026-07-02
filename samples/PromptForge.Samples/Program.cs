using System.Text.Json;
using JetBrains.Annotations;
using PromptForge.Abstractions;
using PromptForge.Core;
using PromptForge.Core.Builders;
using PromptForge.Core.Serialization;

namespace PromptForge.Samples;

[UsedImplicitly]
public class Program
{
    private class MockLlmInvoker : ILlmInvoker
    {
        public Task<string> InvokeAsync(string prompt, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
    }

    public static void Main(string[] args)
    {
        _ = args;

        var compiler = new PromptCompiler(new SerializerFactory());

        var evaluationBuilder = new PromptBuilderFactory(compiler)
            .Create<EvaluationInput, string[]>()
            .WithLlmInvoker(new MockLlmInvoker())
            .WithTemplate(
                """
                Determine which conditions are satisfied. You will be given:
                {{schema:input:Conditions}}
                and
                {{schema:input:BufferStates}}
                which you can evaluate the conditions against.

                Output as follows:
                {{schema:output}}

                Conditions:
                {{Conditions}}
                BufferStates:
                {{BufferStates}}
                """)
            .WithType<StructData>(type => type
                .WithSerialization(
                    (data, _) => JsonSerializer.Serialize(data.Data),
                    "JSON string")
            )
            .WithType<string[]>(stringArray => stringArray
                .WithDeserialization(
                    (serialized, _) => [.. serialized.Split(',').Select(s => s.Trim())],
                    "comma separated strings")
            );
        var evaluationPipeline = evaluationBuilder.Build();

        _ = evaluationPipeline.RunAsync(new EvaluationInput
        {
            BufferStates =
            [
                new BufferState
                {
                    ModuleId = "test_module_1",
                    Data = new StructData
                    {
                        Data = new Dictionary<string, string> { ["message"] = "Hello World!" }
                    }
                }
            ],
            Conditions = []
        });
    }
}
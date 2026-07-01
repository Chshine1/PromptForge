using System.Text.Json;
using JetBrains.Annotations;
using PromptForge.Core;
using PromptForge.Core.Builders;
using PromptForge.Core.Serialization;

namespace PromptForge.Cli;

[UsedImplicitly]
public class Program
{
    public static void Main(string[] args)
    {
        var compiler = new PromptCompiler(new SerializerFactory());
        
        var builder = new PromptBuilderFactory(compiler)
            .Create<EvaluationInput, string[]>()
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
                    (serialized, _) => serialized.Split(',').Select(s => s.Trim()).ToArray(),
                    "comma separated strings")
            );
        var pipeline = builder.Build();

        Task<string[]?> _ = pipeline.RunAsync(new EvaluationInput
        {
            BufferStates = [
                new BufferState
                {
                    ModuleId = "test_module_1",
                    Data = new StructData
                    {
                        Data = new Dictionary<string, string>
                        {
                            ["message"] = "Hello World!"
                            
                        }
                    }
                }
            ],
            Conditions = []
        });
    }
}
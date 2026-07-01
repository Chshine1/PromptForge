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
        _ = args;

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

        var template = builder.Build();
        var instance = template.Resolve(new EvaluationInput
        {
            BufferStates = [],
            Conditions = []
        });
        
        Console.WriteLine(instance);
    }
}
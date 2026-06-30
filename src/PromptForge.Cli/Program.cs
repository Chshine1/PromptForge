using System.Text.Json;
using JetBrains.Annotations;
using PromptForge.Cli.Builders;
using PromptForge.Compilation;

namespace PromptForge.Cli;

[UsedImplicitly]
public class Program
{
    public static void Main(string[] args)
    {
        _ = args;
        
        var compiler = new PromptCompiler();
        var builder = new PromptBuilder(compiler)
            .WithTemplate(
                """
                Determine which conditions are satisfied. You will be given:
                {{schema:input:Conditions}}
                and
                {{schema:input:BufferStates}}
                which you can evaluate the conditions against.

                Output as follows:
                {{schema:output}}
                """)
            .WithInput<EvaluationInput>()
            .WithOutput<string[]>()
            .WithType<StructData>(type => type
                .WithSerialization(
                    data => JsonSerializer.Serialize(data.Data),
                    "JSON string")
            )
            .WithType<BufferOperation>(type => type
                .ForProperty(op => op.Params, prop =>
                    prop.WithSerialization(
                        p => JsonSerializer.Serialize(p.Data),
                        "JSON matching command schema"))
            );

        _ = builder.Build<EvaluationInput>();
    }
}
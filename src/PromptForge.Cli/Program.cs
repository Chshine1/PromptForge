using JetBrains.Annotations;

namespace PromptForge.Cli;

[UsedImplicitly]
public class Program
{
    public static void Main(string[] args)
    {
        _ = args;
        _ = """
            Determine which conditions are satisfied. You will be given:
            {{schema:input:Conditions}}
            and
            {{schema:input:BufferStates}}
            which you can evaluate the conditions against.

            Output as follows:
            {{schema:output}}
            """;
    }
}
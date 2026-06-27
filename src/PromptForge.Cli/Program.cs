using JetBrains.Annotations;

namespace PromptForge.Cli;

[UsedImplicitly]
public class Program
{
    public static void Main(string[] args)
    {
        string ToYaml(ExampleInput obj) =>
            $"""
                 buffer_states:
                 {string.Join("\n  ", obj.BufferStates.Select(kv => $"{kv.Key}:\n    name: {kv.Value.Name}\n    data: {kv.Value.Data}"))}
                 conditions:
                 {string.Join("\n  ", obj.Conditions.Select(c => $"- rule_id: {c.RuleId}\n  predicate: {c.Predicate}"))}
                 """.Trim();
    }
}
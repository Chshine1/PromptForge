using JetBrains.Annotations;

namespace PromptForge.Cli;

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public class BufferState
{
    public required string Name { get; init; }
    public required string Data { get; init; }
}

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public class Condition
{
    public required string RuleId { get; init; }
    public required string Predicate { get; init; }
}

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public class ExampleInput
{
    public required Dictionary<string, BufferState> BufferStates { get; init; }
    public required List<Condition> Conditions { get; init; }
}

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public class ExampleOutput
{
    public required List<string> RuleIds { get; init; }
}
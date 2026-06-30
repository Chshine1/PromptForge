using JetBrains.Annotations;
using PromptForge.Core;

namespace PromptForge.Cli;

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
[SimpleType]
[FormatHint(Format = "binary string")]
public struct Bitmask(long @decimal)
{
    private long _decimal = @decimal;
}

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
[ObjectType]
public class ModuleState
{
    [InputHint(Semantic = "the modules which this state is shared amongst")]
    public Bitmask StateOf { get; init; }

    [PropertyIgnore] public string? InternalId { get; init; }

    public List<string> Tags { get; set; } = [];
}
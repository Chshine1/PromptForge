using JetBrains.Annotations;

namespace PromptForge.Abstractions.Attributes.Hints;

[UsedImplicitly(ImplicitUseKindFlags.Assign, ImplicitUseTargetFlags.WithMembers)]
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Property, AllowMultiple = true)]
public class FormatHintAttribute : Attribute
{
    public string? Format { get; init; }
    public HintTarget Target { get; init; } = HintTarget.TypeAndProperty;
}
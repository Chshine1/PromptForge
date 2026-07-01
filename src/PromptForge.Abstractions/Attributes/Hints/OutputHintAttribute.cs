using JetBrains.Annotations;
using PromptForge.Abstractions.Enums;

namespace PromptForge.Abstractions.Attributes.Hints;

[UsedImplicitly(ImplicitUseKindFlags.Assign, ImplicitUseTargetFlags.WithMembers)]
[MeansImplicitUse(ImplicitUseKindFlags.Assign, ImplicitUseTargetFlags.Members)]
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Property, AllowMultiple = true)]
public class OutputHintAttribute : Attribute
{
    public string? Purpose { get; init; }
    public string? Constraint { get; init; }
    public HintTarget Target { get; init; } = HintTarget.TypeAndProperty;
}
using JetBrains.Annotations;

namespace PromptForge.Abstractions.Attributes.Hints;

[UsedImplicitly(ImplicitUseKindFlags.Assign, ImplicitUseTargetFlags.WithMembers)]
[MeansImplicitUse(ImplicitUseKindFlags.Access, ImplicitUseTargetFlags.Members)]
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Property, AllowMultiple = true)]
public class InputHintAttribute : Attribute
{
    public string? Semantic { get; init; }
    public HintTarget Target { get; init; } = HintTarget.TypeAndProperty;
}
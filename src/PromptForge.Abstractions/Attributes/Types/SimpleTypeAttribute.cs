using JetBrains.Annotations;

namespace PromptForge.Abstractions.Attributes.Types;

[UsedImplicitly(ImplicitUseKindFlags.Assign, ImplicitUseTargetFlags.WithMembers)]
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
public class SimpleTypeAttribute : Attribute
{
    public string? Name { get; init; }
}
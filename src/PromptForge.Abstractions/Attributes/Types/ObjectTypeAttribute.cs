using JetBrains.Annotations;

namespace PromptForge.Abstractions.Attributes.Types;

[UsedImplicitly(ImplicitUseKindFlags.Assign, ImplicitUseTargetFlags.WithMembers)]
[AttributeUsage(AttributeTargets.Class)]
public class ObjectTypeAttribute : Attribute
{
    public string? Name { get; init; }
}
using JetBrains.Annotations;

namespace PromptForge.Abstractions.Attributes;

[UsedImplicitly(ImplicitUseKindFlags.Assign, ImplicitUseTargetFlags.WithMembers)]
[AttributeUsage(AttributeTargets.Property)]
public class PromptIgnoreAttribute : Attribute;
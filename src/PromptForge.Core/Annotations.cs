using JetBrains.Annotations;

namespace PromptForge.Core;

[AttributeUsage(AttributeTargets.Class)]
public class ObjectTypeAttribute : Attribute
{
    public string? Name { get; init; }
}

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
public class SimpleTypeAttribute : Attribute
{
    public string? Name { get; init; }
}

public enum HintTarget
{
    TypeAndProperty,
    ArrayElement,
    MapValue
}

[MeansImplicitUse(ImplicitUseKindFlags.Access, ImplicitUseTargetFlags.Members)]
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Property, AllowMultiple = true)]
public class InputHintAttribute : Attribute
{
    public string? Semantic { get; init; }
    public HintTarget Target { get; init; } = HintTarget.TypeAndProperty;
}

[MeansImplicitUse(ImplicitUseKindFlags.Assign, ImplicitUseTargetFlags.Members)]
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Property, AllowMultiple = true)]
public class OutputHintAttribute : Attribute
{
    public string? Purpose { get; init; }
    public string? Constraint { get; init; }
    public HintTarget Target { get; init; } = HintTarget.TypeAndProperty;
}

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Property, AllowMultiple = true)]
public class FormatHintAttribute : Attribute
{
    public string? Format { get; init; }
    public HintTarget Target { get; init; } = HintTarget.TypeAndProperty;
}

[AttributeUsage(AttributeTargets.Property)]
public class PromptIgnoreAttribute : Attribute;
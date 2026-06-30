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

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Property)]
public class InputHintAttribute : Attribute
{
    public string? Semantic { get; init; }
}

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Property)]
public class OutputHintAttribute : Attribute
{
    public string? Purpose { get; init; }
    public string? Constraint { get; init; }
}

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Property)]
public class FormatHintAttribute : Attribute
{
    public string? Format { get; init; }
}

[AttributeUsage(AttributeTargets.Property)]
public class PropertyIgnoreAttribute : Attribute;
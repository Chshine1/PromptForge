namespace PromptForge.Core.Model;

public interface ITypeDefinition
{
    Type ClrType { get; }
    string Name { get; set; }
    PromptHint? Hint { get; set; }
}

public class SimpleType(Type clrType, string name, PromptHint? hint = null) : ITypeDefinition
{
    public Type ClrType => clrType;
    public string Name { get; set; } = name;
    public PromptHint? Hint { get; set; } = hint;
}

public class ObjectType(Type clrType, string name, IEnumerable<PropertyDefinition> properties, PromptHint? hint = null)
    : ITypeDefinition
{
    public Type ClrType => clrType;
    public string Name { get; set; } = name;
    public IEnumerable<PropertyDefinition> Properties { get; } = properties;
    public PromptHint? Hint { get; set; } = hint;
}

public class ArrayType(
    Type clrType,
    ITypeDefinition elementType,
    PromptHint? hint = null,
    PromptHint? elementHint = null) : ITypeDefinition
{
    public Type ClrType => clrType;
    public string Name { get; set; } = "Array";
    public ITypeDefinition ElementType { get; } = elementType;
    public PromptHint? Hint { get; set; } = hint;
    public PromptHint? ElementHint { get; set; } = elementHint;
}

public class MapType(Type clrType, ITypeDefinition valueType, PromptHint? hint = null, PromptHint? valueHint = null)
    : ITypeDefinition
{
    public Type ClrType => clrType;
    public string Name { get; set; } = "Map";
    public ITypeDefinition ValueType { get; } = valueType;
    public PromptHint? Hint { get; set; } = hint;
    public PromptHint? ValueHint { get; set; } = valueHint;
}
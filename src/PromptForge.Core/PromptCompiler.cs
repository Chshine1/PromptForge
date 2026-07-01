using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using PromptForge.Abstractions;
using PromptForge.Abstractions.Model;
using ObjectType = PromptForge.Abstractions.Model.ObjectType;

namespace PromptForge.Core;

public partial class PromptCompiler : IPromptCompiler
{
    private static readonly Regex placeholderRegex = PlaceHolderRegex();

    public IPromptTemplate<T> Compile<T>(PromptContract contract, IMetadataScope scope)
    {
        var parts = new List<Func<T, string>>();
        var staticParts = new List<string>();

        var template = contract.PromptTemplate;
        var lastIndex = 0;
        foreach (Match m in placeholderRegex.Matches(template))
        {
            if (m.Index > lastIndex)
                staticParts.Add(template.Substring(lastIndex, m.Index - lastIndex));

            if (m.Groups["property"].Success)
            {
                // {{schema:input:FieldName}}
                var propertyName = m.Groups["property"].Value;
                var property = contract.InputType.Properties.First(p => p.Name == propertyName);
                var schemaText = CompileInput(scope, propertyName, property.TypeDefinition, property.Hint);
                staticParts.Add(schemaText);
                parts.Add(_ => string.Empty);
            }
            else if (m.Groups["value"].Success)
            {
                // {{FieldName}} - Runtime
                var fieldName = m.Groups["value"].Value;
                var valueGetter = BuildValueGetter<T>(fieldName);
                parts.Add(valueGetter);
                staticParts.Add(string.Empty);
            }
            else
            {
                // {{schema:output}}
                var outputSchema = CompileOutput(scope, "output", contract.OutputType.ClrType);
                staticParts.Add(outputSchema);
                parts.Add(_ => string.Empty);
            }

            lastIndex = m.Index + m.Length;
        }

        if (lastIndex < template.Length)
            staticParts.Add(template[lastIndex..]);

        return staticParts.Count != parts.Count + 1
            ? throw new InvalidOperationException("Internal error: segment count mismatch.")
            : new PromptTemplate<T>(staticParts, parts);
    }

    private Func<T, string> BuildValueGetter<T>(string fieldName)
    {
        var property = typeof(T).GetProperty(fieldName,
            BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
        if (property == null)
            throw new ArgumentException($"Property '{fieldName}' not found on type '{typeof(T).Name}'.");

        var target = Expression.Parameter(typeof(T), "target");
        Expression body = Expression.Property(target, property);

        if (property.PropertyType != typeof(string))
        {
            var toStringMethod = property.PropertyType.GetMethod("ToString", Type.EmptyTypes);
            // ReSharper disable once NullableWarningSuppressionIsUsed
            body = Expression.Call(body, toStringMethod != null
                ? toStringMethod
                : typeof(Convert).GetMethod("ToString", [typeof(object)])!);
        }

        if (!property.PropertyType.IsValueType || Nullable.GetUnderlyingType(property.PropertyType) != null)
        {
            var nullExp = Expression.Constant(null, property.PropertyType);
            var equalsNull = Expression.Equal(body, nullExp);
            body = Expression.Condition(equalsNull, Expression.Constant(string.Empty), body);
        }

        var lambda = Expression.Lambda<Func<T, string>>(body, target);
        return lambda.Compile();
    }

    private static string CompileInput(IMetadataScope scope, string propertyName, Type clrType, PromptHint? propertyHint,
        int depth = 0)
    {
        var builder = new StringBuilder();
        var type = scope[clrType] ?? throw new InvalidOperationException($"Type {clrType} is not registered.");

        var propertySemantic = propertyHint?.Semantic != null ? $": {propertyHint.Semantic}" : "";
        var typeSemanticHint = type.Hint?.Semantic != null ? $", {type.Hint.Semantic}" : "";
        var formatHint = propertyHint?.Format != null ? $", format: {propertyHint.Format}" :
            type.Hint?.Format != null ? $", format: {type.Hint.Format}" : "";
        var hasTypeInfo = typeSemanticHint != "" || formatHint != "" || type is ArrayType || type is MapType;

        var typeInfo = hasTypeInfo ? $" (a {type.Name}{typeSemanticHint}{formatHint})" : "";

        builder.Append(new string(' ', depth * 2));
        builder.Append($"{propertyName}{propertySemantic}{typeInfo}:");
        builder.AppendLine();

        depth++;
        switch (type)
        {
            case ArrayType arrayType:
                builder.Append(
                    CompileInput(scope, "each element", arrayType.ElementType, arrayType.ElementHint, depth));
                break;
            case MapType mapType:
                builder.Append(
                    CompileInput(scope, "each value", mapType.ValueType, mapType.ValueHint, depth));
                break;
            case ObjectType objectType:
                foreach (var property in objectType.Properties)
                {
                    builder.Append(
                        CompileInput(scope, property.Name, property.TypeDefinition, property.Hint, depth));
                }

                break;
            case SimpleType:
                break;
            default:
                throw new NotSupportedException($"The type {type} is not supported.");
        }

        return builder.ToString();
    }

    private static string CompileOutput(IMetadataScope scope, string propertyName, Type clrType, PromptHint? propertyHint = null,
        int depth = 0)
    {
        var builder = new StringBuilder();
        var type = scope[clrType] ?? throw new InvalidOperationException($"Type {clrType} is not registered.");
        
        var propertyPurpose = propertyHint?.Purpose != null ? $": {propertyHint.Purpose}" : "";
        var formatHint = propertyHint?.Format != null ? $", format: {propertyHint.Format}" :
            type.Hint?.Format != null ? $", format: {type.Hint.Format}" : "";
        var constraintHint = type.Hint?.Constraint != null || propertyHint?.Constraint != null
            ? $", {MergeConstraints(type.Hint?.Constraint, propertyHint?.Constraint)}"
            : "";

        var typeInfo = $" (a {type.Name}{formatHint}{constraintHint})";

        builder.Append(new string(' ', depth * 2));
        builder.Append($"{propertyName}{propertyPurpose}{typeInfo}:");
        builder.AppendLine();

        depth++;
        switch (type)
        {
            case ArrayType arrayType:
                builder.Append(
                    CompileOutput(scope, "each element", arrayType.ElementType, arrayType.ElementHint, depth));
                break;
            case MapType mapType:
                builder.Append(
                    CompileOutput(scope, "each value", mapType.ValueType, mapType.ValueHint, depth));
                break;
            case ObjectType objectType:
                foreach (var property in objectType.Properties)
                {
                    builder.Append(
                        CompileOutput(scope, property.Name, property.TypeDefinition, property.Hint, depth));
                }

                break;
            case SimpleType:
                break;
            default:
                throw new NotSupportedException($"The type {clrType.Name} is not supported.");
        }

        return builder.ToString();
    }

    private static string MergeConstraints(string? c1, string? c2)
    {
        if (c1 != null)
        {
            return c2 != null ? $"{c1} && {c2}" : c1;
        }

        return c2 ?? throw new InvalidOperationException();
    }

    [GeneratedRegex(@"\{\{(schema:input:(?<property>[A-Za-z]+)|schema:output|(?<value>[A-Za-z]+))\}\}",
        RegexOptions.Compiled)]
    private static partial Regex PlaceHolderRegex();
}

internal class PromptTemplate<T>(List<string> staticParts, List<Func<T, string>> valueGetters)
    : IPromptTemplate<T>
{
    private readonly string[] _staticParts = staticParts.ToArray();
    private readonly Func<T, string>[] _valueGetters = valueGetters.ToArray();

    public string Resolve(T data)
    {
        var sb = new StringBuilder();
        for (var i = 0; i < _valueGetters.Length; i++)
        {
            sb.Append(_staticParts[i]);
            sb.Append(_valueGetters[i](data));
        }

        sb.Append(_staticParts[^1]);
        return sb.ToString();
    }
}
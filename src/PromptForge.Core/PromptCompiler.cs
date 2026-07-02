using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using PromptForge.Abstractions;
using PromptForge.Abstractions.Metadata;
using PromptForge.Abstractions.Model;
using PromptForge.Abstractions.Serialization;
using ObjectType = PromptForge.Abstractions.Model.ObjectType;

namespace PromptForge.Core;

public partial class PromptCompiler(ISerializerFactory serializerFactory) : IPromptCompiler
{
    private static readonly Regex placeholderRegex = PlaceHolderRegex();

    public IPromptPipeline<TIn, TOut> Compile<TIn, TOut>(ILlmInvoker llmInvoker, string template, IMetadataScope scope)
        where TIn : notnull where TOut : notnull
    {
        var parts = new List<Func<TIn, string>>();
        var staticParts = new List<string>();

        var inputTypeDefinition =
            GetTypeDefinition(typeof(TIn), scope) as ObjectType
            ?? throw new InvalidOperationException($"Type {typeof(TIn).Name} is not registered as an object type.");
        var outputTypeDefinition = GetTypeDefinition(typeof(TOut), scope);

        var lastIndex = 0;
        var serializer = serializerFactory.Create(scope.SerializationConfigs);
        foreach (Match m in placeholderRegex.Matches(template))
        {
            if (m.Index > lastIndex)
                staticParts.Add(template.Substring(lastIndex, m.Index - lastIndex));

            if (m.Groups["property"].Success)
            {
                // {{schema:input:FieldName}}
                var propertyName = m.Groups["property"].Value;
                var property = inputTypeDefinition.Properties.First(p => p.Name == propertyName);
                var schemaText = CompileInput(scope, propertyName, inputTypeDefinition, property.Hint);
                staticParts.Add(schemaText);
                parts.Add(_ => string.Empty);
            }
            else if (m.Groups["value"].Success)
            {
                // {{FieldName}} - Runtime
                var fieldName = m.Groups["value"].Value;
                var valueGetter = BuildValueGetter<TIn>(fieldName, serializer);
                parts.Add(valueGetter);
                staticParts.Add(string.Empty);
            }
            else
            {
                // {{schema:output}}
                var outputSchema = CompileOutput(scope, "output", outputTypeDefinition);
                staticParts.Add(outputSchema);
                parts.Add(_ => string.Empty);
            }

            lastIndex = m.Index + m.Length;
        }

        if (lastIndex < template.Length)
            staticParts.Add(template[lastIndex..]);

        return staticParts.Count != parts.Count + 1
            ? throw new InvalidOperationException("Internal error: segment count mismatch.")
            : new PromptPipeline<TIn, TOut>(staticParts, parts, llmInvoker, serializer);
    }

    private static Func<T, string> BuildValueGetter<T>(string fieldName, ISerializer serializer)
    {
        var prop = typeof(T).GetProperty(fieldName,
            BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
        if (prop == null)
            throw new ArgumentException($"Property '{fieldName}' not found on type '{typeof(T).Name}'.");

        var target = Expression.Parameter(typeof(T), "target");
        // ReSharper disable once NullableWarningSuppressionIsUsed
        var method = typeof(ISerializer).GetMethod(nameof(ISerializer.SerializePropertyValue))!;
        var serializerExp = Expression.Constant(serializer);
        var fieldNameExp = Expression.Constant(fieldName);
        var ownerTypeExp = Expression.Constant(typeof(T));

        var call = Expression.Call(serializerExp, method,
            Expression.Convert(target, typeof(object)),
            fieldNameExp,
            ownerTypeExp);

        return Expression.Lambda<Func<T, string>>(call, target).Compile();
    }

    private static string CompileInput(IMetadataScope scope, string propertyName, ITypeDefinition type,
        PromptHint? propertyHint,
        int depth = 0)
    {
        var builder = new StringBuilder();

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
                builder.Append(CompileInput(
                    scope, "each element",
                    GetTypeDefinition(arrayType.ElementType, scope),
                    arrayType.ElementHint,
                    depth));
                break;
            case MapType mapType:
                builder.Append(CompileInput(
                    scope, "each value",
                    GetTypeDefinition(mapType.ValueType, scope),
                    mapType.ValueHint,
                    depth));
                break;
            case ObjectType objectType:
                foreach (var property in objectType.Properties)
                    builder.Append(CompileInput(
                        scope, property.Name,
                        GetTypeDefinition(property.TypeDefinition, scope),
                        property.Hint,
                        depth));

                break;
            case SimpleType:
                break;
            default:
                throw new NotSupportedException($"The type {type} is not supported.");
        }

        return builder.ToString();
    }

    private static string CompileOutput(IMetadataScope scope, string propertyName, ITypeDefinition type,
        PromptHint? propertyHint = null,
        int depth = 0)
    {
        var builder = new StringBuilder();

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
                builder.Append(CompileOutput(
                    scope, "each element",
                    GetTypeDefinition(arrayType.ElementType, scope),
                    arrayType.ElementHint,
                    depth));
                break;
            case MapType mapType:
                builder.Append(CompileOutput(
                    scope, "each value",
                    GetTypeDefinition(mapType.ValueType, scope),
                    mapType.ValueHint,
                    depth));
                break;
            case ObjectType objectType:
                foreach (var property in objectType.Properties)
                    builder.Append(CompileOutput(scope, property.Name,
                        GetTypeDefinition(property.TypeDefinition, scope),
                        property.Hint,
                        depth));

                break;
            case SimpleType:
                break;
            default:
                throw new NotSupportedException($"The type {type.GetType().Name} is not supported.");
        }

        return builder.ToString();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ITypeDefinition GetTypeDefinition(Type clrType, IMetadataScope scope)
    {
        return scope[clrType] ?? throw new IndexOutOfRangeException();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static string MergeConstraints(string? c1, string? c2)
    {
        if (c1 != null) return c2 != null ? $"{c1} && {c2}" : c1;

        return c2 ?? throw new InvalidOperationException();
    }

    [GeneratedRegex(@"\{\{(schema:input:(?<property>[A-Za-z]+)|schema:output|(?<value>[A-Za-z]+))\}\}",
        RegexOptions.Compiled)]
    private static partial Regex PlaceHolderRegex();
}
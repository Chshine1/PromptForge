using System.Collections.Immutable;
using System.Reflection;
using PromptForge.Abstractions.Attributes.Hints;
using PromptForge.Abstractions.Enums;
using PromptForge.Abstractions.Model;

namespace PromptForge.Core.Metadata.Registry;

public readonly struct RegisterResult
{
    public required ITypeDefinition Type { get; init; }
    public required ImmutableHashSet<Type> TypeOccurrences { get; init; }
}

public static partial class TypeMetadataRegistry
{
    private static readonly Dictionary<Type, RegisterResult> clrToTypeDefinitions = new();

    public static ITypeDefinition GetDefinitionFromClrType(Type clrType)
    {
        return clrToTypeDefinitions[clrType].Type;
    }

    public static RegisterResult RegisterClrType(Type clrType)
    {
        if (clrToTypeDefinitions.TryGetValue(clrType, out var cached)) return cached;

        var typeHints = HintAttributes.CollectFromType(clrType);
        var typeHint = typeHints.BuildHint(HintTarget.TypeAndProperty);

        // ReSharper disable InvertIf
        if (TryBuildSimpleResult(clrType, typeHint, out var simpleResult))
        {
            clrToTypeDefinitions[clrType] = simpleResult;
            return simpleResult;
        }

        var genericInterfaces = GetSupportedGenericInterfaces(clrType);

        if (TryBuildMapResult(clrType, genericInterfaces, typeHints, out var mapResult))
        {
            clrToTypeDefinitions[clrType] = mapResult;
            return mapResult;
        }

        if (TryBuildArrayResult(clrType, genericInterfaces, typeHints, out var arrayResult))
        {
            clrToTypeDefinitions[clrType] = arrayResult;
            return arrayResult;
        }

        if (TryBuildObjectResult(clrType, typeHint, out var objectResult))
        {
            clrToTypeDefinitions[clrType] = objectResult;
            return objectResult;
        }
        // ReSharper restore InvertIf

        throw new InvalidOperationException();
    }

    private static ImmutableArray<Type> GetSupportedGenericInterfaces(Type clrType)
    {
        var interfaces = clrType.IsInterface
            ? clrType.GetInterfaces().Concat([clrType])
            : clrType.GetInterfaces();

        return
        [
            ..interfaces
                .Where(i =>
                    i.IsGenericType &&
                    (i.GetGenericTypeDefinition() == typeof(IDictionary<,>)
                     || i.GetGenericTypeDefinition() == typeof(IReadOnlyDictionary<,>)
                     || i.GetGenericTypeDefinition() == typeof(IEnumerable<>)))
        ];
    }

    private readonly struct HintAttributes
    {
        private readonly ImmutableArray<InputHintAttribute> _inputHints;
        private readonly ImmutableArray<OutputHintAttribute> _outputHints;
        private readonly ImmutableArray<FormatHintAttribute> _formatHints;

        private HintAttributes(
            ImmutableArray<InputHintAttribute> inputHints,
            ImmutableArray<OutputHintAttribute> outputHints,
            ImmutableArray<FormatHintAttribute> formatHints)
        {
            _inputHints = inputHints;
            _outputHints = outputHints;
            _formatHints = formatHints;
        }

        public static HintAttributes CollectFromType(Type type)
        {
            return new HintAttributes(
                [.. type.GetCustomAttributes<InputHintAttribute>()],
                [.. type.GetCustomAttributes<OutputHintAttribute>()],
                [.. type.GetCustomAttributes<FormatHintAttribute>()]);
        }

        public static HintAttributes CollectFromMember(MemberInfo member)
        {
            return new HintAttributes(
                [.. member.GetCustomAttributes<InputHintAttribute>()],
                [.. member.GetCustomAttributes<OutputHintAttribute>()],
                [.. member.GetCustomAttributes<FormatHintAttribute>()]);
        }

        public PromptHint? BuildHint(HintTarget target)
        {
            var input = _inputHints.FirstOrDefault(a => a.Target == target);
            var output = _outputHints.FirstOrDefault(a => a.Target == target);
            var format = _formatHints.FirstOrDefault(a => a.Target == target);

            PromptHint? hint = null;
            if (input is not null) hint = new PromptHint(input.Semantic);

            if (output is not null)
            {
                if (hint is null) hint = new PromptHint(Purpose: output.Purpose, Constraint: output.Constraint);
                else hint = hint with { Purpose = output.Purpose, Constraint = output.Constraint };
            }

            if (format is null) return hint;
            if (hint is null) hint = new PromptHint(Format: format.Format);
            else hint = hint with { Format = format.Format };

            return hint;
        }
    }
}
using System.Collections.Immutable;
using PromptForge.Abstractions.Enums;
using PromptForge.Abstractions.Model;

namespace PromptForge.Core.Metadata.Registry;

public static partial class TypeMetadataRegistry
{
    private static bool TryBuildArrayResult(
        Type clrType,
        ImmutableArray<Type> interfaces,
        HintAttributes typeHints,
        out RegisterResult result)
    {
        var enumInterface = interfaces.FirstOrDefault(i =>
            i.GetGenericTypeDefinition() == typeof(IEnumerable<>));

        if (enumInterface is null)
        {
            result = default;
            return false;
        }

        var args = enumInterface.GetGenericArguments();
        if (args.Length != 1) throw new InvalidOperationException();

        var elementResult = RegisterClrType(args[0]);
        var hint = typeHints.BuildHint(HintTarget.TypeAndProperty);
        var elementHint = typeHints.BuildHint(HintTarget.ArrayElement);

        result = new RegisterResult
        {
            Type = new ArrayType(args[0], hint, elementHint),
            TypeOccurrences = elementResult.TypeOccurrences.Append(clrType).ToImmutableHashSet()
        };
        return true;
    }
}
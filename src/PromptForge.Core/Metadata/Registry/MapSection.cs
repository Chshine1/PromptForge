using System.Collections.Immutable;
using PromptForge.Abstractions.Enums;
using PromptForge.Abstractions.Model;

namespace PromptForge.Core.Metadata.Registry;

public static partial class TypeMetadataRegistry
{
    private static bool TryBuildMapResult(
        Type clrType,
        ImmutableArray<Type> interfaces,
        HintAttributes typeHints,
        out RegisterResult result)
    {
        var dictInterface = interfaces.FirstOrDefault(i =>
            i.GetGenericTypeDefinition() == typeof(IDictionary<,>) ||
            i.GetGenericTypeDefinition() == typeof(IReadOnlyDictionary<,>));

        if (dictInterface is null)
        {
            result = default;
            return false;
        }

        var args = dictInterface.GetGenericArguments();
        if (args.Length != 2 || args[0] != typeof(string))
            throw new InvalidOperationException();

        var valueResult = RegisterClrType(args[1]);
        var hint = typeHints.BuildHint(HintTarget.TypeAndProperty);
        var valueHint = typeHints.BuildHint(HintTarget.MapValue);

        result = new RegisterResult
        {
            Type = new MapType(args[1], hint, valueHint),
            TypeOccurrences = [.. valueResult.TypeOccurrences, clrType]
        };
        return true;
    }
}
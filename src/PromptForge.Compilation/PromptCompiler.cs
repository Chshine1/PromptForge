using System.Text;
using PromptForge.Core.Model;

namespace PromptForge.Compilation;

public static class PromptCompiler
{
    public static string GenerateTypeDescription(ITypeDefinition typeDef, string role,
        string? serializationDescription = null, string? deserializationDescription = null)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"{role} type: {typeDef.Name}");

        if (!string.IsNullOrEmpty(typeDef.Hint?.Semantic))
            sb.AppendLine($"  Description: {typeDef.Hint?.Semantic}");

        if (!string.IsNullOrEmpty(serializationDescription))
            sb.AppendLine($"  Input format: {serializationDescription}");

        if (!string.IsNullOrEmpty(deserializationDescription))
            sb.AppendLine($"  Output format: {deserializationDescription}");

        RenderTypeDetails(typeDef, sb, "  ");

        return sb.ToString();
    }

    private static void RenderTypeDetails(ITypeDefinition typeDef, StringBuilder sb, string indent)
    {
        throw new NotImplementedException();
    }
}
using PromptForge.Abstractions.Model;

namespace PromptForge.Abstractions.Metadata;

public readonly record struct TypeMetadata(ITypeDefinition TypeDefinition, SerializationConfig Config);

public interface IMetadataScope
{
    TypeMetadata? this[Type type] { get; }
}
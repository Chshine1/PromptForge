using System.Collections.Immutable;
using PromptForge.Abstractions.Metadata;

namespace PromptForge.Core.Metadata;

public class MetadataScope(ImmutableDictionary<Type, TypeMetadata> typeDefinitions) : IMetadataScope
{
    public TypeMetadata? this[Type type] => typeDefinitions.GetValueOrDefault(type);
}
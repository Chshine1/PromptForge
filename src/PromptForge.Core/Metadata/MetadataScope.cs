using System.Collections.Immutable;
using PromptForge.Abstractions.Metadata;
using PromptForge.Abstractions.Model;
using PromptForge.Abstractions.Serialization;

namespace PromptForge.Core.Metadata;

public readonly record struct TypeMetadata(ITypeDefinition TypeDefinition, SerializationConfig SerializationConfig);

public class MetadataScope(ImmutableDictionary<Type, TypeMetadata> metadata) : IMetadataScope
{
    public ITypeDefinition? this[Type type] => metadata.GetValueOrDefault(type).TypeDefinition;

    public ImmutableDictionary<Type, SerializationConfig> SerializationConfigs { get; } = metadata
        .Select(kvp => new KeyValuePair<Type, SerializationConfig>(kvp.Key, kvp.Value.SerializationConfig))
        .ToImmutableDictionary();
}
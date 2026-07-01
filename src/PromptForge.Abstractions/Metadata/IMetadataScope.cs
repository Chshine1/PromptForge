using System.Collections.Immutable;
using PromptForge.Abstractions.Model;
using PromptForge.Abstractions.Serialization;

namespace PromptForge.Abstractions.Metadata;

public interface IMetadataScope
{
    ITypeDefinition? this[Type type] { get; }
    ImmutableDictionary<Type, SerializationConfig> SerializationConfigs { get; }
}
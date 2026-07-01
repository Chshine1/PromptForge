using System.Collections.Immutable;
using PromptForge.Abstractions;
using PromptForge.Abstractions.Model;

namespace PromptForge.Core.Metadata;

public class MetadataScope(ImmutableDictionary<Type, ITypeDefinition> typeDefinitions) : IMetadataScope
{
    public ITypeDefinition? this[Type type] => typeDefinitions.GetValueOrDefault(type);
}
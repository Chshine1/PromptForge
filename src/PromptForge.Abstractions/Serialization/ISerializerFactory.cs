using System.Collections.Immutable;

namespace PromptForge.Abstractions.Serialization;

public interface ISerializerFactory
{
    ISerializer Create(ImmutableDictionary<Type, SerializationConfig> configs);
}
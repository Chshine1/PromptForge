using System.Collections.Immutable;
using PromptForge.Abstractions.Serialization;

namespace PromptForge.Core.Serialization;

public class SerializerFactory : ISerializerFactory
{
    public ISerializer Create(ImmutableDictionary<Type, SerializationConfig> configs)
    {
        return new Serializer(configs);
    }
}
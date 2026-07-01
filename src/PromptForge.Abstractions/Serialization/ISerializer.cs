using JetBrains.Annotations;

namespace PromptForge.Abstractions.Serialization;

public interface ISerializer
{
    [UsedImplicitly(ImplicitUseKindFlags.Access)]
    string Serialize<T>(T value) where T : notnull;

    [UsedImplicitly(ImplicitUseKindFlags.Access)]
    T? Deserialize<T>(string value) where T : new();

    string SerializePropertyValue(object owner, string propertyName, Type ownerType);
}
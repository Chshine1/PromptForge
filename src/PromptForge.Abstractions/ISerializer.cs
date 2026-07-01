namespace PromptForge.Abstractions;

public interface ISerializer
{
    string Serialize<T>(T value) where T : notnull;
    T Deserialize<T>(string value) where T : new();
}
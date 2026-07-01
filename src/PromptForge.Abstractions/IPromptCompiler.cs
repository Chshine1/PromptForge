using PromptForge.Abstractions.Model;

namespace PromptForge.Abstractions;

public interface IPromptCompiler
{
    IPromptTemplate<T> Compile<T>(PromptContract contract);
    void SetSerialization(ISerializer serializer);
}
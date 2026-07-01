using PromptForge.Abstractions;
using PromptForge.Core.Model;

namespace PromptForge.Core.Compilation;

public interface IPromptCompiler
{
    IPromptTemplate<T> Compile<T>(PromptContract contract);
    void SetSerialization(ISerializer serializer);
}
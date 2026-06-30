using PromptForge.Core;
using PromptForge.Core.Model;

namespace PromptForge.Compilation;

public interface IPromptCompiler
{
    IPromptTemplate<T> Compile<T>(PromptContract contract);
    void SetSerialization(ISerializer serializer);
}
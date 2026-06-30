using PromptForge.Core.Model;

namespace PromptForge.Compilation;

public interface IPromptCompiler
{
    IFillable<T> Compile<T>(PromptContract contract);
}
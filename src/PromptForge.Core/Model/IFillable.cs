using System.Text;

namespace PromptForge.Core.Model;

public interface IFillable<in T>
{
    string Fill(T data);
}
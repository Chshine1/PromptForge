using PromptForge.Abstractions.Model;

namespace PromptForge.Abstractions.Metadata;

public record PropertyOverride(PromptHint? Hint)
{
    public PropertyOverride WithOther(PropertyOverride? other)
    {
        return other is null ? this : new PropertyOverride(Hint is not null ? Hint.WithOther(other.Hint) : other.Hint);
    }
}
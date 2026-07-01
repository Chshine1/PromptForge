namespace PromptForge.Abstractions.Model;

public record PropertyDefinition(
    string Name,
    Type TypeDefinition,
    PromptHint? Hint = null)
{
    public PropertyDefinition OverrideWith(PropertyOverride @override)
    {
        return this with { Hint = Hint is not null ? Hint.WithOther(@override.Hint) : @override.Hint };
    }
}
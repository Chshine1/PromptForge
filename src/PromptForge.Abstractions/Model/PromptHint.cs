namespace PromptForge.Abstractions.Model;

/// <param name="Semantic">for input: explains how to interpret data</param>
/// <param name="Purpose">for output: explains the role in generation</param>
/// <param name="Constraint">for output: explains the constraint</param>
/// <param name="Format">for both: describes representation details</param>
public record PromptHint(
    string? Semantic = null,
    string? Purpose = null,
    string? Constraint = null,
    string? Format = null)
{
    public PromptHint WithOther(PromptHint? other)
    {
        if (other is null) return this;
        return new PromptHint(
            Semantic: string.IsNullOrWhiteSpace(other.Semantic) ? Semantic : other.Semantic,
            Purpose: string.IsNullOrWhiteSpace(other.Purpose) ? Purpose : other.Purpose,
            Constraint: string.IsNullOrWhiteSpace(other.Constraint) ? Constraint : other.Constraint,
            Format: string.IsNullOrWhiteSpace(other.Format) ? Format : other.Format);
    }
}
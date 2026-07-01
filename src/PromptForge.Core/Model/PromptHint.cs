namespace PromptForge.Core.Model;

/// <param name="semantic">for input: explains how to interpret data</param>
/// <param name="purpose">for output: explains the role in generation</param>
/// <param name="constraint">for output: explains the constraint</param>
/// <param name="format">for both: describes representation details</param>
public class PromptHint(
    string? semantic = null,
    string? purpose = null,
    string? constraint = null,
    string? format = null)
{
    public string? Semantic { get; set; } = semantic;
    public string? Purpose { get; set; } = purpose;
    public string? Constraint { get; set; } = constraint;
    public string? Format { get; set; } = format;
}
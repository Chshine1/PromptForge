namespace PromptForge.Core.Model;

/// <summary>
/// 
/// </summary>
/// <param name="Semantic">for input: explains how to interpret data</param>
/// <param name="Purpose">for output: explains the role in generation</param>
/// <param name="Constraint">for output: explains the constraint</param>
/// <param name="Format">for both: describes representation details</param>
public record PromptHint(
    string? Semantic = null,
    string? Purpose = null,
    string? Constraint = null,
    string? Format = null);
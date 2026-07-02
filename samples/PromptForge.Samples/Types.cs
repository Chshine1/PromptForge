using PromptForge.Abstractions.Attributes;
using PromptForge.Abstractions.Attributes.Hints;
using PromptForge.Abstractions.Attributes.Types;
using PromptForge.Abstractions.Enums;

namespace PromptForge.Samples;

[SimpleType]
[InputHint(Semantic = "any JSON object")]
[OutputHint(Purpose = "any JSON object")]
public readonly struct StructData
{
    public object Data { get; init; }
}

[ObjectType]
public class BufferState
{
    [InputHint(Semantic = "id of the module this buffer belongs to")]
    public required string ModuleId { get; init; }

    [InputHint(Semantic = "arbitrary structured data in the buffer")]
    public required StructData Data { get; init; }
}

[ObjectType]
public class ProceduralCondition
{
    [InputHint(Semantic = "a unique identifier")]
    public required string RuleId { get; init; }

    [InputHint(Semantic = "structural condition to match against buffer states")]
    public required StructData Condition { get; init; }

    [InputHint(Semantic = "fuzzy/neural conditions or hints")]
    public required StructData Semantics { get; init; }
}

[ObjectType]
public class EvaluationInput
{
    public required BufferState[] BufferStates { get; init; }
    public required ProceduralCondition[] Conditions { get; init; }
}

[ObjectType]
public class BufferOperation
{
    public required string TargetModuleId { get; init; }

    [OutputHint(Purpose = "name of the command to call", Constraint = "must exist in schema")]
    public required string Command { get; init; }

    [OutputHint(Purpose = "concrete parameters matching the command schema")]
    public required StructData Params { get; init; }
}

[ObjectType]
public class ModuleSchema
{
    public required string ModuleId { get; init; }

    [InputHint(Semantic = "keys are commands' names")]
    [InputHint(Target = HintTarget.MapValue, Semantic = "a JSON Schema-like description of expected parameters")]
    public required Dictionary<string, string> CommandSchemas { get; init; }
}

[ObjectType]
public class NeuroOperation
{
    [PromptIgnore] public required string RuleId { get; init; }

    [InputHint(Semantic =
        "possible structurally incomplete commands, those incomplete ones need implementations to match the schemas")]
    [InputHint(Target = HintTarget.ArrayElement,
        Semantic = "a command's params may be incomplete comparing the given schema")]
    public required BufferOperation[] Commands { get; init; }

    [InputHint(Semantic = "a key provides cases: merging into a command, a direct neuro operation, a ")]
    [InputHint(Target = HintTarget.MapValue,
        Semantic =
            "command case: a neuro expectation of the command's params which may combine with a structural incomplete one. " +
            "neuro case: a complete unconstrained description. " +
            "meta case: metadata relating these neuro info to the structural commands above")]
    [FormatHint(Format = "the key is a string 'command: <command_name>'|'neuro'|'meta'")]
    public required Dictionary<string, StructData> Semantics { get; init; }
}
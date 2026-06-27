## README

**PromptForge** generates LLM system prompts from structured metadata extracted from code, eliminating manual prompt
writing.

### Design

All prompt information originates from a single source of truth, the `AbilityContract`. This contract defines
input/output types, semantic annotations, constraints, serialization syntax, and task instructions. Metadata is
extracted from source code (via Roslyn attributes) or API specifications (OpenAPI, Protobuf) and compiled into a dense,
natural-language system prompt. Compilation is modular: renderers handle sections (role, input, output, constraints),
optimizers compress and refine the final text. JSON Pointer and a path naturalizer translate structural references into
readable constraints.

### Purpose

Make prompt engineering reproducible and maintainable by binding it to code artifacts instead of ad‑hoc text.

### Input DSL (describing data the LLM will receive)

**Goal**: Expose only what the model cannot infer from a JSON instance.

**Syntax outline**:
```
<property-name> [:<property-hint-semantic>] [(<type-info>)] :
```

Where `<type-info>` is built as:
```
("a" | "an") <type-name> [, <type-hint-semantic>] [, format: <format>]
```

**Degradation rules** (applied in order until a stable line is reached):
1. If the property has no hint and the type has no hint, no merged format, and the type is not Array or Map → remove the `(...)` part.
2. If the property has no hint → remove the `:<property-hint-semantic>` and the preceding colon.
3. If both the property hint and the type info part are absent (i.e. the line would be empty) → remove the whole line.
4. Inside `(...)`, if the type has no hint → remove the `<type-hint-semantic>` part.
5. If neither the property nor the type contributes a format → remove the `format:` part.
6. If after all removals the parentheses contain only `an Array` or `a Map` → append `, each element` (for Array) or `, each value` (for Map) right after the type name, inside the parentheses.

**Object / Array / Map expansion**:
- Object: add an indented block recursively listing its properties.
- Array: add an indented line `each element:` followed by the element type’s description.
- Map: add an indented line `each value:` followed by the value type’s description.

**Example** (based on `ModuleState`):
```
ModuleState:
  state_of: the modules which this state is shared amongst (a Bitmask, its instance has many bits which can be set either "0" or "1", format: binary string)
  conditions (an Array):
    each element:
      predicate: a natural language predicate to be evaluated
```

---

### Output DSL (describing data the LLM must generate)

**Goal**: Provide exactly the constraints, purpose and format hints the model needs to produce correct output; type and structure are never omitted because there is no instance to learn from.

**Syntax outline**:
```
<property-name> [:<purpose>] (<type-name> [, <constraints>] [, format: <format>]) :
```
- `purpose` comes from `Hint.Purpose`
- `constraints` are rendered as comma‑separated natural language fragments (e.g. `length between 1 and 50`, `one of: "admin", "user"`, `matching valid email`)
- `format` is optional and describes desired serialization (e.g. `YYYY-MM-DD`, or a composite layout)

**Degradation rules** (simpler, because type is mandatory):
- The type name is always present.
- If a property has no purpose, no constraints and no format, the parentheses contain only the type: `(a string)`.
- A property is **never removed** from the output, even if it has no extra annotations; the field name and type are essential generation instructions.

**Container expansion**:
- Object: indented block with its properties.
- Array: `each element:` with element type and annotations.
- Map: `each value:` with value type and annotations.
- An object may carry an overall `format` clause on its first line, e.g.:
  ```
  UserInfo (format: "<name>"-<age:number>-"<email>"-"<role>"):
  ```

**Example**:
```
UserInfo (format: "<name>"-<age:number>-"<email>"-"<role>"):
  name: the user's real name (a string, length between 1 and 50):
  age: age (a number, between 1 and 150):
  email (a string, matching a valid email):
  role (a string, one of: "admin", "user"):
```

### Usage

```bash
# Extract metadata from C# project
promptforge extract ./src/MyService --output contract.json

# Compile contract into system prompt
promptforge compile contract.json --output prompt.txt
```

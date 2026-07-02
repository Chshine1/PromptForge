# Roadmap

This document outlines the current state and planned evolution of PromptForge.

## v0.0.1 (now)

- [x] Core expression‑tree pipeline compiler
- [x] Fluent builder API (`Create<TIn, TOut>()`)
- [x] Custom type serialization/deserialization
- [x] Schema injection through `InputHint`/`OutputHint` attributes
- [x] Manual template composition
- [x] Basic sample project

### Known gaps / missing pieces (help wanted)

- [ ] Limited to single‑turn text completion; no messages/chat support
- [ ] No built‑in few‑shot example support (plan: `WithExample(TIn, TOut)` API)
- [ ] No support for different llm calls (sync/async/stream)
- [ ] No middleware pipeline (logging, caching, retry)
- [ ] No source generator for AOT/compatibility
- [ ] Minimal documentation and tests

## v0.1.0 (first usable release)

- [ ] `ChatPipeline` for multi‑turn conversations
- [ ] Complete `WithExample` and `WithExamples` fluent extensions
- [ ] Registration of different llm invokers
- [ ] Complete documentation and tests

## Recent plans

- [ ] Basic middleware support (logging, retry)
- [ ] Integration with `Microsoft.Extensions.AI`
- [ ] Stable public API (semantic versioning starts)

## Future Ideas

- Source generator to eliminate runtime expression compilation
- Native Function Calling / Tool Use support
- Prompt optimization plugins (compression, semantic caching)
- Multi‑modal (images, audio) via `StructData`
- .NET integration helpers
- Visual Studio / Rider extension for template editing

Contributions towards any of these items are extremely welcome.
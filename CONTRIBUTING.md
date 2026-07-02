# Contributing to PromptForge

Thank you for your interest in contributing! This document will help you get started.

## Getting Started

1. **Fork and clone** the repository.
2. Install .NET 10 SDK (or later).
3. Open `PromptForge.sln` in your IDE (Visual Studio, Rider, VS Code).
4. Build the solution: `dotnet build`

## Development Workflow

- Create a new branch for your work: `feature/my-feature` or `fix/issue-123`.
- Make changes and add tests for new functionality.
- Ensure all tests pass: `dotnet test`
- Follow existing coding style and XML comments for public APIs.
- Submit a **Pull Request** with a clear description and reference to any related issue.

## Project Structure

- `src/PromptForge.Abstractions` – project abstractions
- `src/PromptForge.Core` – core library
- `tests/PromptForge.Tests` – unit tests
- `samples/PromptForge.Samples` – example usage

## Roadmap & Ideas

Check [ROADMAP.md](ROADMAP.md) for planned features and missing pieces. We especially welcome help with:

- Chat / messages pipelines
- Few‑shot example support
- Middleware and plugin extensibility
- Source generator integration

Feel free to open an issue to discuss any new idea before implementing.

## Code of Conduct

Be respectful, inclusive, and collaborative. Harassment or disrespectful behavior will not be tolerated.

## Questions?

Open an issue or start a discussion.
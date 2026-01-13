# Development Workflow

This document outlines the standard workflow for contributing to the Priority Task Manager project. It is designed to provide context and guidance for developers, including LLM-based coding assistants.

## Guiding Principles

-   **Task-Driven Development.** All work should be guided by a task list. Currently, this is the root `TODO.md` file. For larger teams, this will transition to GitHub Issues.
-   **Architecture First.** Before implementing a new feature, refer to `docs/ARCHITECTURE.md` to understand the existing design patterns and ensure your changes are consistent with the project's structure.
-   **Check the Status.** For a high-level overview of the project's current capabilities and state, refer to `docs/STATUS.md`.
-   **Small, iterative changes.** Prefer small, well-defined commits over large, monolithic ones.

## Working with AI Assistants

When using AI tools (GitHub Copilot, etc.) to contribute to this project:

1.  **Reference Definitions**: Use the terms defined in `ARCHITECTURE.md` (e.g., "Ask the SchedulingAgent to...", not "Ask the thing that puts tasks on the calendar").
2.  **Consult Documentation First**: As per `copilot.instructions.md`, always ask the AI to verify its plan against `ARCHITECTURE.md` and `STATUS.md`.
3.  **Update Documentation**: If you or the AI refactor code, you **must** update the corresponding documentation. The AI is instructed to help with this.
4.  **TDD**: Instruct the AI to write the failing test *before* implementing the logic, enhancing reliability.

## Standard Workflow

1.  **Select a Task**: Choose the highest-priority task from `TODO.md` that has not yet been completed.
2.  **Understand the Goal**: Read the task description and analyze the relevant parts of the codebase. Refer to `docs/ARCHITECTURE.md` and `docs/STATUS.md` to understand how the feature fits into the overall system.
3.  **Implement the Changes**: Write the necessary code, following the established patterns.
4.  **Build and Verify**: Ensure the project builds and runs without errors.
5.  **Update Documentation**: If the changes affect the architecture, workflow, or status, update the relevant `.md` files in the `docs/` folder.
6.  **Update `TODO.md`**: Mark the task as complete or update its status.
7.  **Commit**: Write a clear and concise commit message describing the changes.

## Building and Running the Application

**Prerequisites:**
*   .NET SDK (8.0 or higher)

**Build the solution:**
```bash
dotnet build
```

**Run the CLI application:**
```bash
cd PriorityTaskManager.CLI
dotnet run
```

## Testing

The project `PriorityTaskManager.Tests/` contains the unit tests for the core library.

To run the tests:
```bash
dotnet test
```
**Important Note:** As of this writing, the tests in this project are **outdated** due to significant architectural refactoring. A major task in `TODO.md` is to overhaul and update these tests to reflect the current implementation.

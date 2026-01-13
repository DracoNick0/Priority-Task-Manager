# Priority Task Manager

An intelligent command-line application designed to solve decision fatigue in task management through a sophisticated, agent-based scheduling engine.

This project transforms a simple to-do list into a smart assistant that generates a detailed, optimized schedule by analyzing tasks based on due dates, complexity, and dependencies.

## Documentation

Detailed documentation for developers and users can be found in the `docs/` directory:

*   **[Project Status & Features](docs/STATUS.md)**: Current capabilities, command reference, and known issues.
*   **[Architecture](docs/ARCHITECTURE.md)**: High-level design, agent pipeline (MCP), and core services.
*   **[Development Workflow](docs/WORKFLOW.md)**: How to build, run, and contribute to the project.
*   **[Testing Strategy](docs/TESTING_STRATEGY.md)**: Plan for unit and integration testing.
*   **[Roadmap](docs/TODO.md)**: Active tasks and future improvements.

## Getting Started

**Prerequisites:**
*   .NET SDK (8.0 or higher)

**Build and Run:**
1.  Build the solution:
    ```bash
    dotnet build
    ```
2.  Run the CLI application:
    ```bash
    cd PriorityTaskManager.CLI
    dotnet run
    ```

## Core Architecture

The solution uses a **Multi-Agent Coordination Pattern (MCP)** to separate concerns:

*   **PriorityTaskManager (Core)**: Contains all business logic and the agent pipeline.
*   **PriorityTaskManager.CLI**: Handles user interaction and command parsing.

The scheduling pipeline follows a **Prioritize -> Balance -> Schedule** workflow, breaking down complex scheduling decisions into small, single-responsibility agents. See [ARCHITECTURE.md](docs/ARCHITECTURE.md) for the full diagram and details.

## Contributing

This project follows a structured workflow. Please check [WORKFLOW.md](docs/WORKFLOW.md) before making changes. Use [TODO.md](docs/TODO.md) to pick up available tasks.

---

### **License**

This project is licensed under the MIT License.
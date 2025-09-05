# Copilot Instructions for Priority Task Manager

## Project Overview
Priority Task Manager is a command-line application designed to manage tasks based on the "Upstream Urgency" philosophy. It calculates task urgency dynamically using deadlines, durations, and dependencies. The project is structured into three main components:

1. **Core Library (`PriorityTaskManager`)**: Contains models, services, and logic for task management.
2. **Command-Line Interface (`PriorityTaskManager.CLI`)**: Provides a user interface for interacting with the core library.
3. **Tests (`PriorityTaskManager.Tests`)**: Ensures the correctness of the core library and CLI.

## Key Architectural Concepts
- **Dynamic Urgency Engine**: Implements the "Latest Possible Start Date (LPSD)" calculation to prioritize tasks.
- **Dependency Management**: Prevents circular dependencies and ensures accurate task timelines.
- **Multi-List Organization**: Supports multiple task lists with independent sorting options.

### Code Structure
- **Models** (`PriorityTaskManager/Models`): Defines `TaskItem`, `TaskList`, and `SortOption`.
- **Services** (`PriorityTaskManager/Services`): Contains `TaskManagerService` for core logic.
- **Handlers** (`PriorityTaskManager.CLI/Handlers`): Implements commands like `AddHandler`, `ListHandler`, etc.
- **Utils** (`PriorityTaskManager.CLI/Utils`): Provides helper utilities like `ConsoleInputHelper`.
- **Tests** (`PriorityTaskManager.Tests`): Includes unit tests for `TaskManagerService` and list management.

## Developer Workflows

### Building the Project
Ensure you have .NET SDK 8.0 or higher installed. Run the following commands:
```powershell
cd Priority-Task-Manager
# Build the solution
cd PriorityTaskManager.CLI; dotnet build
```

### Running the Application
Navigate to the CLI project and execute:
```powershell
dotnet run
```

### Running Tests
To validate changes, run all tests:
```powershell
cd PriorityTaskManager.Tests; dotnet test
```

### Debugging
Use the `dotnet` CLI or an IDE like Visual Studio to debug. Set breakpoints in the `Handlers` or `Services` directories for CLI or core logic issues.

## Project-Specific Conventions
- **Command Handlers**: Each CLI command has a corresponding handler in `PriorityTaskManager.CLI/Handlers`. Handlers implement the `ICommandHandler` interface.
- **Task Sorting**: Sorting options (`Default`, `Alphabetical`, `DueDate`, `Id`) are defined in `SortOption`.
- **Dependency Management**: Use `depend add` and `depend remove` commands to manage task dependencies.

## Integration Points
- **External Dependencies**: The project relies on .NET libraries. Ensure compatibility with .NET 8.0.
- **Cross-Component Communication**: The CLI interacts with the core library via `TaskManagerService`.

## Examples
### Adding a Task
```powershell
dotnet run -- add "Write documentation"
```

### Viewing Tasks
```powershell
dotnet run -- list
```

### Managing Dependencies
```powershell
dotnet run -- depend add 2 1
```

## Contribution Guidelines
Follow the steps in the `README_v2.md` under the "Contributing" section. Use clear commit messages and ensure all tests pass before opening a pull request.

## Logging Changes

For every major code change request, Copilot will provide a log entry in markdown format. The log will follow this structure:

```markdown
# Log Entry [Next Log Number]

## User Prompt

[The full prompt the user provided]

### Copilot's Action

[A concise summary of the changes made.]
```

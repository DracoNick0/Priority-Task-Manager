# **Priority Task Manager**

### **Brief Project Overview**

The Priority Task Manager is an intelligent command-line application designed to solve the problem of decision fatigue in task management. The current implementation uses a sophisticated **"Upstream Urgency"** algorithm, which logically calculates task priority based on a time-based analysis of deadlines and dependencies.

The project is built on a forward-looking and extensible architecture, featuring a fully implemented **Model Context Protocol (MCP)** framework for the `cleanup` feature. This foundation was designed to support the project's ultimate vision: to evolve the single-algorithm engine into a **multi-agent coordination system** where a team of specialized software agents cooperate to provide truly context-aware task prioritization.

This document provides an overview of the project's current state, instructions for setup, and a guide to its codebase for peer reviewers.

---

## **Getting Started (For Reviewers)**

This section provides the essential steps to get the application running on your local machine.

**Prerequisites:**
*   .NET SDK (8.0 or higher)

**Setup Instructions:**
1.  Clone the repository:
    ```bash
    git clone https://github.com/DracoNick0/Priority-Task-Manager.git
    ```
2.  Navigate to the solution directory:
    ```bash
    cd Priority-Task-Manager
    ```3.  Build the entire solution:
    ```bash
    dotnet build
    ```
4.  Navigate to the command-line interface project and run the application:
    ```bash
    cd PriorityTaskManager.CLI
    dotnet run
    ```

---

## **Project Structure & File Descriptions**

The solution is organized into three distinct projects to enforce a clean separation of concerns.

| Project / Directory | Description |
| :--- | :--- |
| **`PriorityTaskManager/`** | The **Core Logic** project. Contains all business logic, data models, and services. It is completely independent of any user interface. |
| ↪ `Services/` | Contains the core business logic, including the `TaskManagerService.cs` and the `UrgencyService.cs`. |
| ↪ `MCP/` | Contains the implemented **Model Context Protocol framework** (`IAgent.cs`, `MCPContext.cs`, `MCP.cs`). This is a key area for review. |
| **`PriorityTaskManager.CLI/`** | The **User Interface** project. Responsible for parsing commands and displaying output to the console. |
| ↪ `Handlers/` | Contains a handler class for each main command (e.g., `AddHandler.cs`, `ListHandler.cs`). The `CleanupHandler.cs` is the entry point for the MCP workflow. |
| ↪ `MCP/Agents/Cleanup/` | Contains the individual agent classes that are used by the `cleanup` command's MCP workflow. |
| **`PriorityTaskManager.Tests/`** | The **Unit Testing** project. Contains all xUnit tests that validate the logic in the Core project. |

---

## **Key Implemented Features**

*   **"Upstream Urgency" Engine:** The current prioritization algorithm calculates a task's **Latest Possible Start Date (LPSD)** based on its deadline and the deadlines of any tasks that depend on it.
*   **Dependency Management:** Users can create dependency chains between tasks (`depend add <childId> <parentId>`), which the urgency engine uses to build accurate timelines.
*   **Multi-List Organization:** Users can create and switch between multiple task lists to organize their work into different contexts.
*   **`cleanup` Command (MCP Implementation):** A powerful `cleanup` utility serves as the proof-of-concept for the MCP framework. It safely archives completed tasks, deletes them from the active list, and then re-indexes all remaining tasks and their dependencies in a robust, transparent, and fail-safe workflow.

---

## **Architectural Deep Dive: Model Context Protocol (MCP)**

The MCP is the most advanced architectural pattern in this project and is **fully implemented**. It is a generic framework designed to orchestrate complex workflows by passing a shared `MCPContext` object through a pipeline of independent, single-responsibility agents.

This framework is currently used to power the `cleanup` command, where it turns a dangerous, multi-step operation into a safe and auditable process. This demonstrates its value for transactional workflows.

---

## **Future Vision: The Multi-Agent Prioritization Engine**

The next major phase for this project is to leverage the existing MCP framework to build a truly intelligent, multi-agent prioritization system. This will introduce a new "multi-agent" mode where prioritization is handled by a team of cooperating agents, each analyzing the tasks from a different perspective (e.g., task complexity, user context, long-term goals). This will fulfill the project's ultimate vision of creating a productivity assistant that adapts to the user's unique and changing context.

---

## Command Reference

### Task Commands
| Command | Description |
| :--- | :--- |
| `add <Title>` | Add a new task to the active list. |
| `view <Id>` | View all details of a specific task. |
| `edit <Id> ...` | Edit a task's attributes interactively. |
| `edit <attr> <Id> [val]` | Edit a specific attribute, optionally providing the new value directly. |
| `delete <Id1,Id2,...>` | Delete one or more tasks by ID. |
| `complete <Id1,Id2,...>` | Mark one or more tasks as complete. |
| `uncomplete <Id1,Id2,...>` | Mark one or more tasks as incomplete. |

### List Commands
| Command | Description |
| :--- | :--- |
| `list` | Display tasks in the current active list. |
| `list all` | Show all available lists and which one is active. |
| `list create <Name>` | Create a new task list. |
| `list switch [Name]` | Switch the active list, or open an interactive menu if no name is given. |
| `list sort <Option>` | Set the sort order for the active list (`Default`, `Alphabetical`, `DueDate`, `Id`). |
| `list delete <Name>` | Delete a list and all of its tasks. |

### Dependency Commands
| Command | Description |
| :--- | :--- |
| `depend add <childId> <parentId>` | Make the child task dependent on the parent task. |
| `depend remove <childId> <parentId>` | Remove a dependency link. |

### General Commands
| Command | Description |
| :--- | :--- |
| `help` | Show this help message. |
| `exit` | Exit the application. |

---

### **License**

This project is licensed under the MIT License.
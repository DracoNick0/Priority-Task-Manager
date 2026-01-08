## TODOs

1.  **Refactor Project Structure**
    -   Consolidate all Multi-Agent Coordination Pattern (MCP) components into the `PriorityTaskManager/MCP` directory.
    -   Move agent classes to `MCP/Agents`.
    -   Move `MultiAgentUrgencyStrategy` to `MCP`.
    -   Update all namespaces and references to reflect the new structure.

2.  **Remove Deprecated Code**
    -   Remove the broken and deprecated `SingleAgentStrategy.cs` file.
    -   Remove the `mode` command and `ModeHandler.cs` from the CLI.
    -   Clean up any remaining references in the codebase.

3.  **Fix and Update Tests**
    -   Establish a solid test foundation with mock services.
    -   Write comprehensive tests for `TaskManagerService`.
    -   Write unit tests for each individual agent.
    -   Write integration tests for the full MCP pipeline.

4.  **Fix Dependency Scheduling Logic**
    -   The scheduling engine currently does not correctly respect task dependencies. This needs to be fixed to ensure dependent tasks are always scheduled after their prerequisites.

5.  **Improve Event Scheduling & Command Consistency**
    -   Implement support for repeating events (e.g., daily, weekly).
    -   Make the event creation process more user-friendly.
    -   Standardize command naming conventions across the application (e.g., ensure `delete` is used consistently for all entities instead of a mix of `delete`, `remove`, etc.).

6.  **Improve Settings Interface**
    -   Make the `settings` command more interactive and user-friendly for viewing and changing user profile settings.

7.  **Improve Overall CLI User Experience**
    -   Review all commands to simplify their usage, improve prompts, and provide clearer, more helpful error messages.

---
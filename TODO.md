## TODOs

1. **Temporary: Simulated Date/Time Feature**
	- Add a feature allowing the user to specify a pretend date and time, so the scheduler and CLI behave as if it is that moment.
	- Ensure all scheduling logic and outputs respect the simulated time when set.

2. **Fix Dependency Scheduling Logic**
	- The scheduling engine currently does not correctly respect task dependencies. This needs to be fixed to ensure dependent tasks are always scheduled after their prerequisites.

3. **Fix and update tests**
	- Review, fix, and update all existing tests to ensure they pass and cover new/changed functionality.

4. **Remove SingleAgentStrategy and `mode` command**
	- Remove the broken and deprecated `SingleAgentStrategy.cs` file.
	- Remove the `mode` command and `ModeHandler.cs` from the CLI, as it will be redundant.
	- Clean up any remaining references in the codebase (e.g., in `TaskManagerService`).

5. **Improve Event Scheduling & Command Consistency**
	- Implement support for repeating events (e.g., daily, weekly).
	- Make the event creation process more user-friendly.
	- Standardize command naming conventions across the application (e.g., ensure `delete` is used consistently for all entities instead of a mix of `delete`, `remove`, etc.).

6. **Improve Settings Interface**
	- Make the `settings` command more interactive and user-friendly for viewing and changing user profile settings.

7. **Improve Overall CLI User Experience**
	- Review all commands to simplify their usage, improve prompts, and provide clearer, more helpful error messages.

---
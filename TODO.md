## TODOs

1. **Overdue Task Display**
	- When a task is overdue, display its ID and name to allow user interaction (e.g., mark complete, edit, etc.).

2. **Data File Relocation & Interface**
	- Move `lists.json`, `tasks.json`, `user_profile.json`, and `events.json` to the `PriorityTaskManager` project folder.
	- Implement an interface for data interaction so other UIs can access and modify these files.

3. **LLM Onboarding & Continuity Markdown Files**
	 - Create dedicated markdown files that provide LLMs with:
		 - A summary of current project status and progress
		 - A list of outstanding issues and tasks
		 - Clear instructions on what needs work and what to do next
		 - Guidance on where in the architecture and files to focus
	 - Purpose: Ensure that whenever you switch LLMs or start a new chat, the LLM can immediately understand the project context, priorities, and next steps without manual onboarding.

4. **Event Block Display in CLI**
	- When a task is split around an event, ensure the CLI displays the '=' sign immediately before the event block for clarity.

5. **Task Splitting Across Multiple Events**
	- Ensure that tasks can be split between multiple events, not just a single event, and are displayed correctly in the CLI.
---
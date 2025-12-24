## TODOs

1. **Data File Relocation & Interface**
	- Move `lists.json`, `tasks.json`, `user_profile.json`, and `events.json` to the `PriorityTaskManager` project folder.
	- Implement an interface for data interaction so other UIs can access and modify these files.

2. **LLM Onboarding & Continuity Markdown Files**
	 - Create dedicated markdown files that provide LLMs with:
		 - A summary of current project status and progress
		 - A list of outstanding issues and tasks
		 - Clear instructions on what needs work and what to do next
		 - Guidance on where in the architecture and files to focus
	 - Purpose: Ensure that whenever you switch LLMs or start a new chat, the LLM can immediately understand the project context, priorities, and next steps without manual onboarding.

3. **Task Splitting Across Multiple Events**
	- Ensure that tasks can be split between multiple events, not just a single event, and are displayed correctly in the CLI.
---
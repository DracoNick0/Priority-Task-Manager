## TODOs

1. **Refactor Agent Scheduling Logic to Fix Corruption Bug**
	- **Problem:** The `UserContextAgent` is currently running *after* the `SchedulingAgent` and is incorrectly rewriting the valid, non-contiguous schedule into a contiguous one, causing display bugs.
	- **Solution:**
		- In `MultiAgentUrgencyStrategy.cs`, change the agent execution order so `UserContextAgent` runs **before** `SchedulingAgent`.
		- Rewrite `UserContextAgent`'s logic. Its new role is to **only sort** the master task list that will be fed to the `SchedulingAgent`. It should not modify any `ScheduledParts`. The sort order should be by due date, then by complexity.
		- This ensures the `SchedulingAgent` receives a correctly prioritized list and is the sole authority for creating the final, accurate schedule.

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

4. **Task Splitting Improvements**
	- Revise and improve task splitting logic in general.
	- Ensure that tasks can be split between multiple events, not just a single event, and are displayed correctly in the CLI.

5. **Fix and update tests**
	- Review, fix, and update all existing tests to ensure they pass and cover new/changed functionality.

6. **Temporary: Simulated Date/Time Feature**
	- Add a feature allowing the user to specify a pretend date and time, so the scheduler and CLI behave as if it is that moment.
	- Ensure all scheduling logic and outputs respect the simulated time when set.

7. **Improve Event Adding Commands**
	- Make event adding commands in the CLI more user friendly and intuitive.
	- Simplify input, provide better prompts, and improve error messages for event creation.

---
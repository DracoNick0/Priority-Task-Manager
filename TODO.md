## TODOs

1. **LLM Onboarding & Continuity Markdown Files**
	 - Create dedicated markdown files that provide LLMs with:
		 - A summary of current project status and progress
		 - A list of outstanding issues and tasks
		 - Clear instructions on what needs work and what to do next
		 - Guidance on where in the architecture and files to focus
		 - A high-level overview of how key classes interact, especially across different projects (e.g., how `PriorityTaskManager.CLI` uses services from `PriorityTaskManager`).
	 - Purpose: Ensure that whenever you switch LLMs or start a new chat, the LLM can immediately understand the project context, priorities, and next steps without manual onboarding.

2. **Fix and update tests**
	- Review, fix, and update all existing tests to ensure they pass and cover new/changed functionality.

3. **Temporary: Simulated Date/Time Feature**
	- Add a feature allowing the user to specify a pretend date and time, so the scheduler and CLI behave as if it is that moment.
	- Ensure all scheduling logic and outputs respect the simulated time when set.

5. **Improve Event Adding Commands**
	- Make event adding commands in the CLI more user friendly and intuitive.
	- Simplify input, provide better prompts, and improve error messages for event creation.

---
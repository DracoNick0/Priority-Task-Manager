## **Priority Task Manager: Project Roadmap**

This document outlines the major features and tasks required to complete the "Upstream Urgency" vision for the Priority Task Manager.

### **Epic 1: Advanced Scheduling & Conflict Resolution**
*Goal: Evolve the core engine from a simple sorter into a true scheduler that understands time as a finite resource.*

#### **Feature: Unified Timeline Slotting**
-   [ ] **TDD: Write new tests for timeline slotting.** Create unit tests that define the expected behavior: a standalone task's LPSD should be pushed back if its ideal time slot is already occupied by a higher-priority dependency chain.
-   [ ] **Refactor the Urgency Calculation Engine.** Modify `CalculateUrgencyForAllTasks` to first identify all dependency chains and treat their collective durations as "reserved time blocks" on a timeline.
-   [ ] **Implement "Gap-Based" LPSD Calculation.** For standalone (non-dependent) tasks, modify their LPSD calculation to find the first available "gap" between the reserved time blocks that is large enough to accommodate them.

#### **Feature: Interactive Conflict Resolution**
-   [ ] **Implement Conflict Detection.** In the `AddTask` and `UpdateTask` methods, add logic to detect when a standalone task has no available gaps and would result in a negative slack time (a scheduling conflict).
-   [ ] **Implement "The Nudge" (Level 1 Resolution).**
    -   [ ] Create a service method to identify the best "nudgeable" candidate—a task in a different dependency chain with the highest total project slack.
    -   [ ] Create a new interactive prompt in the CLI to present the conflict to the user and propose the nudge (e.g., "To fit this task, we can delay 'Project X' by 2 hours. Is this okay? (y/n)").
-   [ ] **Implement "Task Fragmentation" (Level 2 Resolution).**
    -   [ ] If a nudge is not possible or is rejected, implement the final fallback.
    -   [ ] Create a new interactive prompt that informs the user the task is too large, states the size of the largest available gap, and asks them to break the task down into a smaller sub-task.

---

### **Epic 2: The "Focus Dashboard" TUI**
*Goal: Transition the application from a traditional scrolling CLI to a modern, managed Text-based User Interface for a more focused and interactive experience.*

#### **Feature: Foundational TUI Architecture**
-   [ ] **Research/Select a TUI Framework (Optional but Recommended).** Investigate libraries like `Terminal.Gui` or `Spectre.Console` to see if they can accelerate TUI development.
-   [ ] **Create a "Screen Manager" / Main Render Loop.** Overhaul `Program.cs` to replace the simple `while` loop with a main application loop that takes ownership of the screen, handles redraws, and manages the overall layout.
-   [ ] **Implement a Persistent Layout.** Design the main screen with a fixed input area at the bottom and dedicated panels for the other UI components.

#### **Feature: Dashboard Component Implementation**
-   [ ] **Build the "Up Next" Queue Panel.** This panel should fetch the top 3-5 most urgent tasks from the service and render them in a list that can be navigated with up/down arrow keys.
-   [ ] **Build the "Project Health Indicators" Panel.**
    -   [ ] Create a new service method to calculate the total slack time for an entire dependency chain.
    -   [ ] Create the UI component to render the project name and the color-coded slack bar for each major project.
-   [ ] **Build the "Contextual Timeline" Panel.** This panel should listen for the highlighted task in the "Up Next" queue and dynamically render a "micro-Gantt" view showing the preceding task, the current task, and the succeeding task.
-   [ ] **Make the UI Window-Size Aware.** Add logic to the main render loop to detect console resize events and adjust the layout of the panels accordingly.

---

### **Epic 3: Command & UX Enhancements**
*Goal: Iteratively improve the existing command-based workflow for power users.*

#### **Feature: Streamlined Commands**
-   [ ] **Enhance Task Creation/Editing.** Rework the `add` and `edit` commands to use an interactive, arrow-key based menu for selecting and changing specific attributes, rather than a linear series of prompts.
-   [ ] **Improve Dependency Management.** Make the `depend add` command more user-friendly, perhaps by presenting a navigable list of potential parent tasks to choose from.

#### **Feature: Efficiency Improvements**
-   [ ] **Implement Command Abbreviations/Aliases.** Modify the command handler dictionary in `Program.cs` to allow for shortened commands (e.g., `l` for `list`, `a` for `add`).
-   [ ] **Implement Quoted Argument Parsing.** Enhance the command parser in `Program.cs` to treat text enclosed in `"` or `'` as a single argument, allowing for task titles with spaces to be set directly (e.g., `add "My new task"`).
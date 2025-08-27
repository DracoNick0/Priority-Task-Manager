### **Project Summary: Priority Task Manager**

**1. Project Vision**

To develop an intelligent task management application that prioritizes tasks not just by user-set importance, but through a dynamic calculation based on multiple attributes. The system will help users focus on the most critical tasks by providing clear, context-aware organization and sorting capabilities. The initial implementation will be a Command-Line Interface (CLI) application in C#, with a potential future evolution into a cross-platform Flutter application.

**2. Core Features**

*   **Task Management (CRUD):** Users will have the ability to perform fundamental task operations:
    *   **Create:** Add new tasks with detailed attributes.
    *   **Edit:** Modify the attributes of existing tasks.
    *   **Complete:** Mark tasks as finished.
    *   **Delete:** Remove tasks.

*   **Task Lists & Sorting:** Tasks will be organized within lists that can be sorted and viewed in multiple ways to enhance user workflow:
    *   Alphabetically
    *   By Task ID/Index
    *   By any specific task attribute (e.g., Due Date, Difficulty)
    *   By a calculated overall priority score.

**3. Task Attributes & Priority Calculation**

Each task will be defined by a rich set of attributes that feed into the priority system:

*   **Core Attributes:**
    *   **Importance:** User-defined significance of the task.
    *   **Due Date:** The deadline for task completion.
    *   **Estimated Duration:** The expected time required to complete the task.
    *   **Progress:** The current completion status of the task (e.g., a percentage).
    *   **Difficulty:** The level of effort or complexity involved.
    *   **Location Requirement:** Any physical location constraint for the task.
    *   **Task Dependency:** A list of prerequisite tasks that must be completed first.
    *   **Motivation:** (Optional) The user's willingness or desire to work on the task.

*   **Calculated Attributes:**
    *   **Urgency:** A dynamically calculated value based on the **Due Date**, **Estimated Duration**, and **Progress**. A task becomes more urgent as the deadline approaches with insufficient progress.
    *   **Priority:** The primary feature of the application. This will be a calculated score derived from a combination of the attributes above, providing an intelligent and objective measure of what to work on next.

**4. Stretch Goal: Calendar Integration**

To elevate the application from a simple task list to a comprehensive life planner, a calendar integration feature is planned:

*   **Schedule Awareness:** The system will account for the user's existing schedule (meetings, appointments) to understand true availability.
*   **Contextual Task Suggestion:** The application will intelligently prioritize tasks based on the user's state. For example:
    *   Assign high-difficulty tasks during blocks of high energy/focus time.
    *   Suggest easier, low-motivation tasks during periods of low energy.
    *   Account for interruptions and their impact on workflow and task duration.

---

### **Setup Instructions**

**Prerequisites:**
- .NET SDK 8.0 or higher installed on your system.
- A terminal or command prompt to run commands.

**Steps to Setup:**
1. Clone the repository:
   ```bash
   git clone https://github.com/DracoNick0/Priority-Task-Manager.git
   ```
2. Navigate to the project directory:
   ```bash
   cd Priority-Task-Manager
   ```
3. Restore dependencies:
   ```bash
   dotnet restore
   ```

**Steps to Run the CLI Application:**
1. Navigate to the CLI project directory:
   ```bash
   cd PriorityTaskManager.CLI
   ```
2. Run the application:
   ```bash
   dotnet run
   ```

**Available Commands:**
- `add <Title>`: Add a new task (prompts for details).
- `list`: List all tasks sorted by urgency.
- `edit <Id>`: Edit a task by Id.
- `delete <Id>`: Delete a task by Id.
- `complete <Id>`: Mark a task as complete.
- `help`: Show the help message.
- `exit`: Exit the application.

---

### **Contributing**

Contributions are welcome! Please follow these steps:
1. Fork the repository.
2. Create a new branch for your feature or bug fix.
3. Commit your changes with clear messages.
4. Push your branch and create a pull request.

---

### **License**

This project is licensed under the MIT License.
# **Project Proposal: Priority Task Manager with Multi-Agent Coordination**

**Author:** Nicholas Santos
**Date:** October 6, 2025

---

## **Table of Contents**

1.  **Part 1: Executive Summary & Vision**
    *   1.1 Project Overview
    *   1.2 Vision of Success
    *   1.3 Primary Users & Use Cases
    *   1.4 Real-World Relevance & Career Alignment
    *   1.5 Track Specialization: Mobile Development

2.  **Part 2: Technical Architecture & Multi-Agent Design**
    *   2.1 System Workflow Diagram
    *   2.2 Multi-Agent System Design
    *   2.3 Agent Communication & Coordination
    *   2.4 Technical Specifications
    *   2.5 Data Storage & Persistence
    *   2.6 Data Validation & Error Recovery
    *   2.7 Quality Assurance & Standards

3.  **Part 3: Individual Project Management & Development Plan**
    *   3.1 Project Timeline: An 8-Week Plan
    *   3.2 Personal Responsibilities & Workflow
    *   3.3 Scope Definition
    *   3.4 Feasibility Analysis

4.  **Part 4: Foundation Phase Integration & Reflection**
    *   4.1 Concept Integration
    *   4.2 Professional Growth Demonstration

---

## **Part 1: Executive Summary & Vision**

### **1.1 Project Overview**

Standard to-do list applications function as passive, "dumb" lists, requiring users to manually assess priorities and decide what to work on next. This manual process often leads to decision fatigue, procrastination on high-impact tasks, and a disconnect between daily actions and long-term goals. The core problem is that these applications lack context; they do not understand the user's current environment, energy levels, or overarching objectives. This project, the Priority Task Manager, aims to solve this problem by transforming a simple command-line task manager into an intelligent productivity assistant through the implementation of a multi-agent coordination system.

Instead of relying on a single, rigid algorithm, the system will employ a team of three cooperating software agents, each specializing in a different aspect of prioritization. A `TaskAnalyzerAgent` will evaluate task properties like deadlines and complexity, a `UserContextAgent` will consider time of day to match tasks with energy levels, and a `PrioritizationAgent` will synthesize their distributed intelligence into a dynamic, context-aware, and highly relevant task list through a Model Context Protocol (MCP).

The primary value of this multi-agent approach lies in its adaptability and decision quality. Unlike static solutions, the Priority Task Manager will provide recommendations that are tailored to the user's immediate situation, reducing cognitive load and promoting focus on what is truly important. The project will include the ability to toggle between a "single-agent" (the existing rule-based engine) and "multi-agent" mode, allowing for a direct comparison and clear demonstration of the benefits of distributed intelligence in everyday productivity software.

### **1.2 Vision of Success**

The vision of success for this project is to create a command-line application that feels less like a static list and more like a proactive, intelligent assistant. Success will be measured by the system's ability to consistently generate a prioritized task list that a user would intuitively agree with, effectively answering the question, "What is the most important thing I should be doing *right now*?" A successful outcome will be a fully functional proof-of-concept that clearly demonstrates the superior adaptability and decision quality of a multi-agent system over a traditional, single-algorithm approach, culminating in a powerful portfolio piece that showcases advanced architectural and AI coordination skills.

### **1.3 Primary Users & Use Cases**

*   **Primary Users:**
    *   **Technical Professionals (Developers, System Admins):** Individuals comfortable with a command-line interface who manage multiple projects and need an efficient way to prioritize work without leaving their terminal.
    *   **Students & Researchers:** Users who need to balance long-term research goals with short-term deadlines and assignments.
    *   **Productivity Enthusiasts:** Individuals interested in leveraging advanced tools and methodologies to optimize their daily workflows.

*   **Use Cases:**
    1.  **Daily Work Planning:** A developer starts their day by running the application. The multi-agent system considers that it's morning (a high-energy time for this user), that a major project deadline is approaching, and recommends focusing on a complex coding task that aligns with that project's goal.
    2.  **Adapting to Shifting Contexts:** In the afternoon, the user's context has changed. The `UserContextAgent` knows this is a lower-energy time. When the user asks for their priority list again, the system de-prioritizes complex coding tasks and instead suggests a series of smaller, less demanding tasks like writing documentation or responding to emails.
    3.  **Long-Term Goal Alignment:** A student has a long-term goal of "Complete Thesis." When adding tasks, they can tag tasks related to this goal. The `GoalAlignmentAgent` ensures that these tasks receive a consistent priority boost, preventing them from being perpetually overlooked in favor of more urgent but less important daily assignments.
    4.  **Direct Prioritization Comparison:** A user is unsure if the multi-agent system is working for them. They switch to "single-agent" mode, which uses a simple deadline-based algorithm. They notice that the list is less intuitive and doesn't align with their current focus, so they switch back, validating the effectiveness of the multi-agent approach.

### **1.4 Real-World Relevance, Career Alignment & Portfolio Positioning**

The concept of using multiple, specialized agents to solve a complex problem is a foundational pattern in modern AI and distributed systems. This approach is directly relevant to real-world applications such as supply chain optimization, financial fraud detection, and autonomous vehicle navigation, where no single algorithm can account for all variables. This project serves as a practical, small-scale implementation of this powerful paradigm.

**Career Relevance:** My career goal is to work as a Software Engineer specializing in backend systems and application architecture. This project directly supports that goal by providing deep, hands-on experience in:
*   **Advanced Architectural Patterns:** Implementing a sophisticated pattern like MCP to manage complexity.
*   **AI Integration:** Moving beyond using AI as a coding assistant to designing systems where AI components are a core part of the application logic.
*   **System Design:** Making critical decisions about how software components communicate, handle errors, and scale.

**Demonstration Value:** The most impressive feature for potential employers will be the live, demonstrable comparison between the "single-agent" and "multi-agent" modes. This isn't a theoretical improvement; it's a feature that allows anyone to immediately see the superior decision-making of the coordinated agent system. This highlights an ability to not only implement complex AI architectures but also to design systems that clearly prove their own value.

### **1.5 Track Specialization: Mobile Development**

**Primary Track Focus**:
☐ **Game Development Track**
☐ **Web Development Track**
☐ **Data Science/ML Track**
☒ **Mobile Development Track**
☐ **Other Track**: _________________________________________________

**Track Integration Justification:** This project aligns with the **Mobile Development Track** because it focuses on the core competencies required for building robust, professional applications on any platform—mobile, desktop, or command-line. The key alignment points are:

*   **Application Architecture:** The project demonstrates mastery of software design patterns (Strategy Pattern via `IUrgencyStrategy`), dependency injection, and separation of concerns—all fundamental to building maintainable mobile applications.
*   **State Management:** The multi-agent system's coordination through `MCPContext` is conceptually identical to state management challenges in mobile apps (React Context, Redux, SwiftUI's @State).
*   **Offline-First Design:** The application works entirely with local JSON files without network dependencies, mirroring the offline-first design philosophy critical to mobile development.
*   **Cross-Platform Principles:** Built on .NET 8, this application uses the same runtime and patterns that power .NET MAUI mobile applications, demonstrating transferable skills.
*   **Performance & Efficiency:** The agent coordination system must execute quickly to provide responsive user feedback, teaching optimization skills essential for resource-constrained mobile environments.

While this specific implementation is a CLI application, the architectural patterns, state management techniques, and application design principles are directly transferable to mobile development and demonstrate readiness for building sophisticated mobile applications.

---

## **Part 2: Technical Architecture & Multi-Agent Design**

### **2.1 System Workflow Diagram**

The following diagram illustrates the data flow when the user requests a prioritized task list in "multi-agent" mode.

```
[User Input: `list`] --> [CLI: ListHandler]
       |
       v
[TaskManagerService (with MultiAgentUrgencyStrategy)]
       |
       v
[MCP.Coordinate(agents, context)]
       |
       +------------------------------------------------------+
       |                                                      |
       v                                                      v
[MCPContext] <--- [1. TaskAnalyzerAgent]      (Adds complexity/deadline scores)
       |
       v
[MCPContext] <--- [2. UserContextAgent]       (Adds user preference/time scores)
       |
       v
[MCPContext] <--- [3. GoalAlignmentAgent]     (Adds goal alignment scores)
       |
       v
[MCPContext] <--- [4. PrioritizationAgent]    (Calculates final UrgencyScore)
       |
       v
[Final MCPContext with prioritized tasks]
       |
       v
[TaskManagerService] --> [CLI: ListHandler] --> [Formatted Output to User]
```

### **2.2 Multi-Agent System Design**

The multi-agent system is designed as a cooperative, sequential workflow. Each agent enriches a shared `MCPContext` object with its analysis, building upon the work of the a previous agents.

| Agent | Role | Input (from Context) | Output (to Context) | Failure Handling |
| :--- | :--- | :--- | :--- | :--- |
| **`TaskAnalyzerAgent`** | Evaluates objective task properties. | The initial list of all tasks. | Adds a `Dictionary<int, AnalysisData>` to `SharedState`, containing scores for complexity and deadline proximity for each task ID. | Logs an error to `History` and terminates coordination if the task list is missing or invalid. Returns control with error state. |
| **`UserContextAgent`** | Considers the user's environment and preferences. | The analysis from the previous agent. A `UserProfile` object loaded from `user_profile.json`. | Adds a `Dictionary<int, ContextScores>` to `SharedState`, containing scores based on time of day and user preferences. | Logs a warning if the user profile is missing or corrupted. Creates a default profile and continues with standard preferences to maintain system functionality. |
| **`GoalAlignmentAgent`** | Assesses how tasks align with the user's long-term goals. | The analyses from previous agents. Active goals from `user_profile.json`. | Adds a `Dictionary<int, GoalScores>` to `SharedState`, containing scores for tasks that match the user's active goal. | Logs a warning if no active goal is set. Continues operation with zero goal scores for all tasks. |
| **`PrioritizationAgent`** | Synthesizes all analyses into a final priority score. | All scores from the previous three agents. | Iterates through each task, calculates a final `UrgencyScore` using a weighted formula, and updates the task objects directly. | Logs an error and uses fallback single-agent mode if input score dictionaries are malformed or critical data is missing. |

### **2.3 Agent Communication & Coordination**

*   **Communication Method:** All communication between agents is indirect and asynchronous, mediated by the `MCPContext` object. An agent never calls another agent directly. It reads data from the context's `SharedState`, performs its analysis, and writes its results back to the `SharedState` for subsequent agents to consume.
*   **Coordination:** The `MCP.Coordinate` function provides the primary coordination mechanism, ensuring that the agents are executed in a predefined, logical sequence.
*   **Conflict Resolution:** In this cooperative system, conflicts are minimal by design. Since no two agents are responsible for writing the same piece of data, there are no race conditions. The final `PrioritizationAgent` is the sole arbiter responsible for resolving the "conflict" of competing priorities by synthesizing all inputs into a final score.
*   **State Transitions:** If any agent detects a critical failure (corrupt data, missing required files), it logs the error to the MCP history and returns a failure state. The `MultiAgentUrgencyStrategy` detects this failure state and can fall back to the single-agent strategy to maintain system functionality.
*   **Scalability:** The architecture is highly scalable from a feature perspective. Adding a new factor to the prioritization—for example, a `LocationAgent` that considers the user's physical location—would simply require creating a new agent and adding it to the coordination chain, without modifying any existing agents.

### **2.4 Technical Specifications**

*   **Language:** C#
*   **Platform:** .NET 8
*   **Core APIs:** No external APIs are required. The project relies on the .NET Base Class Library for file I/O, collections, and JSON serialization.
*   **MCP Integration:** The system will leverage the custom-built MCP framework (`IAgent`, `MCPContext`, `MCP.cs`) developed in Assignment 4.

### **2.5 Data Storage & Persistence**

*   **Task Data:** Stored in a local `tasks.json` file.
*   **List Data:** Stored in a local `lists.json` file.
*   **User Profile Data:** A new `user_profile.json` file will be created to store user-specific context, such as preferred work times, active goals, and user preferences.
*   **Archived Data:** Stored in `archive.json` via the `cleanup` command.
*   **File Location:** All JSON files are stored in a `data/` subdirectory within the application's execution directory.

### **2.6 Data Validation & Error Recovery**

To ensure system robustness, the following validation and recovery mechanisms will be implemented:

*   **JSON Schema Validation:** All JSON files will be validated against expected schemas on load. Invalid files will trigger a warning and the system will attempt to use default values or recover from a backup.
*   **Automatic Backups:** Before any write operation that modifies `tasks.json`, `lists.json`, or `user_profile.json`, a timestamped backup copy will be created in a `data/backups/` directory. The system will retain the most recent 5 backups.
*   **Corrupt File Recovery:** If a JSON file fails to parse, the system will:
    1.  Log a detailed error with the parse exception
    2.  Attempt to load the most recent backup file
    3.  If no valid backup exists, initialize with empty/default data and notify the user
*   **Context State Validation:** Before each agent executes, the MCP coordinator validates that the `MCPContext` contains all required data from previous agents. Missing or malformed data triggers a controlled failure with detailed logging.
*   **Graceful Degradation:** If the multi-agent system encounters unrecoverable errors, the system automatically falls back to single-agent mode, ensuring the user always has a functional task list.

### **2.7 Quality Assurance & Standards**

*   **Testing:** All new business logic within the agents and services will be developed using a Test-Driven Development (TDD) approach. The xUnit framework will be used to write unit tests for each agent, ensuring its logic is correct in isolation.
*   **Integration Testing:** End-to-end tests will verify that the full agent coordination pipeline produces correct results for common scenarios (urgent deadline, low energy time, goal alignment).
*   **Edge Case Testing:** Specific tests will cover error conditions: missing files, corrupt JSON, invalid user input, and agent failures.
*   **Monitoring:** For this command-line application, monitoring consists of the rich logging provided by the MCP `History` trail, which can be output to the console to debug any prioritization decision.
*   **Documentation:** All public methods and classes will be documented using standard C# XML comments. The project `README.md` will be updated with a comprehensive guide to the multi-agent architecture.

---

## **Part 3: Individual Project Management & Development Plan**

### **3.1 Project Timeline: A 7-Week Plan**

This project will be developed over a seven-week period, with a focused commitment of 6-8 hours per week. The scope has been carefully reduced to ensure that a high-quality core product can be delivered within this timeframe.

**Week 1: [Sprint 1: Foundational Refactoring]**
- [ ] Define the `IUrgencyStrategy.cs` interface to abstract the prioritization logic.
- [ ] Refactor the existing urgency calculation into `SingleAgentStrategy.cs`.
- [ ] Create the class file for `MultiAgentUrgencyStrategy.cs`.
- [ ] Update `TaskManagerService` to use the new strategy interface via dependency injection.
- [ ] Ensure all existing tests pass after refactoring.

**Week 2: [Sprint 2: Data & Agent Scaffolding]**
- [ ] Define a simplified `UserProfile.cs` model to store the selected mode and basic user context (e.g., preferred work hours).
- [ ] Implement the service logic to load and save the `user_profile.json` file.
- [ ] Create scaffolded class files for the three core agents: `TaskAnalyzerAgent.cs`, `UserContextAgent.cs`, and `PrioritizationAgent.cs`, ensuring they implement `IAgent`.
- [ ] Write unit tests for the user profile loading/saving logic.

**Week 3: [Sprint 3: Implement TaskAnalyzerAgent]**
- [ ] Implement the full analysis logic within `TaskAnalyzerAgent.cs` to score tasks based on objective properties like complexity and deadlines.
- [ ] Write comprehensive xUnit tests covering the agent's scoring logic for various task states.

**Week 4: [Sprint 4: Implement UserContextAgent]**
- [ ] Implement the analysis logic within `UserContextAgent.cs` to score tasks based on the time of day relative to the user's preferred work hours.
- [ ] Write unit tests to verify the context-based scoring.

**Week 5: [Sprint 5: Implement PrioritizationAgent & Initial Integration]**
- [ ] Implement the synthesis logic within `PrioritizationAgent.cs` to calculate a final score from the `TaskAnalyzer` and `UserContext` agent inputs.
- [ ] Develop a simple, clear weighting formula.
- [ ] Perform initial integration testing to ensure all three agents coordinate correctly through the MCP.

**Week 6: [Sprint 6: Mode Switching & System Integration]**
- [ ] Implement the `mode <strategy>` command in the CLI to switch between "single-agent" and "multi-agent" modes.
- [ ] Persist the chosen mode in `user_profile.json`.
- [ ] Conduct end-to-end testing, comparing the output of both modes to verify the system works as expected.
- [ ] Fix any bugs discovered during integration.

**Week 7: [Sprint 7: Final Polish, Documentation & Submission]**
- [ ] Conduct a final testing pass on the entire application.
- [ ] Update the `README.md` to explain the new architecture and commands.
- [ ] Add final code comments and XML documentation.
- [ ] Record and edit the final demo video, focusing on the comparison between the two modes.
- [ ] Prepare the project for submission.

### **3.2 Personal Responsibilities & Workflow**

As the sole developer on this project, I am responsible for all aspects of its lifecycle, including design, implementation, testing, and documentation. My workflow will be structured and disciplined to ensure success.

*   **Development Tools:**
    *   **IDE:** Visual Studio Code
    *   **Language/Platform:** C# on .NET 8
    *   **Version Control:** Git, with regular commits to a public GitHub repository. A `develop` branch will be used for new features, which will be merged into `main` only after testing is complete.
    *   **AI Pair Programming:** Copilot will be used for code generation and completing boilerplate tasks, directed by prompts I engineer with the help of Gemini.

*   **Development Process (Agile/Individual):**
    I will follow a personal agile methodology. The 7-week plan serves as the project backlog. Each week's goals are the "sprint backlog." I will start each week by breaking down the high-level goals into a detailed checklist of smaller, actionable tasks in a `TODO.md` file. This approach provides the flexibility of an agile process while maintaining the structure needed for an individual project. TDD will be a core part of the process, ensuring that every new piece of logic is accompanied by a corresponding test.

### **3.3 Scope Definition**

To ensure successful completion within the 6-8 hour weekly timeframe, the project scope has been tightly focused on the core value proposition: demonstrating the superiority of a multi-agent system over a single algorithm.

*   **Core Features (Must-Have):**
    1.  A functional "multi-agent" mode using three distinct agents (`TaskAnalyzer`, `UserContext`, `Prioritization`).
    2.  Implementation of the `IUrgencyStrategy` to allow runtime switching between "single-agent" and "multi-agent" modes.
    3.  A `mode` command for the user to switch between strategies.
    4.  A simplified `user_profile.json` to store the active mode and basic time-of-day preferences.
    5.  Graceful degradation to single-agent mode if the multi-agent system fails.
    6.  Solid unit test coverage (target 70%) for all new agent logic.
    7.  Updated `README.md` documentation explaining the new architecture.

*   **Stretch Goals (Nice-to-Have):**
    *   A `config` command to let users easily set their work-hour preferences.
    *   Implementation of the data backup and recovery system.
    *   More advanced weighting formula in the `PrioritizationAgent`.

*   **Out-of-Scope:**
    *   **Goal Alignment:** The `GoalAlignmentAgent` and all related goal-management CLI commands are explicitly out of scope. This is the primary scope reduction to ensure feasibility.
    *   A graphical user interface (GUI).
    *   Integration with any external APIs or services.
    *   User accounts or cloud synchronization.

### **3.4 Feasibility Analysis**

**Critical Assessment:** This project is designed to be achievable within a consistent **6-8 hour per week** time commitment over 7 weeks. The scope has been deliberately and significantly reduced from the initial concept to focus on delivering a high-quality, functional core that meets the primary learning objectives of the assignment.

**Required Conditions for Success:**

1.  **Consistent Time Commitment:** A focused 6-8 hours must be available each week. The timeline has no buffer weeks, so consistency is key.
2.  **Stable Foundation:** The existing codebase must be stable and require no unplanned refactoring.
3.  **Adherence to Scope:** The "Out-of-Scope" boundaries, particularly the exclusion of the `GoalAlignmentAgent`, must be strictly maintained.

**Risk Factors:**

*   **Underestimation of Complexity:** Integrating the three agents in Week 5 may present unforeseen challenges that take longer than the allotted time.
*   **Minor Delays:** On a tight schedule, even a small technical issue or a missed work session can cause a week's deliverables to slip, creating pressure on subsequent weeks.
*   **Testing Overhead:** Writing good tests takes time. A disciplined TDD approach will be necessary to avoid cutting corners on quality assurance.

**Mitigation Requirements:**

*   **Focus on Core Logic:** The primary focus will always be on the agent logic and coordination. UI polish or minor features will be deferred if time becomes a constraint.
*   **Simplified Formulas:** The prioritization and scoring formulas will be kept simple and clear. Complexity will only be added if all core features are complete and stable.
*   **Weekly Check-ins:** A personal weekly review of progress against the checklist will be conducted. If a task is more than 2-3 hours behind schedule by the end of a week, the scope of the following week will be re-evaluated.

**Realistic Probability Assessment:**

*   **All core features delivered successfully:** ~90%
*   **Core features delivered with reduced test coverage or documentation:** ~95%
*   **Project failure (non-functional multi-agent system):** <5%

**Recommendation:**

This revised plan is highly feasible. By cutting the most complex and time-consuming feature (Goal Alignment) and focusing on the essential demonstration of a two-mode system, the project retains its core value as a portfolio piece while fitting comfortably within the available time. This timeline is challenging but achievable with discipline and a strict adherence to the defined scope.

---

## **Part 4: Foundation Phase Integration & Reflection**

### **4.1 Concept Integration**

This proposal represents a synthesis of the project's original vision with the powerful architectural patterns learned and validated during the course.

*   **Assignment 1 Connection:** This project builds directly on the exploration of an AI tool ecosystem. From its inception, I utilized an AI pair programmer (Copilot) for code generation, and this proposal was refined using a coordinating large language model (Gemini). The workflow defined in Part 3 formalizes this AI-augmented development process.

*   **Assignment 2 Evolution:** This proposal is a direct evolution of the track-specific system architecture design. The initial design concepts for a modular, intelligent task manager are now fully realized with a concrete multi-agent system, detailed data persistence plans, and robust error handling, moving from a high-level design to a production-ready implementation plan.

*   **Assignment 3 Advancement:** This project significantly advances the multi-agent coordination skills prototyped in Assignment 3. Instead of a simple prototype, it designs a cooperative, sequential workflow where multiple agents (TaskAnalyzer, UserContext, etc.) enrich a shared context to achieve a complex goal, demonstrating a more sophisticated coordination pattern.

*   **Assignment 4 Enhancement:** This proposal elevates the MCP-integrated multi-agent coordination from Assignment 4, making it the absolute core of the application's intelligence. The same `MCP.Coordinate` framework used for a utility `cleanup` command is now leveraged to orchestrate the primary decision-making engine, proving its versatility and power.

*   **Track-Specific Integration:** The project showcases advanced competency in the Mobile Development track by implementing core principles essential for mobile platforms. The proposed refactoring into swappable `IUrgencyStrategy` implementations is a classic example of the Strategy design pattern. The focus on offline-first design, robust state management via the MCP, and graceful error handling directly mirrors the challenges and solutions in professional mobile development.

### **4.2 Professional Growth Demonstration**

*   **Skill Progression:** This project demonstrates a clear progression of skill from Week 1 to Week 7. My initial vision was for a modular service, but it lacked a formal mechanism for coordinating multiple, distinct decision-making components. The introduction of MCP in Assignment 4 provided the key to breaking through that ceiling. This proposal shows a shift from designing a single, complex algorithm to orchestrating a symphony of simpler, specialized components—a far more scalable and professional architectural approach.

*   **Professional Practices:** The project will implement several industry patterns from Week 6. Section 2.6, "Data Validation & Error Recovery," outlines a plan for graceful degradation and automatic backups, ensuring fault tolerance. Section 2.7 details a professional TDD workflow, comprehensive logging for monitoring, and clear documentation standards.

*   **Quality Standards:** This project is designed to demonstrate professional-level development practices. The clean separation of concerns using the Strategy pattern, the robust and auditable workflow provided by MCP, and the comprehensive testing and error handling strategy all contribute to a portfolio piece that showcases production-ready application architecture skills applicable to any platform. piece that showcases production-ready application architecture skills applicable to any platform.
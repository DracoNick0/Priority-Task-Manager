# **Project Proposal: Priority Task Manager with Multi-Agent Coordination**

**Author:** Nicholas Santos
**Date:** October 6, 2025

---

## **Table of Contents**

1.  **Part 1: Executive Summary & Vision**
    *   1.1 Project Overview
    *   1.2 Vision of Success
    *   1.3 Primary Users & Use Cases
    *   1.4 Portfolio Positioning
    *   1.5 Track Specialization Identification

2.  **Part 2: Technical Architecture & Multi-Agent Design**
    *   2.1 System Workflow Diagram
    *   2.2 Agent Architecture Design
    *   2.3 Agent Communication & Coordination
    *   2.4 Technical Specifications
    *   2.5 Data Storage & Persistence
    *   2.6 Professional Practices: Data Validation & Error Recovery
    *   2.7 Professional Practices: Quality Assurance & Standards

3.  **Part 3: Individual Project Management & Development Plan**
    *   3.1 Timeline & Sprint Planning
    *   3.2 Individual Development Plan
    *   3.3 Scope & Feasibility Analysis

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

*   **Core Use Cases:**
    1.  **Daily Work Planning:** A developer starts their day by running the application. The multi-agent system considers that it's morning (a high-energy time for this user), that a major project deadline is approaching, and recommends focusing on a complex coding task that aligns with that project's goal.
    2.  **Adapting to Shifting Contexts:** In the afternoon, the user's context has changed. The `UserContextAgent` knows this is a lower-energy time. When the user asks for their priority list again, the system de-prioritizes complex coding tasks and instead suggests a series of smaller, less demanding tasks like writing documentation or responding to emails.
    3.  **Long-Term Goal Alignment:** A student has a long-term goal of "Complete Thesis." When adding tasks, they can tag tasks related to this goal. The `GoalAlignmentAgent` ensures that these tasks receive a consistent priority boost, preventing them from being perpetually overlooked in favor of more urgent but less important daily assignments.
    4.  **Direct Prioritization Comparison:** A user is unsure if the multi-agent system is working for them. They switch to "single-agent" mode, which uses a simple deadline-based algorithm. They notice that the list is less intuitive and doesn't align with their current focus, so they switch back, validating the effectiveness of the multi-agent approach.

### **1.4 Portfolio Positioning**

*   **Career Relevance:** The concept of using multiple, specialized agents to solve a complex problem is a foundational pattern in modern AI and distributed systems. This approach is directly relevant to real-world applications such as supply chain optimization, financial fraud detection, and autonomous vehicle navigation. My career goal is to work as a Software Engineer specializing in backend systems and application architecture. This project directly supports that goal by providing deep, hands-on experience in Advanced Architectural Patterns (MCP), AI Integration, and System Design.

*   **Demonstration Value:** The most impressive feature for a potential employer will be the live, direct comparison between the "single-agent" and "multi-agent" modes. This feature provides undeniable proof of concept; it's not just a theoretical improvement but a tangible demonstration of how a sophisticated multi-agent architecture produces superior, context-aware results compared to a traditional monolithic algorithm. This clearly showcases an ability to design, build, and deploy advanced, intelligent systems.

### **1.5 Track Specialization Identification**

**Primary Track Focus:**
☐ **Game Development Track**
☐ **Web Development Track**
☐ **Data Science/ML Track**
☒ **Mobile Development Track**
☐ **Other Track**: _________________________________________________

**Track Integration Justification**: This project aligns with the **Mobile Development Track** because it focuses on the core competencies required for building robust, professional applications on any platform. The architectural patterns (Strategy Pattern, dependency injection), state management techniques (MCPContext as a state container), and offline-first design (local JSON files) are all fundamental principles directly transferable to building sophisticated mobile applications with frameworks like .NET MAUI.

---

## **Part 2: Technical Architecture & Multi-Agent Design**

### **2.1 System Workflow Diagram**

The following diagram illustrates the data flow when the user requests a prioritized task list in "multi-agent" mode. A formal version of this diagram is included in the `architecture-diagrams/` directory.

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

### **2.2 Agent Architecture Design**

The multi-agent system is designed as a cooperative, sequential workflow.

**Agent Specifications:**
```
Agent Name: TaskAnalyzerAgent
Primary Responsibility: Evaluates objective task properties like complexity and deadlines.
Input: The initial list of all tasks from the MCPContext.
Output: Adds a dictionary of analysis scores (complexity, deadline proximity) to the MCPContext.
Coordination Pattern: Sequential enrichment of the shared MCPContext.
Failure Handling: Logs an error to the MCP History and terminates coordination if the task list is invalid.
```
```
Agent Name: UserContextAgent
Primary Responsibility: Considers the user's environment and preferences (e.g., time of day).
Input: Analysis from the previous agent and a UserProfile object from the MCPContext.
Output: Adds a dictionary of context-based scores to the MCPContext.
Coordination Pattern: Sequential enrichment of the shared MCPContext.
Failure Handling: Logs a warning and uses a default profile if the user profile is missing or corrupt, allowing the system to continue.
```
```
Agent Name: GoalAlignmentAgent
Primary Responsibility: Assesses how tasks align with the user's defined long-term goals.
Input: Analyses from previous agents and active goals from the MCPContext.
Output: Adds a dictionary of goal alignment scores to the MCPContext.
Coordination Pattern: Sequential enrichment of the shared MCPContext.
Failure Handling: Logs a warning and proceeds with zero scores if no active goal is set.
```
```
Agent Name: PrioritizationAgent
Primary Responsibility: Synthesizes all analyses from previous agents into a final priority score.
Input: All score dictionaries from the previous three agents in the MCPContext.
Output: Updates each task object in the context with a final, calculated UrgencyScore.
Coordination Pattern: Final aggregation and resolution of data from the shared MCPContext.
Failure Handling: Logs an error and triggers a fallback to the single-agent mode if input scores are missing or malformed.
```

**System Coordination Pattern**:
*   **Communication Protocol:** Communication is indirect and asynchronous, mediated by the `MCPContext` object. Agents do not communicate directly.
*   **Conflict Resolution:** Conflicts are resolved by design. The final `PrioritizationAgent` acts as the sole arbiter, synthesizing all inputs into a final score using a weighted formula.
*   **Scalability Considerations:** The architecture is scalable. A new agent (e.g., `LocationAgent`) can be added to the coordination chain in the `MCP.Coordinate` call without modifying any existing agents.

### **2.3 Technology Stack & MCP Integration**

*   **Programming Languages:** C# on .NET 8.
*   **MCP Implementation:** The system will use the custom-built MCP framework (`IAgent`, `MCPContext`, `MCP.cs`) to orchestrate the agent sequence.
*   **External APIs/Services:** None. The project is self-contained.
*   **Database/Storage:** Data persistence is handled via local JSON files (`tasks.json`, `user_profile.json`).
*   **Testing Strategy:** Unit tests for each agent will be written using xUnit. Integration tests will verify the end-to-end coordination process.

### **2.4 Professional Practices Integration**

*   **Error Handling:** The system uses graceful degradation. If the multi-agent coordination fails, it automatically falls back to the simpler, reliable single-agent mode.
*   **Monitoring & Logging:** All agent actions, successes, and failures are logged to the MCP `History` trail, which can be output to the console for debugging.
*   **Documentation Standards:** All public methods and classes will use standard C# XML comments. The project `README.md` will be updated with a guide to the multi-agent architecture.
*   **Code Quality:** The project will adhere to standard C# coding conventions and leverage design patterns (Strategy, Dependency Injection) for maintainability.

---

## **Part 3: Individual Project Management & Development Planning**

### **3.1 Timeline & Sprint Planning**

This project will be developed over 7 weeks, structured into the following four sprints.

**Week 8: [Sprint 1: Foundational Architecture & Agent Scaffolding]**
- [ ] Define and implement `IUrgencyStrategy` interface to abstract prioritization logic.
- [ ] Refactor existing logic into `SingleAgentStrategy.cs`.
- [ ] Create class files for `MultiAgentUrgencyStrategy.cs` and all four agents (`TaskAnalyzer`, `UserContext`, `GoalAlignment`, `Prioritization`).
- [ ] Update `TaskManagerService` to use dependency injection for the strategy.
- [ ] Define the `UserProfile.cs` model and implement the service to load/save `user_profile.json`.
- [ ] Implement data backup and JSON validation systems.
- [ ] All existing tests must pass after refactoring.

**Week 9: [Sprint 2: Core Agent Implementation]**
- [ ] Implement the full analysis logic within `TaskAnalyzerAgent.cs`.
- [ ] Write comprehensive xUnit tests for `TaskAnalyzerAgent`.
- [ ] Implement time-of-day and user preference logic in `UserContextAgent.cs`.
- [ ] Implement the CLI `config` command for users to set preferences.
- [ ] Implement `GoalAlignmentAgent.cs` to score tasks based on goal alignment.
- [ ] Implement CLI commands for goal management (`goal add`, `goal set-active`).
- [ ] Write unit tests for user context and goal alignment logic.

**Week 10: [Sprint 3: System Integration & Final Agent Logic]**
- [ ] Implement the synthesis and weighting formula logic within `PrioritizationAgent.cs`.
- [ ] Implement failure detection and the fallback mechanism to single-agent mode.
- [ ] Implement the `mode <strategy>` command to switch between modes and persist the choice.
- [ ] Begin end-to-end integration testing with all agents working together in the MCP chain.
- [ ] Fix critical bugs discovered during integration.

**Week 11: [Sprint 4 & Final Presentation]**
- [ ] Final integration and testing of the complete system in both modes.
- [ ] Complete all project documentation, including the comprehensive `README.md` update.
- [ ] Add inline code comments and XML documentation to all public APIs.
- [ ] Create a user guide for all new commands and features.
- [ ] Record and edit the final demo video, focusing on the single-agent vs. multi-agent comparison.
- [ ] Prepare the final project for submission.

### **3.2 Individual Development Plan**

*   **Personal Role & Responsibilities:** As the sole developer, I am responsible for all aspects of the project: design, implementation, testing, and documentation.
*   **Development Tools & Workflow:** I will use Visual Studio Code, C# on .NET 8, and Git for version control on a public GitHub repository. A `develop` branch will be used for new features, merged into `main` after testing. Copilot and Gemini will be used as AI assistants.
*   **Development Methodology:** I will follow a personal agile methodology. The 4-sprint plan serves as the project backlog. Each week, I will break down the sprint goals into a detailed checklist. Test-Driven Development (TDD) will be used for new business logic.

### **3.3 Scope & Feasibility Analysis**

*   **Core Features (Must-Have):**
    1.  A functional "multi-agent" mode using all four agents.
    2.  Runtime switching between "single-agent" and "multi-agent" modes via the `mode` command.
    3.  A `user_profile.json` file to store user preferences and goals.
    4.  Goal management commands (`goal add`, `goal set-active`).
    5.  Data validation, backup/recovery, and graceful degradation on failure.
    6.  Comprehensive unit test coverage for all new agent logic.

*   **Stretch Goals (Nice-to-Have):**
    *   Allowing users to customize the agent weighting formula via the `config` command.
    *   Export/import functionality for user profiles.

*   **Scope Boundaries (Out-of-Scope):**
    *   A graphical user interface (GUI).
    *   Integration with external APIs (e.g., Google Calendar).
    *   User accounts or cloud synchronization.

*   **Feasibility Validation:** The 7-week timeline requires a consistent commitment of possibly over 6 hours per week. The plan is achievable because it builds upon a stable existing codebase and leverages a pre-validated architectural pattern (MCP). The primary risks are time compression and lack of buffer for unexpected issues. Mitigation involves ruthless prioritization: if a sprint's core goals are at risk, stretch goals will be abandoned, and test coverage targets may be slightly reduced to ensure core functionality is delivered.

---

## **Part 4: Foundation Phase Integration & Reflection**

### **4.1 Concept Integration**

This project represents a synthesis of all concepts from the Foundation Phase.

*   **Assignment 1 Connection:** This proposal was developed by leveraging the AI tool ecosystem explored in Assignment 1. Copilot was used for boilerplate code generation, while Gemini was used for higher-level architectural reasoning and documentation refinement, demonstrating an effective coordination of AI assistants.
*   **Assignment 2 Evolution:** The project advances the track-specific system architecture designed in Assignment 2. The initial design was enhanced with a more sophisticated multi-agent approach, moving from a simple service architecture to a coordinated, intelligent system.
*   **Assignment 3 Advancement:** This project expands significantly on the multi-agent coordination prototype from Assignment 3. Instead of a simple prototype, it implements a production-ready system with multiple, specialized agents that handle complex, real-world data and user context.
*   **Assignment 4 Enhancement:** The project builds directly upon the MCP-integrated expertise from Assignment 4. The same MCP framework validated with the `cleanup` command is now elevated to become the core engine of the application's primary feature, demonstrating a deep and versatile application of the pattern.
*   **Track-Specific Integration:** The project showcases advanced competency in the Mobile Development track by implementing platform-agnostic, professional-grade architectural patterns. Specific examples include:
    *   **The Strategy Pattern:** The `IUrgencyStrategy` interface allows for swapping prioritization logic at runtime, a key pattern for building extensible and maintainable mobile apps.
    *   **State Management:** The `MCPContext` object functions as a robust, transactional state container, mirroring state management challenges and solutions in mobile frameworks like Redux or React Context.
    *   **Offline-First Design:** The application's reliance on local JSON files mirrors the critical offline-first design philosophy essential for reliable mobile user experiences.

### **4.2 Professional Growth Demonstration**

*   **Skill Progression:** This project showcases a clear progression from Week 1 to Week 7. My initial vision for the project was sound but lacked the formal architectural pattern needed for true multi-agent coordination. The introduction of MCP in Assignment 4 provided the key to unlocking the project's full potential, enabling a shift from designing a single complex algorithm to orchestrating a system of simpler, specialized components.
*   **Professional Practices:** The project will implement several industry patterns from Week 6, including **Graceful Degradation** (falling back to single-agent mode on failure) and robust **Monitoring & Logging** via the MCP History trail.
*   **Quality Standards:** The project demonstrates professional-level development through its commitment to Test-Driven Development (TDD), comprehensive documentation (XML comments and README), and the use of established design patterns, resulting in a maintainable, extensible, and portfolio-worthy application. design patterns, resulting in a maintainable, extensible, and portfolio-worthy application.
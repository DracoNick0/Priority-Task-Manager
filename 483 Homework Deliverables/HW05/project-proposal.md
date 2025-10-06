# **Project Proposal: Priority Task Manager with Multi-Agent Coordination**

**Author:** Gemini (AI Project Strategist)
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
    *   2.6 Quality Assurance & Standards

3.  **Part 3: Individual Project Management & Development Plan**
    *   3.1 Project Timeline: A 7-Week Plan
    *   3.2 Personal Responsibilities & Workflow
    *   3.3 Scope Definition
    *   3.4 Feasibility Analysis

4.  **Part 4: Foundation Phase Integration & Reflection**
    *   4.1 Building on Previous Assignments
    *   4.2 Integration of Core Concepts
    *   4.3 Reflection on Skill Growth

---

## **Part 1: Executive Summary & Vision**

### **1.1 Project Overview**

Standard to-do list applications function as passive, "dumb" lists, requiring users to manually assess priorities and decide what to work on next. This manual process often leads to decision fatigue, procrastination on high-impact tasks, and a disconnect between daily actions and long-term goals. The core problem is that these applications lack context; they do not understand the user's current environment, energy levels, or overarching objectives. This project, the Priority Task Manager, aims to solve this problem by transforming a simple command-line task manager into an intelligent productivity assistant through the implementation of a multi-agent coordination system.

Instead of relying on a single, rigid algorithm, the system will employ a team of cooperating software agents, each specializing in a different aspect of prioritization. A `TaskAnalyzerAgent` will evaluate task properties like deadlines and complexity, a `UserContextAgent` will consider user-defined preferences and time of day, and a `GoalAlignmentAgent` will assess how tasks contribute to larger goals. These agents will coordinate their findings through a Model Context Protocol (MCP), allowing a final `PrioritizationAgent` to synthesize their distributed intelligence into a dynamic, context-aware, and highly relevant task list.

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

### **1.4 Real-World Relevance & Career Alignment**

The concept of using multiple, specialized agents to solve a complex problem is a foundational pattern in modern AI and distributed systems. This approach is directly relevant to real-world applications such as supply chain optimization, financial fraud detection, and autonomous vehicle navigation, where no single algorithm can account for all variables. This project serves as a practical, small-scale implementation of this powerful paradigm.

My career goal is to work as a Software Engineer specializing in backend systems and application architecture. This project directly supports that goal by providing deep, hands-on experience in:
*   **Advanced Architectural Patterns:** Implementing a sophisticated pattern like MCP to manage complexity.
*   **AI Integration:** Moving beyond using AI as a coding assistant to designing systems where AI components are a core part of the application logic.
*   **System Design:** Making critical decisions about how software components communicate, handle errors, and scale.

### **1.5 Track Specialization: Data Science/ML**

This project is most directly aligned with the **Data Science/ML Track**. While it does not implement a traditional machine learning model, its core architecture is fundamentally that of a **multi-agent data processing pipeline**, a key concept in the Data Science field. The justification is as follows:

*   **Data Pipeline Architecture:** The system's primary function is to process raw input data (task properties, user profiles, goals) through a sequential pipeline of analytical agents. Each agent transforms and enriches the data, which is then synthesized into a final, value-added output (the prioritized task list). This is the exact workflow of a data science pipeline designed to produce actionable insights.
*   **Data-Driven Agent System:** The project is a clear example of a data-driven agent system. The behavior and output of each agent are entirely determined by the data it receives from the shared `MCPContext`. The agents perform a series of micro-analyses on the data to inform a complex, synthesized decision.
*   **Focus on Analytics over UI:** Unlike the other tracks, which are heavily focused on the user interface and platform-specific technologies (Web, Mobile, Game), this project's primary innovation is in its backend analytical engine. The core challenge is not in how the information is presented, but in how the data is processed to generate an intelligent result, which is the central focus of the Data Science/ML track.

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

The multi-agent system is designed as a cooperative, sequential workflow. Each agent enriches a shared `MCPContext` object with its analysis, building upon the work of the previous agents.

| Agent | Role | Input (from Context) | Output (to Context) | Failure Handling |
| :--- | :--- | :--- | :--- | :--- |
| **`TaskAnalyzerAgent`** | Evaluates objective task properties. | The initial list of all tasks. | Adds a `Dictionary<int, AnalysisData>` to `SharedState`, containing scores for complexity and deadline proximity for each task ID. | Logs an error to `History` and terminates if the task list is missing or invalid. |
| **`UserContextAgent`** | Considers the user's environment and preferences. | The analysis from the previous agent. A `UserProfile` object loaded from `user_profile.json`. | Adds a `Dictionary<int, ContextScores>` to `SharedState`, containing scores based on time of day and user preferences. | Logs a warning if the user profile is missing but continues the operation with default values. |
| **`GoalAlignmentAgent`** | Assesses how tasks align with the user's long-term goals. | The analyses from previous agents. | Adds a `Dictionary<int, GoalScores>` to `SharedState`, containing scores for tasks that match the user's active goal. | Logs a warning if no active goal is set but continues the operation. |
| **`PrioritizationAgent`** | Synthesizes all analyses into a final priority score. | All scores from the previous three agents. | Iterates through each task, calculates a final `UrgencyScore` using a weighted formula, and updates the task objects directly. | Logs an error and terminates if the input score dictionaries are malformed or missing. |

### **2.3 Agent Communication & Coordination**

*   **Communication Method:** All communication between agents is indirect and asynchronous, mediated by the `MCPContext` object. An agent never calls another agent directly. It reads data from the context's `SharedState`, performs its analysis, and writes its results back to the `SharedState` for subsequent agents to consume.
*   **Coordination:** The `MCP.Coordinate` function provides the primary coordination mechanism, ensuring that the agents are executed in a predefined, logical sequence.
*   **Conflict Resolution:** In this cooperative system, conflicts are minimal by design. Since no two agents are responsible for writing the same piece of data, there are no race conditions. The final `PrioritizationAgent` is the sole arbiter responsible for resolving the "conflict" of competing priorities by synthesizing all inputs into a final score.
*   **Scalability:** The architecture is highly scalable from a feature perspective. Adding a new factor to the prioritization—for example, a `LocationAgent` that considers the user's physical location—would simply require creating a new agent and adding it to the coordination chain, without modifying any existing agents.

### **2.4 Technical Specifications**

*   **Language:** C#
*   **Platform:** .NET 8
*   **Core APIs:** No external APIs are required. The project relies on the .NET Base Class Library for file I/O, collections, and JSON serialization.
*   **MCP Integration:** The system will leverage the custom-built MCP framework (`IAgent`, `MCPContext`, `MCP.cs`) developed in Assignment 4.

### **2.5 Data Storage & Persistence**

*   **Task Data:** Stored in a local `tasks.json` file.
*   **List Data:** Stored in a local `lists.json` file.
*   **User Profile Data:** A new `user_profile.json` file will be created to store user-specific context, such as preferred work times and active goals.
*   **Archived Data:** Stored in `archive.json` via the `cleanup` command.

### **2.6 Quality Assurance & Standards**

*   **Testing:** All new business logic within the agents and services will be developed using a Test-Driven Development (TDD) approach. The xUnit framework will be used to write unit tests for each agent, ensuring its logic is correct in isolation.
*   **Monitoring:** For this command-line application, monitoring consists of the rich logging provided by the MCP `History` trail, which can be output to the console to debug any prioritization decision.
*   **Documentation:** All public methods and classes will be documented using standard C# XML comments. The project `README.md` will be updated with a comprehensive guide to the multi-agent architecture.

---

## **Part 3: Individual Project Management & Development Plan**

### **3.1 Project Timeline: A 7-Week Plan**

This project will be developed over a seven-week period, following an iterative approach. Each week is treated as a mini-sprint with a clear goal and a set of defined deliverables, ensuring steady progress and allowing for adjustments as needed.

| Week | Goal | Key Deliverables |
| :--- | :--- | :--- |
| **1** | **Foundational Refactoring & Scaffolding** | Create the `IUrgencyStrategy` interface. Refactor the existing `UrgencyService` to implement this interface. Create the new `MultiAgentUrgencyStrategy` class. Scaffold the class files for all new intelligent agents (`TaskAnalyzer`, `UserContext`, etc.) and define the models for `user_profile.json`. |
| **2** | **Implement Task Analysis & Prioritization Agents** | Implement the full logic for the `TaskAnalyzerAgent` (scoring complexity/deadlines). Implement the final `PrioritizationAgent`, initially using a simple formula to synthesize scores. Write comprehensive unit tests for both agents. |
| **3** | **Implement User Context Agent** | Create the `user_profile.json` file and loading logic. Implement the full logic for the `UserContextAgent` to score tasks based on time of day and user preferences. |
| **4** | **Integrate Mode Switching** | Implement the `mode` command to switch between "single-agent" and "multi-agent" strategies. Update the `ListHandler` to use the selected strategy. Perform end-to-end integration testing. |
| **5** | **Stretch Goal: Goal Alignment Agent** | *If time permits.* Add a `Goal` property to the `TaskItem` model. Implement the `GoalAlignmentAgent`. Create CLI commands for the user to set and view their active goal. |
| **6** | **Documentation, Testing & Refinement** | Write the complete `README.md` documentation for the new architecture. Add XML comments to all public-facing code. Refine the prioritization formula based on testing. Ensure all unit tests are passing. |
| **7** | **Final Demo Preparation & Project Submission** | Create the demo video script. Record and edit the final demo video. Write the final reflection paper. Prepare the project for submission. |

### **3.2 Personal Responsibilities & Workflow**

As the sole developer on this project, I am responsible for all aspects of its lifecycle, including design, implementation, testing, and documentation. My workflow will be structured and disciplined to ensure success.

*   **Development Tools:**
    *   **IDE:** Visual Studio Code
    *   **Language/Platform:** C# on .NET 8
    *   **Version Control:** Git, with regular commits to a private GitHub repository. A `develop` branch will be used for new features, which will be merged into `main` only after testing is complete.
    *   **AI Pair Programming:** Copilot will be used for code generation and completing boilerplate tasks, directed by prompts engineered with Gemini.

*   **Development Process (Agile/Individual):**
    I will follow a personal agile methodology. The 7-week plan serves as the project backlog. Each week's goals are the "sprint backlog." I will start each week by breaking down the high-level goals into a detailed checklist of smaller, actionable tasks in a `TODO.md` file. This approach provides the flexibility of an agile process while maintaining the structure needed for an individual project. TDD will be a core part of the process, ensuring that every new piece of logic is accompanied by a corresponding test.

### **3.3 Scope Definition**

To ensure the project is completed successfully within the timeline, the scope is clearly defined.

*   **Core Features (Must-Have):**
    1.  A fully functional "multi-agent" prioritization mode that uses at least three distinct analysis agents.
    2.  Implementation of the `IUrgencyStrategy` to allow for runtime switching between "single-agent" and "multi-agent" modes.
    3.  A new `mode` command for the user to switch between prioritization strategies.
    4.  A `user_profile.json` file to store at least one user preference (e.g., preferred work times).
    5.  Comprehensive unit test coverage for all new agent logic.
    6.  Updated README documentation explaining the new architecture.

*   **Stretch Goals (Nice-to-Have):**
    *   Implement the `GoalAlignmentAgent` and the corresponding CLI commands to manage goals.
    *   Add a `config` command to allow the user to change their preferences directly from the CLI.
    *   Develop a more sophisticated weighting formula in the `PrioritizationAgent` that can be configured by the user.

*   **Out-of-Scope:**
    *   A graphical user interface (GUI). The project will remain a command-line application.
    *   Integration with any external calendar or to-do list APIs (e.g., Google Calendar, Todoist).
    *   User accounts or cloud synchronization. The application will remain a local, single-user experience.

### **3.4 Feasibility Analysis**

The successful completion of this project within the 7-week timeframe is highly feasible for several key reasons:

1.  **Strong Foundation:** The project is not being built from scratch. It builds upon a stable, well-architected codebase with a robust MCP framework, a clean service layer, and a suite of existing unit tests.
2.  **Modular Design:** The multi-agent architecture inherently lends itself to incremental development. Each agent is a self-contained unit that can be built, tested, and completed independently, minimizing risk and complexity.
3.  **Clear Scope:** The scope has been tightly defined, with a clear distinction between essential features and non-essential stretch goals. This provides a clear path to a "minimum viable product" and prevents scope creep.
4.  **Efficient Workflow:** The use of AI-assisted development tools (Copilot and Gemini) and a disciplined TDD process will significantly accelerate the implementation and validation phases, allowing more time to be spent on design and testing.

---

## **Part 4: Foundation Phase Integration & Reflection**

### **4.1 Building on Previous Assignments**

The Priority Task Manager project was an active development effort prior to the start of this course, with a foundational architecture and a clear vision for creating an intelligent task prioritization system. The assignments in the Foundation Phase did not serve to create this project, but rather to validate its existing workflow, enhance its architecture with formal patterns, and provide the critical mechanism to realize its ultimate vision.

The project was already in alignment with the principles of **Assignments 1 and 2**, which focused on AI-assisted programming and AI architecture design. From its inception, I utilized an AI pair programmer (Copilot) for code generation, and the project's core architectural goal was to move beyond a simple algorithm into a more intelligent, multi-faceted system. These assignments provided a structured framework to formalize and reflect on this existing AI-augmented development process, reinforcing best practices in prompt engineering and system design.

The most significant impact came from **Assignments 3 and 4**, which introduced the Model Context Protocol (MCP). The existing `cleanup` command was a planned feature, but its implementation was a perfect opportunity to integrate this new, formal pattern. Rather than building a monolithic and risky function, I implemented the MCP framework as a direct result of the course material. This assignment tangibly improved the project by adding a robust, modular, and fail-safe workflow, demonstrating a direct application of learned concepts to enhance the existing codebase.

### **4.2 Integration of Core Concepts**

This proposal represents a synthesis of the project's original vision with the powerful architectural patterns learned and validated during the course.

*   **MCP Integration:** The successful integration of MCP for the `cleanup` command in Assignment 4 validated its power for creating transactional, auditable workflows. This proposal now elevates that pattern from a utility function to the absolute core of the application's intelligence. The very same `MCP.Coordinate` framework will be used to orchestrate the new decision-making agents, demonstrating a deep and versatile application of the concept beyond the initial assignment's requirements.

*   **Multi-Agent Coordination:** The project's founding vision was to create a prioritization engine that was more intelligent than a simple, rule-based calculator. The concept of multi-agent coordination, formalized in the course, provided the ideal architectural solution to achieve this. MCP became the enabling technology—the "negotiation table"—that allows specialized agents to cooperate and contribute their unique insights, fulfilling the project's original goal in a way that is far more robust and scalable than a complex, monolithic algorithm.

*   **Track-Specific Specialization (Mobile/Desktop):** The project continues to demonstrate best practices in application development. The proposed refactoring—separating the prioritization logic into swappable `IUrgencyStrategy` implementations—is a classic example of the Strategy design pattern, a cornerstone of building maintainable and extensible applications. This focus on decoupled, testable, and robust architecture is central to professional desktop and mobile development.

### **4.3 Reflection on Skill Growth**

My journey through the Foundation Phase has been one of enhancing a well-designed system with a more powerful architectural paradigm. My initial vision for the Priority Task Manager was always built on a foundation of sound engineering principles; the separation of concerns and a service-oriented architecture ensured the application was inherently testable and extensible. My initial plan for the intelligent prioritization engine, for example, was a modular service that could be expanded over time. However, this design had a ceiling; it was well-suited for adding more rules to a single decision-making process, but not for orchestrating multiple, distinct decision-making *components*.

Assignment 4 and the introduction of the Model Context Protocol provided the key to breaking through that ceiling. MCP wasn't just a replacement for a bad design; it was an upgrade to a good one. It provided the formal structure needed to coordinate a system of cooperating agents, allowing the application's intelligence to grow in a way that was far more sophisticated than my original idea. Learning and implementing MCP enabled a crucial shift from designing a single, complex algorithm to orchestrating a symphony of simpler, specialized components. This unlocked a new level of potential for the project, allowing it to evolve from a smart task manager into a truly intelligent, multi-faceted assistant.
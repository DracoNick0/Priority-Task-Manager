# Complexity Guide

This guide provides a standardized way to quantify task complexity on a scale of 1-10. Use this reference to ensure consistent data entry.

## Concept: Complexity vs. Duration
*   **Duration**: How long it takes (Time).
*   **Complexity**: How much cognitive load/focus it requires (Effort/Brainpower).
*   *Example*: Data Entry might take 4 hours (High Duration) but is low complexity (1). Debugging a race condition might take 30 minutes (Low Duration) but is maximum complexity (10).

## The Scale

| Level | Category | Description | Examples (Software) | Examples (General) |
| :--- | :--- | :--- | :--- | :--- |
| **1-2** | **Trivial** | Can be done while distracted. Little thinking required. Low consequence of error. | • Fix typo <br>• Update package version <br>• Formatting code | • Washing dishes <br>• Checking email <br>• Organize files |
| **3-4** | **Simple** | Routine work. requires attention but standard patterns apply. | • Write standard CRUD endpoint <br>• Add CSS style <br>• Write unit test for simple function | • Grocery shopping <br>• Writing a standard report <br>• Scheduling meetings |
| **5-6** | **Moderate** | Requires focus. Non-standard logic or coordinating multiple pieces. | • Implement new feature logic <br>• Refactor a small class <br>• Debugging a clear error | • Writing an essay <br>• Planning a trip <br>• Cooking a new recipe |
| **7-8** | **Complex** | High focus required. High consequence of error. Requires deep understanding of system. | • Database schema change <br>• Optimizing query performance <br>• Implementing complex validation rules | • Tax preparation (Self) <br>• Learning a new skill <br>• Assembling complex furniture |
| **9-10**| **Severe** | Full cognitive load. "Flow state" required. Research heavy or high risk. | • Distributed system architecture <br>• Debugging intermittent race condition <br>• Cryptography implementation | • Legal defense strategy <br>• Advanced scientific research <br>• Writing a book chapter |

## Usage in Priority Task Manager
When inputting complexity for the `ComplexityBalancerAgent`:
*   **10** will be treated as a "Primary Focus" for the day. The user should not have many of these back-to-back.
*   **1** is treated as "Filler" that can be slotted into small gaps or tired periods.

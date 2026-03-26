using PriorityTaskManager.MCP;
using PriorityTaskManager.Models;
using System;
using System.Collections.Generic;

namespace PriorityTaskManager.MCP.Agents
{
    /// <summary>
    /// An agent responsible for cleaning and normalizing task data before it enters the main scheduling pipeline.
    /// It ensures that tasks have sensible default values for critical properties like Importance,
    /// Estimated Duration, and Complexity, preventing errors in downstream calculations.
    /// </summary>
    public class TaskAnalyzerAgent : IAgent
    {
        public MCPContext Act(MCPContext context)
        {
            // Retrieve the tasks list from the shared context.
            if (!context.SharedState.TryGetValue("Tasks", out var tasksObj) || tasksObj is not List<TaskItem> tasks)
            {
                context.History.Add("TaskAnalyzerAgent: No valid task list found in context. Nothing to analyze.");
                return context;
            }

            context.History.Add("TaskAnalyzerAgent: Analyzing tasks and applying default values...");

            foreach (var task in tasks)
            {
                // Ensure Importance has a baseline value.
                if (task.Importance == 0)
                    task.Importance = 1;

                // Ensure EstimatedDuration is a positive, non-zero value.
                if (task.EstimatedDuration <= TimeSpan.Zero)
                    task.EstimatedDuration = TimeSpan.FromHours(1);

                // Ensure Complexity has a baseline value.
                if (task.Complexity <= 0.0)
                    task.Complexity = 1.0;
            }

            context.History.Add("TaskAnalyzerAgent: Task analysis complete. Defaults applied.");

            return context;
        }
    }
}

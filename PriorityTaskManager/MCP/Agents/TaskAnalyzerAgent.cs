using PriorityTaskManager.MCP;
using PriorityTaskManager.Models;
using System;
using System.Collections.Generic;

namespace PriorityTaskManager.MCP.Agents
{
    public class TaskAnalyzerAgent : IAgent
    {
        public MCPContext Act(MCPContext context)
        {
            // Retrieve the tasks list from context
            if (!context.SharedState.TryGetValue("Tasks", out var tasksObj) || tasksObj is not List<TaskItem> tasks)
            {
                context.History.Add("TaskAnalyzerAgent: No valid task list found in context.");
                return context;
            }

            foreach (var task in tasks)
            {
                // Apply default for Importance
                if (task.Importance == 0)
                    task.Importance = 1;

                // Apply default for EstimatedDuration
                if (task.EstimatedDuration == TimeSpan.Zero)
                    task.EstimatedDuration = TimeSpan.FromHours(1);

                // Apply default for Complexity
                if (task.Complexity <= 0.0)
                    task.Complexity = 1.0;
            }

            // Update the context with the processed list
            context.SharedState["Tasks"] = tasks;
            context.History.Add("TaskAnalyzerAgent: Tasks analyzed and defaults applied.");
            return context;
        }
    }
}

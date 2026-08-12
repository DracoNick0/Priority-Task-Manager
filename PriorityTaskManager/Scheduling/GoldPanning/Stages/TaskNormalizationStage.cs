using PriorityTaskManager.Scheduling.GoldPanning;
using PriorityTaskManager.Models;
using System;
using System.Collections.Generic;

namespace PriorityTaskManager.Scheduling.GoldPanning.Stages
{
    /// <summary>
    /// An agent responsible for cleaning and normalizing task data before it enters the main scheduling pipeline.
    /// It ensures that tasks have sensible default values for critical properties like Importance,
    /// Estimated Duration, and Complexity, preventing errors in downstream calculations.
    /// </summary>
    public class TaskNormalizationStage : ISchedulingStage
    {
        public SchedulingContext Act(SchedulingContext context)
        {
            // Retrieve the tasks list from the shared context.
            if (!context.SharedState.TryGetValue("Tasks", out var tasksObj) || tasksObj is not List<TaskItem> tasks)
            {
                context.History.Add("TaskNormalizationStage: No valid task list found in context. Nothing to normalize.");
                return context;
            }

            context.History.Add("TaskNormalizationStage: Normalizing tasks and applying default values...");

            foreach (var task in tasks)
            {
                // Ensure Importance has a baseline value.
                if (task.Importance == 0)
                    task.Importance = 1;

                // Ensure EstimatedDuration is a positive, non-zero value.
                if (task.EstimatedDuration <= TimeSpan.Zero)
                    task.EstimatedDuration = TimeSpan.FromHours(1);

                // Ensure Complexity has a baseline value.
                if (task.Complexity <= 0)
                    task.Complexity = 1;

                // An inverted NotBefore/DueDate range would make the task permanently unschedulable,
                // so clamp NotBefore back to DueDate as a safe default.
                if (task.NotBefore.HasValue && task.DueDate.HasValue && task.NotBefore.Value > task.DueDate.Value)
                {
                    task.NotBefore = task.DueDate;
                    context.History.Add($"  -> Task '{task.Title}' had NotBefore after DueDate; clamped NotBefore to DueDate.");
                }
            }

            context.History.Add("TaskNormalizationStage: Task normalization complete. Defaults applied.");

            return context;
        }
    }
}

using System.Collections.Generic;

namespace PriorityTaskManager.Models
{
    /// <summary>
    /// Encapsulates the output of a scheduling strategy execution.
    /// </summary>
    public class PrioritizationResult
    {
        /// <summary>
        /// Gets or sets the list of tasks with their `ScheduledParts` populated.
        /// This represents the final, ordered schedule.
        /// </summary>
        public List<TaskItem> Tasks { get; set; } = new List<TaskItem>();

        /// <summary>
        /// Gets or sets a log of decisions and steps taken by the scheduling strategy.
        /// Useful for debugging and providing user-facing explanations.
        /// </summary>
        public List<string> History { get; set; } = new List<string>();

        /// <summary>
        /// Gets or sets a list of tasks that the scheduling strategy could not fit into the schedule.
        /// </summary>
        public List<TaskItem> UnscheduledTasks { get; set; } = new List<TaskItem>();
    }
}

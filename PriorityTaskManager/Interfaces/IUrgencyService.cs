using System.Collections.Generic;
using PriorityTaskManager.Models;

namespace PriorityTaskManager.Interfaces
{
    /// <summary>
    /// Interface for urgency calculation services.
    /// </summary>
    public interface IUrgencyService
    {
        /// <summary>
        /// Calculates and applies urgency scores to a collection of tasks.
        /// </summary>
        /// <param name="tasks">The collection of tasks to process.</param>
        void CalculateAndApplyUrgency(IEnumerable<TaskItem> tasks);
    }
}

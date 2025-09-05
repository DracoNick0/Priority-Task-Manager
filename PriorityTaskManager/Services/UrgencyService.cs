using System.Collections.Generic;
using PriorityTaskManager.Interfaces;
using PriorityTaskManager.Models;

namespace PriorityTaskManager.Services
{
    /// <summary>
    /// Service for calculating and applying urgency scores to tasks.
    /// </summary>
    public class UrgencyService : IUrgencyService
    {
        /// <inheritdoc />
        public void CalculateAndApplyUrgency(IEnumerable<TaskItem> tasks)
        {
            // Implementation will be added later.
        }
    }
}

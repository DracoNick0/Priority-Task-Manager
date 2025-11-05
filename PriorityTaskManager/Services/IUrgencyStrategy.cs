using System.Collections.Generic;
using PriorityTaskManager.Models;

namespace PriorityTaskManager.Services
{
    public interface IUrgencyStrategy
    {
        PrioritizationResult CalculateUrgency(List<TaskItem> tasks);
    }
}
using System.Collections.Generic;
using PriorityTaskManager.Models;

namespace PriorityTaskManager.Services
{
    public interface IUrgencyStrategy
    {
        List<TaskItem> CalculateUrgency(List<TaskItem> tasks);
    }
}
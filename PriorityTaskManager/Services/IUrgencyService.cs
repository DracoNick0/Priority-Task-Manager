using System.Collections.Generic;
using PriorityTaskManager.Models;

namespace PriorityTaskManager.Services
{
    public interface IUrgencyService
    {
        void CalculateUrgencyForAllTasks(List<TaskItem> tasks);
    }
}

using System.Collections.Generic;
using PriorityTaskManager.Models;

namespace PriorityTaskManager.Services
{
    public class TaskManagerService
    {
        private readonly List<TaskItem> _tasks = new List<TaskItem>();

        public void AddTask(TaskItem task)
        {
            _tasks.Add(task);
        }

        public int GetTaskCount()
        {
            return _tasks.Count;
        }
    }
}

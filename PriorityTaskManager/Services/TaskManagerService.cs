using System.Collections.Generic;
using PriorityTaskManager.Models;

namespace PriorityTaskManager.Services
{
    public class TaskManagerService
    {
        private readonly List<TaskItem> _tasks = new List<TaskItem>();
        private int _nextId = 1;

        public void AddTask(TaskItem task)
        {
            task.Id = _nextId++;
            _tasks.Add(task);
        }

        public IEnumerable<TaskItem> GetAllTasks()
        {
            return new List<TaskItem>(_tasks);
        }

        public TaskItem? GetTaskById(int id)
        {
            return _tasks.Find(t => t.Id == id);
        }

        public bool UpdateTask(TaskItem updatedTask)
        {
            var existingTask = _tasks.Find(t => t.Id == updatedTask.Id);
            if (existingTask == null)
                return false;
            existingTask.Title = updatedTask.Title;
            existingTask.Description = updatedTask.Description;
            existingTask.Importance = updatedTask.Importance;
            existingTask.DueDate = updatedTask.DueDate;
            existingTask.IsCompleted = updatedTask.IsCompleted;
            return true;
        }

        public bool DeleteTask(int id)
        {
            var task = _tasks.Find(t => t.Id == id);
            if (task == null)
                return false;
            _tasks.Remove(task);
            return true;
        }

        public int GetTaskCount()
        {
            return _tasks.Count;
        }
    }
}

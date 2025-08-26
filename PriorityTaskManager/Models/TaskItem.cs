using System;

namespace PriorityTaskManager.Models
{
    public enum ImportanceLevel
    {
        Low,
        Medium,
        High
    }

    public class TaskItem
    {
        public int Id { get; set; }
        public string? Title { get; set; }
        public string? Description { get; set; }
        public ImportanceLevel Importance { get; set; }
        public DateTime DueDate { get; set; }
        public bool IsCompleted { get; set; }
    }
}

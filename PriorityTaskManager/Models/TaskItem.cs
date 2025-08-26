using System;

namespace PriorityTaskManager.Models
{
    public class TaskItem
    {
        public int Id { get; set; }
        public string? Title { get; set; }
        public string? Description { get; set; }
        public int Importance { get; set; }
        public DateTime DueDate { get; set; }
        public bool IsCompleted { get; set; }

        public TimeSpan EstimatedDuration { get; set; }
        public double Progress { get; set; } // 0.0 to 1.0
        public List<int> Dependencies { get; set; } = new List<int>();

        public double UrgencyScore { get; set; }
        public DateTime LatestPossibleStartDate { get; set; }
    }
}

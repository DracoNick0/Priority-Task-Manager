namespace PriorityTaskManager.Models
{
    public class TaskItem
    {
        public int Id { get; set; }
        private string _title = string.Empty;
        public string Title
        {
            get => _title;
            set
            {
                if (string.IsNullOrWhiteSpace(value))
                    throw new ArgumentException("Task title cannot be null, empty, or whitespace.");
                _title = value;
            }
        }
        public string? Description { get; set; }
        public int Importance { get; set; }
        public DateTime DueDate { get; set; }
        public bool IsCompleted { get; set; }

        public TimeSpan EstimatedDuration { get; set; }
        public double Progress { get; set; }
        public List<int> Dependencies { get; set; } = new List<int>();

        public double UrgencyScore { get; set; }
        public DateTime LatestPossibleStartDate { get; set; }
    }
}

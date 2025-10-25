namespace PriorityTaskManager.Models
{
    /// <summary>
    /// Represents a task item with various properties such as title, description, due date, and more.
    /// </summary>
    public class TaskItem
    {
        /// <summary>
        /// Initializes a new instance of the TaskItem class.
        /// </summary>
        public TaskItem()
        {
            Title = string.Empty;
            Description = string.Empty;
            DueDate = DateTime.Today.AddDays(1);
            EstimatedDuration = TimeSpan.FromHours(1);
            Importance = 5;
            EffectiveImportance = 0;
            Progress = 0.0;
            IsCompleted = false;
            ListName = "General";
            Dependencies = new List<int>();
            UrgencyScore = 0;
            
            // Advanced scheduling defaults
            IsPinned = false;
            Complexity = 1.0;
            Points = 0.0;
            BeforePadding = null;
            AfterPadding = null;
            ScheduledStartTime = null;
            ScheduledEndTime = null;
        }

        /// <summary>
        /// Gets or sets whether the task is pinned and should not be rescheduled automatically.
        /// </summary>
        public bool IsPinned { get; set; }

        /// <summary>
        /// Gets or sets the complexity of the task (arbitrary scale, default 1.0).
        /// </summary>
        public double Complexity { get; set; }

        /// <summary>
        /// Gets or sets the point value of the task (for gamification or effort tracking).
        /// </summary>
        public double Points { get; set; }

        /// <summary>
        /// Gets or sets the required padding before the task (e.g., setup time).
        /// </summary>
        public TimeSpan? BeforePadding { get; set; }

        /// <summary>
        /// Gets or sets the required padding after the task (e.g., cooldown time).
        /// </summary>
        public TimeSpan? AfterPadding { get; set; }

        /// <summary>
        /// Gets or sets the scheduled start time for the task (if assigned).
        /// </summary>
        public DateTime? ScheduledStartTime { get; set; }

        /// <summary>
        /// Gets or sets the scheduled end time for the task (if assigned).
        /// </summary>
        public DateTime? ScheduledEndTime { get; set; }

        /// <summary>
        /// The name of the list this task belongs to.
        /// </summary>
        public string ListName { get; set; }

        /// <summary>
        /// Gets or sets the effective importance used for urgency calculation.
        /// </summary>
        public int EffectiveImportance { get; set; }

        /// <summary>
        /// Gets or sets the unique identifier for the task.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the title of the task.
        /// </summary>
        public string? Title { get; set; }

        /// <summary>
        /// Gets or sets the description of the task.
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Gets or sets the importance level of the task.
        /// </summary>
        public int Importance { get; set; }

        /// <summary>
        /// Gets or sets the due date of the task.
        /// </summary>
        public DateTime DueDate { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the task is completed.
        /// </summary>
        public bool IsCompleted { get; set; }

        /// <summary>
        /// Gets or sets the estimated duration to complete the task.
        /// </summary>
        public TimeSpan EstimatedDuration { get; set; }

        /// <summary>
        /// Gets or sets the progress of the task as a percentage (0.0 to 1.0).
        /// </summary>
        public double Progress { get; set; }

        /// <summary>
        /// Gets or sets the list of task IDs that this task depends on.
        /// </summary>
        public List<int> Dependencies { get; set; } = new List<int>();

        /// <summary>
        /// Gets or sets the urgency score of the task, calculated based on various factors.
        /// </summary>
        public double UrgencyScore { get; set; }

        /// <summary>
        /// Gets or sets the latest possible start date for the task to ensure timely completion.
        /// </summary>
        public DateTime LatestPossibleStartDate { get; set; }

        /// <summary>
        /// Gets or sets the numeric ID of the list to which the task belongs.
        /// </summary>
        public int ListId { get; set; }

        /// <summary>
        /// Gets or sets the display ID of the task, used for identification in the UI.
        /// </summary>
        public int DisplayId { get; set; }
    }
}

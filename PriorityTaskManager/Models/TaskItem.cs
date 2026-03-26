namespace PriorityTaskManager.Models
{
    /// <summary>
    /// Represents a single unit of work, containing all properties related to its scheduling, tracking, and metadata.
    /// </summary>
    public class TaskItem
    {
        /// <summary>
        /// Creates a deep copy of this <see cref="TaskItem"/> instance.
        /// </summary>
        /// <returns>A new `TaskItem` with the same values, including a deep copy of lists.</returns>
        public TaskItem Clone()
        {
            return new TaskItem
            {
                Id = this.Id,
                Title = this.Title,
                Description = this.Description,
                Importance = this.Importance,
                EffectiveImportance = this.EffectiveImportance,
                DueDate = this.DueDate,
                IsCompleted = this.IsCompleted,
                EstimatedDuration = this.EstimatedDuration,
                Progress = this.Progress,
                ListName = this.ListName,
                Dependencies = new List<int>(this.Dependencies),
                UrgencyScore = this.UrgencyScore,
                LatestPossibleStartDate = this.LatestPossibleStartDate,
                ListId = this.ListId,
                DisplayId = this.DisplayId,
                // Advanced scheduling
                IsPinned = this.IsPinned,
                Complexity = this.Complexity,
                Points = this.Points,
                BeforePadding = this.BeforePadding,
                AfterPadding = this.AfterPadding,
                IsDivisible = this.IsDivisible,
                ScheduledParts = new List<ScheduledChunk>(this.ScheduledParts.Select(c => c.Clone()))
            };
        }
        /// <summary>
        /// Initializes a new instance of the <see cref="TaskItem"/> class with default values.
        /// </summary>
        public TaskItem()
        {
            Title = string.Empty;
            Description = string.Empty;
            DueDate = null; // Default to no due date
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
            IsDivisible = true;
            ScheduledParts = new List<ScheduledChunk>();
        }

        #region Core Properties

        /// <summary>
        /// Gets or sets the unique, persistent identifier for the task.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the user-friendly identifier for the task, used for display and interaction in the UI.
        /// </summary>
        public int DisplayId { get; set; }

        /// <summary>
        /// Gets or sets the user-facing title of the task.
        /// </summary>
        public string? Title { get; set; }

        /// <summary>
        /// Gets or sets the detailed description or notes for the task.
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the task has been marked as complete.
        /// </summary>
        public bool IsCompleted { get; set; }

        /// <summary>
        /// Gets or sets the completion progress of the task, represented as a value from 0.0 (not started) to 1.0 (completed).
        /// </summary>
        public double Progress { get; set; }

        #endregion

        #region List & Grouping

        /// <summary>
        /// Gets or sets the numeric ID of the `TaskList` to which this task belongs.
        /// </summary>
        public int ListId { get; set; }

        /// <summary>
        /// Gets or sets the name of the list this task belongs to.
        /// </summary>
        public string ListName { get; set; }

        #endregion

        #region Time & Duration

        /// <summary>
        /// Gets or sets the date and time by which the task must be completed. Can be null if there is no deadline.
        /// </summary>
        public DateTime? DueDate { get; set; }

        /// <summary>
        /// Gets or sets the total estimated time required to complete the task.
        /// </summary>
        public TimeSpan EstimatedDuration { get; set; }

        /// <summary>
        /// Gets or sets the latest possible date the task can start to still be completed by its due date.
        /// This is calculated by the scheduling engine.
        /// </summary>
        public DateTime? LatestPossibleStartDate { get; set; }

        #endregion

        #region Scheduling & Prioritization

        /// <summary>
        /// Gets or sets the user-defined importance of the task, typically on a scale from 1 to 10.
        /// </summary>
        public int Importance { get; set; }

        /// <summary>
        /// Gets or sets the cognitive load or mental effort required for the task, on an arbitrary scale.
        /// Higher values represent more demanding tasks. Defaults to 1.0.
        /// </summary>
        public double Complexity { get; set; }

        /// <summary>
        /// Gets or sets the list of other task IDs that must be completed before this task can begin.
        /// </summary>
        public List<int> Dependencies { get; set; } = new List<int>();

        /// <summary>
        /// Gets or sets the calculated urgency score, used by scheduling strategies to prioritize tasks.
        /// </summary>
        public double UrgencyScore { get; set; }

        /// <summary>
        /// Gets or sets the calculated importance used by the scheduling engine, which may be adjusted from the base `Importance`.
        /// </summary>
        public double EffectiveImportance { get; set; }

        /// <summary>
        /// Gets or sets an optional point value for the task, useful for gamification or effort tracking.
        /// </summary>
        public double Points { get; set; }

        #endregion

        #region Advanced Scheduling

        /// <summary>
        /// Gets or sets a value indicating whether the task can be broken into smaller chunks during scheduling.
        /// Defaults to true.
        /// </summary>
        public bool IsDivisible { get; set; }

        /// <summary>
        /// Gets or sets the list of time blocks this task is scheduled for.
        /// This is populated by the scheduling engine and represents the final output for this task.
        /// </summary>
        public List<ScheduledChunk> ScheduledParts { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the task is pinned to its current schedule
        /// and should be ignored by the automatic scheduling engine.
        /// </summary>
        public bool IsPinned { get; set; }

        /// <summary>
        /// Gets or sets a required time buffer that must be scheduled before this task begins (e.g., for setup or travel).
        /// </summary>
        public TimeSpan? BeforePadding { get; set; }

        /// <summary>
        /// Gets or sets a required time buffer that must be scheduled after this task ends (e.g., for cleanup or cooldown).
        /// </summary>
        public TimeSpan? AfterPadding { get; set; }

        #endregion
    }
}

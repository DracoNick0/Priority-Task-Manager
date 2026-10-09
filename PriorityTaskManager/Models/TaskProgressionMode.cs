namespace PriorityTaskManager.Models
{
    /// <summary>Controls how unresolved occurrences of a recurring task are advanced.</summary>
    public enum TaskProgressionMode
    {
        /// <summary>Wait for the oldest unresolved occurrence before exposing later ones.</summary>
        SequentialCatchUp,

        /// <summary>Advance automatically and keep each missed occurrence as actionable backlog.</summary>
        RollForwardKeepBacklog,

        /// <summary>Advance automatically and disregard missed occurrences.</summary>
        RollForwardDisregard
    }

    /// <summary>Describes the persisted state of one scheduled task occurrence.</summary>
    public enum TaskOccurrenceStatus
    {
        Pending,
        Missed,
        Completed,
        Skipped,
        Disregarded
    }

    /// <summary>Stores completion progress for a single recurrence-rule date.</summary>
    public sealed class TaskOccurrenceState
    {
        /// <summary>Gets or sets the recurrence-rule date this state belongs to.</summary>
        public DateTime ScheduledDate { get; set; }

        /// <summary>Gets or sets the occurrence's persisted status.</summary>
        public TaskOccurrenceStatus Status { get; set; }

        /// <summary>Gets or sets the number of completion actions applied to this occurrence.</summary>
        public int CompletionCount { get; set; }

        /// <summary>Gets or sets the time the occurrence reached its required completion count.</summary>
        public DateTime? CompletedAt { get; set; }
    }

    /// <summary>A recurring task occurrence projected for a requested date window.</summary>
    public sealed record TaskOccurrence(
        TaskItem Series,
        DateTime ScheduledDate,
        TaskOccurrenceStatus Status,
        int CompletionCount,
        bool IsMissed,
        int CurrentStreak,
        int BestStreak);
}

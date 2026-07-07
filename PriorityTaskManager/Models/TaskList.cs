using PriorityTaskManager.Models;

namespace PriorityTaskManager.Models
{
    /// <summary>
    /// Represents a named container for a collection of tasks.
    /// </summary>
    public class TaskList
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TaskList"/> class with default values.
        /// </summary>
        public TaskList()
        {
            Name = string.Empty;
        }

        /// <summary>
        /// Gets or sets the unique numeric identifier for the task list.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the user-defined name of the task list.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the optional description for the task list.
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Gets or sets the preferred sorting option for displaying tasks within this list.
        /// </summary>
        public SortOption? SortOption { get; set; }

        /// <summary>
        /// Gets or sets the preferred scheduling mode for this list.
        /// </summary>
        public SchedulingMode? SchedulingMode { get; set; }

        /// <summary>
        /// Gets or sets the preferred start time for list-level scheduling.
        /// </summary>
        public TimeOnly? WorkStartTime { get; set; }

        /// <summary>
        /// Gets or sets the preferred end time for list-level scheduling.
        /// </summary>
        public TimeOnly? WorkEndTime { get; set; }

        /// <summary>
        /// Gets or sets the preferred workdays for this list.
        /// </summary>
        public List<DayOfWeek>? WorkDays { get; set; }

        /// <summary>
        /// Gets or sets the list-specific urgency threshold for the 'Dire' band.
        /// </summary>
        public double? SlackThresholdDire { get; set; }

        /// <summary>
        /// Gets or sets the list-specific urgency threshold for the 'Pressing' band.
        /// </summary>
        public double? SlackThresholdPressing { get; set; }

        /// <summary>
        /// Gets or sets the list-specific urgency threshold for the 'Focus' band.
        /// </summary>
        public double? SlackThresholdFocus { get; set; }

        /// <summary>
        /// Gets or sets the list-specific urgency threshold for the 'Safe' band.
        /// </summary>
        public double? SlackThresholdSafe { get; set; }

        /// <summary>
        /// Gets or sets the list-specific simulated time preference.
        /// </summary>
        public DateTime? SimulatedTime { get; set; }

        /// <summary>
        /// Copies missing settings from the supplied profile.
        /// </summary>
        /// <param name="profile">The source profile for defaults.</param>
        public void ApplyDefaultsFrom(UserProfile profile)
        {
            Description ??= string.Empty;
            SortOption ??= profile.DefaultListSortOption;
            SchedulingMode ??= profile.SchedulingMode;
            WorkStartTime ??= profile.WorkStartTime;
            WorkEndTime ??= profile.WorkEndTime;
            WorkDays ??= new List<DayOfWeek>(profile.WorkDays);
            SlackThresholdDire ??= profile.SlackThresholdDire;
            SlackThresholdPressing ??= profile.SlackThresholdPressing;
            SlackThresholdFocus ??= profile.SlackThresholdFocus;
            SlackThresholdSafe ??= profile.SlackThresholdSafe;
        }

    }
}

namespace PriorityTaskManager.Models
{
    /// <summary>
    /// Contains user-specific settings and preferences that influence application behavior, especially scheduling.
    /// </summary>
    public class UserProfile
    {
        /// <summary>
        /// Gets or sets the user's preferred minimum break duration between tasks. Defaults to 15 minutes.
        /// </summary>
        public TimeSpan DesiredBreatherDuration { get; set; } = TimeSpan.FromMinutes(15);

        /// <summary>
        /// Gets or sets the time of day the user's work typically starts. Defaults to 9:00 AM.
        /// </summary>
        public TimeOnly WorkStartTime { get; set; } = new TimeOnly(9, 0);

        /// <summary>
        /// Gets or sets the time of day the user's work typically ends. Defaults to 5:00 PM.
        /// </summary>
        public TimeOnly WorkEndTime { get; set; } = new TimeOnly(17, 0);

        /// <summary>
        /// Gets or sets the days of the week the user typically works. Defaults to Monday through Friday.
        /// </summary>
        public List<DayOfWeek> WorkDays { get; set; } = new List<DayOfWeek>
        {
            DayOfWeek.Monday,
            DayOfWeek.Tuesday,
            DayOfWeek.Wednesday,
            DayOfWeek.Thursday,
            DayOfWeek.Friday
        };

        /// <summary>
        /// Gets or sets the scheduling algorithm to use. Defaults to the `GoldPanning` strategy.
        /// </summary>
        public SchedulingMode SchedulingMode { get; set; } = SchedulingMode.GoldPanning;

        /// <summary>
        /// Gets or sets the multiplier of the average workday used to define the 'Dire' (Dark Red) urgency threshold.
        /// A task is 'Dire' if its deadline is within (Average Workday * this value). Defaults to 0.5.
        /// </summary>
        public double SlackThresholdDire { get; set; } = 0.5;

        /// <summary>
        /// Gets or sets the multiplier for the 'Pressing' (Red) urgency threshold. Defaults to 1.0.
        /// </summary>
        public double SlackThresholdPressing { get; set; } = 1.0;

        /// <summary>
        /// Gets or sets the multiplier for the 'Focus' (Yellow) urgency threshold. Defaults to 3.0.
        /// </summary>
        public double SlackThresholdFocus { get; set; } = 3.0;

        /// <summary>
        /// Gets or sets the multiplier for the 'Safe' (Green) urgency threshold. Defaults to 5.0.
        /// </summary>
        public double SlackThresholdSafe { get; set; } = 5.0;
    }
}

namespace PriorityTaskManager.Models
{
    public class UserProfile
    {
        /// <summary>
        /// The user's preferred minimum break duration between tasks. Defaults to 15 minutes.
        /// </summary>
        public TimeSpan DesiredBreatherDuration { get; set; } = TimeSpan.FromMinutes(15);

        /// <summary>
        /// The time of day the user's work typically starts. Defaults to 09:00.
        /// </summary>
        public TimeOnly WorkStartTime { get; set; } = new TimeOnly(9, 0);

        /// <summary>
        /// The time of day the user's work typically ends. Defaults to 17:00.
        /// </summary>
        public TimeOnly WorkEndTime { get; set; } = new TimeOnly(17, 0);

        /// <summary>
        /// The days of the week the user typically works. Defaults to Monday-Friday.
        /// </summary>
        public List<DayOfWeek> WorkDays { get; set; } = new List<DayOfWeek>
        {
            DayOfWeek.Monday,
            DayOfWeek.Tuesday,
            DayOfWeek.Wednesday,
            DayOfWeek.Thursday,
            DayOfWeek.Friday
        };
    }
}

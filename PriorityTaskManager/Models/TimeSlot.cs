namespace PriorityTaskManager.Models
{
    /// <summary>
    /// Represents a simple, continuous block of time with a start and an end.
    /// </summary>
    public class TimeSlot
    {
        /// <summary>
        /// Gets or sets the start time of the slot.
        /// </summary>
        public DateTime StartTime { get; set; }

        /// <summary>
        /// Gets or sets the end time of the slot.
        /// </summary>
        public DateTime EndTime { get; set; }

        /// <summary>
        /// Gets the calculated duration of the time slot.
        /// </summary>
        public TimeSpan Duration => EndTime - StartTime;
    }
}

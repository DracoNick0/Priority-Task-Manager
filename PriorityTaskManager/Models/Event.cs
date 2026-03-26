namespace PriorityTaskManager.Models
{
    /// <summary>
    /// Represents a block of time that is unavailable for scheduling tasks, such as a meeting or appointment.
    /// </summary>
    public class Event
    {
        /// <summary>
        /// Gets or sets the unique identifier for the event.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the name or title of the event.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the date and time the event begins.
        /// </summary>
        public DateTime StartTime { get; set; }

        /// <summary>
        /// Gets or sets the date and time the event ends.
        /// </summary>
        public DateTime EndTime { get; set; }
    }
}

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
        public Guid Id { get; set; }

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

        /// <summary>
        /// Gets or sets the recurrence pattern for this event, if it is the base occurrence of a recurring series.
        /// Null for a plain, non-recurring event.
        /// </summary>
        public RecurrenceRule? RecurrenceRule { get; set; }

        /// <summary>
        /// Gets or sets the series identifier shared by every occurrence expanded from <see cref="RecurrenceRule"/>.
        /// Set to this event's own <see cref="Id"/> when it defines a recurring series; null otherwise.
        /// </summary>
        public Guid? SeriesId { get; set; }

        /// <summary>
        /// Gets or sets the recorded per-occurrence exceptions (e.g. cancellations) for this series.
        /// Empty for a non-recurring event.
        /// </summary>
        public List<RecurrenceException> Exceptions { get; set; } = new();

        /// <summary>
        /// Gets or sets the recorded per-occurrence edits ("this occurrence only") for this series.
        /// Empty for a non-recurring event.
        /// </summary>
        public List<EventOccurrenceOverride> OccurrenceOverrides { get; set; } = new();
    }

    /// <summary>A displayed event and the original recurrence date used to address its series.</summary>
    public record EventOccurrence(Event Event, DateTime? OriginalOccurrenceDate);
}

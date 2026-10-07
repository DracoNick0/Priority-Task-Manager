namespace PriorityTaskManager.Models
{
    /// <summary>
    /// Records a per-occurrence edit ("this occurrence only") for a recurring event series, keyed by the
    /// occurrence's original (unmodified) date. Additive to <see cref="RecurrenceException"/>, which only
    /// tracks cancellations; only occurrences actually edited get an entry.
    /// </summary>
    public class EventOccurrenceOverride
    {
        /// <summary>
        /// Gets or sets the date the occurrence would have fallen on before this override was recorded.
        /// </summary>
        public DateTime OriginalOccurrenceDate { get; set; }

        /// <summary>
        /// Gets or sets the overridden name for this occurrence.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the overridden description for this occurrence.
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Gets or sets the overridden link for this occurrence.
        /// </summary>
        public string? Link { get; set; }

        /// <summary>
        /// Gets or sets the overridden start time for this occurrence.
        /// </summary>
        public DateTime StartTime { get; set; }

        /// <summary>
        /// Gets or sets the overridden end time for this occurrence.
        /// </summary>
        public DateTime EndTime { get; set; }

        /// <summary>
        /// Creates a deep copy of this override.
        /// </summary>
        public EventOccurrenceOverride Clone() => new EventOccurrenceOverride
        {
            OriginalOccurrenceDate = OriginalOccurrenceDate,
            Name = Name,
            Description = Description,
            Link = Link,
            StartTime = StartTime,
            EndTime = EndTime
        };
    }
}

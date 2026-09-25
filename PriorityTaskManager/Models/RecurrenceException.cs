namespace PriorityTaskManager.Models
{
    /// <summary>
    /// Records a single occurrence of a recurring series that was cancelled ("this occurrence only"
    /// delete), keyed by the occurrence's original (unmodified) date. Only occurrences actually acted
    /// on get an entry; future occurrences are never bulk-materialized or persisted ahead of time.
    /// A consumer that also needs to store per-occurrence edits (a modified date/title/etc. for "this
    /// occurrence only") layers its own override data keyed by the same <see cref="OriginalOccurrenceDate"/>
    /// rather than this type growing consumer-specific fields.
    /// </summary>
    public class RecurrenceException
    {
        /// <summary>
        /// Gets or sets the date the occurrence would have fallen on before this exception was recorded.
        /// </summary>
        public DateTime OriginalOccurrenceDate { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this occurrence was cancelled outright (excluded from
        /// expansion). When false, the occurrence still expands but a consumer-owned override applies to it.
        /// </summary>
        public bool IsCancelled { get; set; }

        /// <summary>
        /// Creates a deep copy of this exception.
        /// </summary>
        public RecurrenceException Clone() => new RecurrenceException
        {
            OriginalOccurrenceDate = OriginalOccurrenceDate,
            IsCancelled = IsCancelled
        };
    }
}

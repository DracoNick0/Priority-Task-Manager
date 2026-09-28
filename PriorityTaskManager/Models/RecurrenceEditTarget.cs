namespace PriorityTaskManager.Models
{
    /// <summary>
    /// Specifies which occurrences of a recurring series an edit or delete operation applies to.
    /// </summary>
    public enum RecurrenceEditTarget
    {
        /// <summary>Only the single targeted occurrence is affected.</summary>
        ThisOccurrence,

        /// <summary>The targeted occurrence and every later occurrence are affected; earlier occurrences are untouched.</summary>
        ThisAndFollowing,

        /// <summary>Every occurrence in the series is affected.</summary>
        AllOccurrences
    }
}

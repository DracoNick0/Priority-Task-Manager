using PriorityTaskManager.Models;

namespace PriorityTaskManager.Services
{
    /// <summary>
    /// Expands a <see cref="RecurrenceRule"/> into concrete occurrence dates on demand for a caller-supplied
    /// date range. Occurrences are never persisted ahead of time; every call recomputes them from the rule.
    /// </summary>
    public interface IRecurrenceExpansionService
    {
        /// <summary>
        /// Computes the occurrence dates for <paramref name="rule"/> that fall within
        /// [<paramref name="rangeStart"/>, <paramref name="rangeEnd"/>] (inclusive, date-only comparison),
        /// honoring the rule's end condition and excluding any date recorded as cancelled in <paramref name="exceptions"/>.
        /// No occurrence earlier than <see cref="RecurrenceRule.SeriesStartDate"/> is ever produced.
        /// </summary>
        /// <param name="rule">The recurrence rule to expand.</param>
        /// <param name="exceptions">Recorded per-occurrence exceptions for this series.</param>
        /// <param name="rangeStart">The inclusive start of the date range to expand into.</param>
        /// <param name="rangeEnd">The inclusive end of the date range to expand into.</param>
        /// <returns>Occurrence dates in ascending order.</returns>
        IReadOnlyList<DateTime> GetOccurrences(RecurrenceRule rule, IEnumerable<RecurrenceException> exceptions, DateTime rangeStart, DateTime rangeEnd);
    }
}

using PriorityTaskManager.Models;

namespace PriorityTaskManager.Services.Helpers
{
    /// <summary>
    /// Holds the two series produced by splitting a recurring series at a given occurrence for a
    /// "this and following" edit/delete: the closed-off prior series and the new series continuing from it.
    /// </summary>
    public class RecurrenceSplitResult
    {
        /// <summary>Gets the prior series, now ending the day before the split occurrence.</summary>
        public required RecurrenceRule PriorRule { get; init; }

        /// <summary>Gets the exceptions that stay attached to <see cref="PriorRule"/>.</summary>
        public required List<RecurrenceException> PriorExceptions { get; init; }

        /// <summary>Gets the new series, starting on the split occurrence date.</summary>
        public required RecurrenceRule NewRule { get; init; }

        /// <summary>Gets the exceptions that move to <see cref="NewRule"/>.</summary>
        public required List<RecurrenceException> NewExceptions { get; init; }
    }

    /// <summary>
    /// Implements the "this and following" per-occurrence edit/delete semantics shared by recurring
    /// tasks and recurring events: splits one series into a closed-off prior series and a new series
    /// that continues from the chosen occurrence, without disturbing prior series exceptions.
    /// </summary>
    public static class RecurrenceSplitHelper
    {
        /// <summary>
        /// Splits <paramref name="rule"/> at <paramref name="occurrenceDate"/>. The prior series' end
        /// condition is replaced with an <see cref="UntilDateEndCondition"/> ending the day before the
        /// split; the new series keeps the original pattern and end condition but starts on the split date.
        /// </summary>
        /// <param name="rule">The series being split.</param>
        /// <param name="exceptions">The series' existing exceptions, partitioned by date into the two results.</param>
        /// <param name="occurrenceDate">The occurrence date "this and following" applies from (inclusive).</param>
        public static RecurrenceSplitResult Split(RecurrenceRule rule, IEnumerable<RecurrenceException> exceptions, DateTime occurrenceDate)
        {
            if (rule == null) throw new ArgumentNullException(nameof(rule));
            if (exceptions == null) throw new ArgumentNullException(nameof(exceptions));

            var splitDate = occurrenceDate.Date;

            var priorRule = rule.Clone();
            priorRule.EndCondition = new UntilDateEndCondition { UntilDate = splitDate.AddDays(-1) };

            var newRule = rule.Clone();
            newRule.SeriesStartDate = splitDate;

            var priorExceptions = new List<RecurrenceException>();
            var newExceptions = new List<RecurrenceException>();
            foreach (var exception in exceptions)
            {
                var target = exception.OriginalOccurrenceDate.Date < splitDate ? priorExceptions : newExceptions;
                target.Add(exception.Clone());
            }

            return new RecurrenceSplitResult
            {
                PriorRule = priorRule,
                PriorExceptions = priorExceptions,
                NewRule = newRule,
                NewExceptions = newExceptions
            };
        }
    }
}

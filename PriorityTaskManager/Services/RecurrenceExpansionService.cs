using PriorityTaskManager.Models;

namespace PriorityTaskManager.Services
{
    /// <inheritdoc cref="IRecurrenceExpansionService" />
    public class RecurrenceExpansionService : IRecurrenceExpansionService
    {
        /// <inheritdoc />
        public IReadOnlyList<DateTime> GetOccurrences(RecurrenceRule rule, IEnumerable<RecurrenceException> exceptions, DateTime rangeStart, DateTime rangeEnd)
        {
            if (rule == null) throw new ArgumentNullException(nameof(rule));
            if (exceptions == null) throw new ArgumentNullException(nameof(exceptions));

            var rangeStartDate = rangeStart.Date;
            var rangeEndDate = rangeEnd.Date;
            if (rangeEndDate < rangeStartDate)
                return Array.Empty<DateTime>();

            var effectiveStart = rule.SeriesStartDate.Date > rangeStartDate ? rule.SeriesStartDate.Date : rangeStartDate;

            var hardCap = rangeEndDate;
            if (rule.EndCondition is UntilDateEndCondition until && until.UntilDate.Date < hardCap)
                hardCap = until.UntilDate.Date;

            if (effectiveStart > hardCap)
                return Array.Empty<DateTime>();

            var maxOccurrences = rule.EndCondition is AfterOccurrencesEndCondition after ? after.OccurrenceCount : int.MaxValue;

            // Only iterate from the series start (instead of the requested range start) when occurrence
            // counting from the true beginning of the series is required to honor an occurrence-count limit.
            var iterationStart = maxOccurrences == int.MaxValue ? effectiveStart : rule.SeriesStartDate.Date;

            var cancelledDates = new HashSet<DateTime>(exceptions.Where(e => e.IsCancelled).Select(e => e.OriginalOccurrenceDate.Date));

            var result = new List<DateTime>();
            var occurrenceIndex = 0;
            for (var date = iterationStart; date <= hardCap; date = date.AddDays(1))
            {
                if (!MatchesPattern(rule, date))
                    continue;

                occurrenceIndex++;
                if (occurrenceIndex > maxOccurrences)
                    break;

                if (date < effectiveStart || cancelledDates.Contains(date))
                    continue;

                result.Add(date);
            }

            return result;
        }

        private static bool MatchesPattern(RecurrenceRule rule, DateTime date)
        {
            switch (rule)
            {
                case DailyIntervalRecurrenceRule daily:
                    var intervalDays = Math.Max(1, daily.IntervalDays);
                    return (date.Date - rule.SeriesStartDate.Date).Days % intervalDays == 0;

                case WeeklyRecurrenceRule weekly:
                    if (!weekly.DaysOfWeek.Contains(date.DayOfWeek))
                        return false;
                    var intervalWeeks = Math.Max(1, weekly.IntervalWeeks);
                    var weeksBetween = (StartOfWeek(date) - StartOfWeek(rule.SeriesStartDate.Date)).Days / 7;
                    return weeksBetween >= 0 && weeksBetween % intervalWeeks == 0;

                case MonthlyOnDaysRecurrenceRule monthlyOnDays:
                    var daysInMonth = DateTime.DaysInMonth(date.Year, date.Month);
                    foreach (var configuredDay in monthlyOnDays.DaysOfMonth)
                    {
                        if (configuredDay <= daysInMonth)
                        {
                            if (date.Day == configuredDay)
                                return true;
                        }
                        else if (monthlyOnDays.ShortMonthBehavior == ShortMonthBehavior.ClampToLastDay && date.Day == daysInMonth)
                        {
                            return true;
                        }
                    }
                    return false;

                case MonthlyRelativeRecurrenceRule monthlyRelative:
                    var target = GetRelativeOccurrenceDate(date.Year, date.Month, monthlyRelative.Ordinal, monthlyRelative.DayKind, monthlyRelative.DayOfWeek);
                    return target.HasValue && target.Value.Date == date.Date;

                case YearlyRecurrenceRule yearly:
                    return date.Month == yearly.Month && date.Day == yearly.Day;

                case ExplicitDatesRecurrenceRule explicitDates:
                    return explicitDates.Dates.Any(d => d.Date == date.Date);

                default:
                    throw new NotSupportedException($"Unsupported recurrence rule type '{rule.GetType().Name}'.");
            }
        }

        private static DateTime StartOfWeek(DateTime date) => date.AddDays(-(int)date.DayOfWeek);

        private static DateTime? GetRelativeOccurrenceDate(int year, int month, RelativeWeekOrdinal ordinal, RelativeDayKind kind, DayOfWeek? dayOfWeek)
        {
            var daysInMonth = DateTime.DaysInMonth(year, month);
            var matchingDays = new List<DateTime>();
            for (var day = 1; day <= daysInMonth; day++)
            {
                var candidate = new DateTime(year, month, day);
                var matches = kind switch
                {
                    RelativeDayKind.SpecificDayOfWeek => dayOfWeek.HasValue && candidate.DayOfWeek == dayOfWeek.Value,
                    RelativeDayKind.AnyDay => true,
                    RelativeDayKind.Weekday => candidate.DayOfWeek is >= DayOfWeek.Monday and <= DayOfWeek.Friday,
                    RelativeDayKind.WeekendDay => candidate.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday,
                    _ => false
                };
                if (matches)
                    matchingDays.Add(candidate);
            }

            if (matchingDays.Count == 0)
                return null;

            if (ordinal == RelativeWeekOrdinal.Last)
                return matchingDays[^1];

            var index = ordinal switch
            {
                RelativeWeekOrdinal.First => 0,
                RelativeWeekOrdinal.Second => 1,
                RelativeWeekOrdinal.Third => 2,
                RelativeWeekOrdinal.Fourth => 3,
                _ => -1
            };

            return index >= 0 && index < matchingDays.Count ? matchingDays[index] : null;
        }
    }
}

using System.Text.Json.Serialization;

namespace PriorityTaskManager.Models
{
    /// <summary>
    /// Identifies how a <see cref="MonthlyOnDaysRecurrenceRule"/> handles a configured day-of-month
    /// that does not exist in a given month (e.g. day 31 in April).
    /// </summary>
    public enum ShortMonthBehavior
    {
        /// <summary>Skip that month entirely; it produces no occurrence.</summary>
        Skip,

        /// <summary>Clamp to the last valid day of that month.</summary>
        ClampToLastDay
    }

    /// <summary>
    /// Identifies which occurrence within a month a <see cref="MonthlyRelativeRecurrenceRule"/> targets.
    /// </summary>
    public enum RelativeWeekOrdinal
    {
        First,
        Second,
        Third,
        Fourth,
        Last
    }

    /// <summary>
    /// Identifies the kind of day a <see cref="MonthlyRelativeRecurrenceRule"/> targets within its ordinal week.
    /// </summary>
    public enum RelativeDayKind
    {
        /// <summary>A specific day of the week, given by <see cref="MonthlyRelativeRecurrenceRule.DayOfWeek"/>.</summary>
        SpecificDayOfWeek,

        /// <summary>Any calendar day (e.g. "the last day of the month").</summary>
        AnyDay,

        /// <summary>Any Monday-Friday day (e.g. "the last weekday").</summary>
        Weekday,

        /// <summary>Any Saturday/Sunday day.</summary>
        WeekendDay
    }

    /// <summary>
    /// Describes a recurring pattern shared by recurring tasks and recurring events: how often and on
    /// which dates a series repeats, plus when it stops. Modeled as a polymorphic hierarchy (an abstract
    /// base plus one sealed class per pattern type) rather than a single flat class with nullable fields
    /// per pattern, so each stored instance only serializes the fields its own pattern actually uses and
    /// new pattern types can be added additively without touching existing stored data.
    /// Concrete occurrence dates are never persisted; callers expand a rule on demand for whatever date
    /// range they need (see the recurrence expansion service).
    /// </summary>
    [JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
    [JsonDerivedType(typeof(DailyIntervalRecurrenceRule), typeDiscriminator: "dailyInterval")]
    [JsonDerivedType(typeof(WeeklyRecurrenceRule), typeDiscriminator: "weekly")]
    [JsonDerivedType(typeof(MonthlyOnDaysRecurrenceRule), typeDiscriminator: "monthlyOnDays")]
    [JsonDerivedType(typeof(MonthlyRelativeRecurrenceRule), typeDiscriminator: "monthlyRelative")]
    [JsonDerivedType(typeof(YearlyRecurrenceRule), typeDiscriminator: "yearly")]
    [JsonDerivedType(typeof(ExplicitDatesRecurrenceRule), typeDiscriminator: "explicitDates")]
    public abstract class RecurrenceRule
    {
        /// <summary>
        /// Gets or sets the date the series was created/started from. No occurrence is ever generated
        /// earlier than this date, regardless of what the current (possibly simulated) time is.
        /// </summary>
        public DateTime SeriesStartDate { get; set; }

        /// <summary>
        /// Gets or sets the condition under which the series stops producing occurrences.
        /// </summary>
        public RecurrenceEndCondition EndCondition { get; set; } = new NeverEndCondition();

        /// <summary>
        /// Creates a deep copy of this rule, including a deep copy of <see cref="EndCondition"/>.
        /// </summary>
        public abstract RecurrenceRule Clone();
    }

    /// <summary>
    /// Repeats every fixed number of days (e.g. every 3 days) starting from <see cref="RecurrenceRule.SeriesStartDate"/>.
    /// </summary>
    public sealed class DailyIntervalRecurrenceRule : RecurrenceRule
    {
        /// <summary>
        /// Gets or sets the number of days between occurrences. Must be at least 1.
        /// </summary>
        public int IntervalDays { get; set; } = 1;

        /// <inheritdoc />
        public override RecurrenceRule Clone() => new DailyIntervalRecurrenceRule
        {
            SeriesStartDate = SeriesStartDate,
            EndCondition = EndCondition.Clone(),
            IntervalDays = IntervalDays
        };
    }

    /// <summary>
    /// Repeats on one or more selected weekdays, every N weeks. <see cref="IntervalWeeks"/> of 1 covers
    /// both "every selected weekday(s)" and the "every weekday" shortcut (by selecting Monday-Friday).
    /// </summary>
    public sealed class WeeklyRecurrenceRule : RecurrenceRule
    {
        /// <summary>
        /// Gets or sets the days of the week the series occurs on. Must contain at least one day.
        /// </summary>
        public List<DayOfWeek> DaysOfWeek { get; set; } = new List<DayOfWeek>();

        /// <summary>
        /// Gets or sets the number of weeks between occurrence weeks. Must be at least 1.
        /// </summary>
        public int IntervalWeeks { get; set; } = 1;

        /// <inheritdoc />
        public override RecurrenceRule Clone() => new WeeklyRecurrenceRule
        {
            SeriesStartDate = SeriesStartDate,
            EndCondition = EndCondition.Clone(),
            DaysOfWeek = new List<DayOfWeek>(DaysOfWeek),
            IntervalWeeks = IntervalWeeks
        };
    }

    /// <summary>
    /// Repeats on one or more selected days of the month (e.g. the 1st and 15th).
    /// </summary>
    public sealed class MonthlyOnDaysRecurrenceRule : RecurrenceRule
    {
        /// <summary>
        /// Gets or sets the days of the month (1-31) the series occurs on. Must contain at least one day.
        /// </summary>
        public List<int> DaysOfMonth { get; set; } = new List<int>();

        /// <summary>
        /// Gets or sets how a day that does not exist in a given month (e.g. 31 in April) is handled.
        /// </summary>
        public ShortMonthBehavior ShortMonthBehavior { get; set; } = ShortMonthBehavior.Skip;

        /// <inheritdoc />
        public override RecurrenceRule Clone() => new MonthlyOnDaysRecurrenceRule
        {
            SeriesStartDate = SeriesStartDate,
            EndCondition = EndCondition.Clone(),
            DaysOfMonth = new List<int>(DaysOfMonth),
            ShortMonthBehavior = ShortMonthBehavior
        };
    }

    /// <summary>
    /// Repeats on a relative occurrence within the month (e.g. "the first Monday" or "the last weekday").
    /// </summary>
    public sealed class MonthlyRelativeRecurrenceRule : RecurrenceRule
    {
        /// <summary>
        /// Gets or sets which occurrence within the month is targeted.
        /// </summary>
        public RelativeWeekOrdinal Ordinal { get; set; }

        /// <summary>
        /// Gets or sets the kind of day targeted within the ordinal.
        /// </summary>
        public RelativeDayKind DayKind { get; set; }

        /// <summary>
        /// Gets or sets the specific day of week targeted when <see cref="DayKind"/> is
        /// <see cref="RelativeDayKind.SpecificDayOfWeek"/>; otherwise unused.
        /// </summary>
        public DayOfWeek? DayOfWeek { get; set; }

        /// <inheritdoc />
        public override RecurrenceRule Clone() => new MonthlyRelativeRecurrenceRule
        {
            SeriesStartDate = SeriesStartDate,
            EndCondition = EndCondition.Clone(),
            Ordinal = Ordinal,
            DayKind = DayKind,
            DayOfWeek = DayOfWeek
        };
    }

    /// <summary>
    /// Repeats once a year on a fixed month/day (e.g. every March 15th).
    /// </summary>
    public sealed class YearlyRecurrenceRule : RecurrenceRule
    {
        /// <summary>
        /// Gets or sets the month (1-12) the series occurs on.
        /// </summary>
        public int Month { get; set; }

        /// <summary>
        /// Gets or sets the day of that month the series occurs on.
        /// </summary>
        public int Day { get; set; }

        /// <inheritdoc />
        public override RecurrenceRule Clone() => new YearlyRecurrenceRule
        {
            SeriesStartDate = SeriesStartDate,
            EndCondition = EndCondition.Clone(),
            Month = Month,
            Day = Day
        };
    }

    /// <summary>
    /// Repeats on an explicit, caller-supplied list of dates rather than a computed pattern.
    /// </summary>
    public sealed class ExplicitDatesRecurrenceRule : RecurrenceRule
    {
        /// <summary>
        /// Gets or sets the explicit occurrence dates that make up this series.
        /// </summary>
        public List<DateTime> Dates { get; set; } = new List<DateTime>();

        /// <inheritdoc />
        public override RecurrenceRule Clone() => new ExplicitDatesRecurrenceRule
        {
            SeriesStartDate = SeriesStartDate,
            EndCondition = EndCondition.Clone(),
            Dates = new List<DateTime>(Dates)
        };
    }
}

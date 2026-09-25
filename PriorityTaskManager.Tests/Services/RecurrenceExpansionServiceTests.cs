using PriorityTaskManager.Models;
using PriorityTaskManager.Services;

namespace PriorityTaskManager.Tests.Services
{
    public class RecurrenceExpansionServiceTests
    {
        private readonly RecurrenceExpansionService _service = new();

        [Fact]
        public void GetOccurrences_DailyInterval_ReturnsEveryNthDay()
        {
            var rule = new DailyIntervalRecurrenceRule
            {
                SeriesStartDate = new DateTime(2026, 1, 1),
                IntervalDays = 3
            };

            var occurrences = _service.GetOccurrences(rule, new List<RecurrenceException>(), new DateTime(2026, 1, 1), new DateTime(2026, 1, 10));

            Assert.Equal(new[]
            {
                new DateTime(2026, 1, 1),
                new DateTime(2026, 1, 4),
                new DateTime(2026, 1, 7),
                new DateTime(2026, 1, 10)
            }, occurrences);
        }

        [Fact]
        public void GetOccurrences_WeeklyEveryOtherWeek_SkipsOffWeeks()
        {
            var rule = new WeeklyRecurrenceRule
            {
                SeriesStartDate = new DateTime(2026, 1, 5), // Monday
                DaysOfWeek = new List<DayOfWeek> { DayOfWeek.Monday, DayOfWeek.Wednesday },
                IntervalWeeks = 2
            };

            var occurrences = _service.GetOccurrences(rule, new List<RecurrenceException>(), new DateTime(2026, 1, 5), new DateTime(2026, 2, 1));

            Assert.Equal(new[]
            {
                new DateTime(2026, 1, 5),
                new DateTime(2026, 1, 7),
                new DateTime(2026, 1, 19),
                new DateTime(2026, 1, 21)
            }, occurrences);
        }

        [Fact]
        public void GetOccurrences_MonthlyOnDaysWithClamp_ClampsToLastDayInShortMonths()
        {
            var rule = new MonthlyOnDaysRecurrenceRule
            {
                SeriesStartDate = new DateTime(2026, 1, 1),
                DaysOfMonth = new List<int> { 31 },
                ShortMonthBehavior = ShortMonthBehavior.ClampToLastDay
            };

            var occurrences = _service.GetOccurrences(rule, new List<RecurrenceException>(), new DateTime(2026, 1, 1), new DateTime(2026, 4, 30));

            Assert.Equal(new[]
            {
                new DateTime(2026, 1, 31),
                new DateTime(2026, 2, 28),
                new DateTime(2026, 3, 31),
                new DateTime(2026, 4, 30)
            }, occurrences);
        }

        [Fact]
        public void GetOccurrences_MonthlyOnDaysWithSkip_SkipsShortMonths()
        {
            var rule = new MonthlyOnDaysRecurrenceRule
            {
                SeriesStartDate = new DateTime(2026, 1, 1),
                DaysOfMonth = new List<int> { 31 },
                ShortMonthBehavior = ShortMonthBehavior.Skip
            };

            var occurrences = _service.GetOccurrences(rule, new List<RecurrenceException>(), new DateTime(2026, 1, 1), new DateTime(2026, 4, 30));

            Assert.Equal(new[]
            {
                new DateTime(2026, 1, 31),
                new DateTime(2026, 3, 31)
            }, occurrences);
        }

        [Fact]
        public void GetOccurrences_MonthlyRelativeFirstMonday_ReturnsFirstMondayEachMonth()
        {
            var rule = new MonthlyRelativeRecurrenceRule
            {
                SeriesStartDate = new DateTime(2026, 1, 1),
                Ordinal = RelativeWeekOrdinal.First,
                DayKind = RelativeDayKind.SpecificDayOfWeek,
                DayOfWeek = DayOfWeek.Monday
            };

            var occurrences = _service.GetOccurrences(rule, new List<RecurrenceException>(), new DateTime(2026, 1, 1), new DateTime(2026, 3, 31));

            Assert.Equal(new[]
            {
                new DateTime(2026, 1, 5),
                new DateTime(2026, 2, 2),
                new DateTime(2026, 3, 2)
            }, occurrences);
        }

        [Fact]
        public void GetOccurrences_MonthlyRelativeLastWeekday_ReturnsLastBusinessDayEachMonth()
        {
            var rule = new MonthlyRelativeRecurrenceRule
            {
                SeriesStartDate = new DateTime(2026, 1, 1),
                Ordinal = RelativeWeekOrdinal.Last,
                DayKind = RelativeDayKind.Weekday
            };

            var occurrences = _service.GetOccurrences(rule, new List<RecurrenceException>(), new DateTime(2026, 1, 1), new DateTime(2026, 1, 31));

            // January 31, 2026 is a Saturday, so the last weekday is Friday the 30th.
            Assert.Equal(new[] { new DateTime(2026, 1, 30) }, occurrences);
        }

        [Fact]
        public void GetOccurrences_Yearly_ReturnsSameMonthDayEachYear()
        {
            var rule = new YearlyRecurrenceRule
            {
                SeriesStartDate = new DateTime(2026, 3, 15),
                Month = 3,
                Day = 15
            };

            var occurrences = _service.GetOccurrences(rule, new List<RecurrenceException>(), new DateTime(2026, 1, 1), new DateTime(2028, 12, 31));

            Assert.Equal(new[]
            {
                new DateTime(2026, 3, 15),
                new DateTime(2027, 3, 15),
                new DateTime(2028, 3, 15)
            }, occurrences);
        }

        [Fact]
        public void GetOccurrences_ExplicitDates_ReturnsOnlyListedDatesWithinRange()
        {
            var rule = new ExplicitDatesRecurrenceRule
            {
                SeriesStartDate = new DateTime(2026, 1, 1),
                Dates = new List<DateTime> { new(2026, 1, 5), new(2026, 2, 10), new(2026, 3, 1) }
            };

            var occurrences = _service.GetOccurrences(rule, new List<RecurrenceException>(), new DateTime(2026, 1, 1), new DateTime(2026, 2, 28));

            Assert.Equal(new[] { new DateTime(2026, 1, 5), new DateTime(2026, 2, 10) }, occurrences);
        }

        [Fact]
        public void GetOccurrences_NeverGeneratesBeforeSeriesStartDate_EvenWhenRangeStartsEarlier()
        {
            var rule = new DailyIntervalRecurrenceRule
            {
                SeriesStartDate = new DateTime(2026, 1, 10),
                IntervalDays = 1
            };

            var occurrences = _service.GetOccurrences(rule, new List<RecurrenceException>(), new DateTime(2026, 1, 1), new DateTime(2026, 1, 12));

            Assert.Equal(new[]
            {
                new DateTime(2026, 1, 10),
                new DateTime(2026, 1, 11),
                new DateTime(2026, 1, 12)
            }, occurrences);
        }

        [Fact]
        public void GetOccurrences_UntilDateEndCondition_StopsAfterEndDate()
        {
            var rule = new DailyIntervalRecurrenceRule
            {
                SeriesStartDate = new DateTime(2026, 1, 1),
                IntervalDays = 1,
                EndCondition = new UntilDateEndCondition { UntilDate = new DateTime(2026, 1, 3) }
            };

            var occurrences = _service.GetOccurrences(rule, new List<RecurrenceException>(), new DateTime(2026, 1, 1), new DateTime(2026, 1, 31));

            Assert.Equal(new[]
            {
                new DateTime(2026, 1, 1),
                new DateTime(2026, 1, 2),
                new DateTime(2026, 1, 3)
            }, occurrences);
        }

        [Fact]
        public void GetOccurrences_AfterOccurrencesEndCondition_CountsFromSeriesStartEvenWhenQueriedLater()
        {
            var rule = new DailyIntervalRecurrenceRule
            {
                SeriesStartDate = new DateTime(2026, 1, 1),
                IntervalDays = 1,
                EndCondition = new AfterOccurrencesEndCondition { OccurrenceCount = 5 }
            };

            var occurrences = _service.GetOccurrences(rule, new List<RecurrenceException>(), new DateTime(2026, 1, 3), new DateTime(2026, 1, 31));

            Assert.Equal(new[]
            {
                new DateTime(2026, 1, 3),
                new DateTime(2026, 1, 4),
                new DateTime(2026, 1, 5)
            }, occurrences);
        }

        [Fact]
        public void GetOccurrences_CancelledException_ExcludesThatOccurrenceOnly()
        {
            var rule = new DailyIntervalRecurrenceRule
            {
                SeriesStartDate = new DateTime(2026, 1, 1),
                IntervalDays = 1
            };
            var exceptions = new List<RecurrenceException>
            {
                new() { OriginalOccurrenceDate = new DateTime(2026, 1, 2), IsCancelled = true }
            };

            var occurrences = _service.GetOccurrences(rule, exceptions, new DateTime(2026, 1, 1), new DateTime(2026, 1, 3));

            Assert.Equal(new[] { new DateTime(2026, 1, 1), new DateTime(2026, 1, 3) }, occurrences);
        }

        [Fact]
        public void RecurrenceRule_SerializesAndDeserializesPolymorphically()
        {
            RecurrenceRule rule = new WeeklyRecurrenceRule
            {
                SeriesStartDate = new DateTime(2026, 1, 5),
                DaysOfWeek = new List<DayOfWeek> { DayOfWeek.Tuesday },
                IntervalWeeks = 2,
                EndCondition = new AfterOccurrencesEndCondition { OccurrenceCount = 10 }
            };

            var json = System.Text.Json.JsonSerializer.Serialize(rule);
            var deserialized = System.Text.Json.JsonSerializer.Deserialize<RecurrenceRule>(json);

            var weekly = Assert.IsType<WeeklyRecurrenceRule>(deserialized);
            Assert.Equal(new DateTime(2026, 1, 5), weekly.SeriesStartDate);
            Assert.Equal(2, weekly.IntervalWeeks);
            Assert.Equal(DayOfWeek.Tuesday, Assert.Single(weekly.DaysOfWeek));
            var endCondition = Assert.IsType<AfterOccurrencesEndCondition>(weekly.EndCondition);
            Assert.Equal(10, endCondition.OccurrenceCount);
        }
    }
}

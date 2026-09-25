using PriorityTaskManager.Models;
using PriorityTaskManager.Services.Helpers;

namespace PriorityTaskManager.Tests.Services
{
    public class RecurrenceSplitHelperTests
    {
        [Fact]
        public void Split_ClosesPriorSeriesTheDayBeforeSplitAndStartsNewSeriesOnSplitDate()
        {
            var rule = new DailyIntervalRecurrenceRule
            {
                SeriesStartDate = new DateTime(2026, 1, 1),
                IntervalDays = 1,
                EndCondition = new NeverEndCondition()
            };
            var splitDate = new DateTime(2026, 1, 15);

            var result = RecurrenceSplitHelper.Split(rule, new List<RecurrenceException>(), splitDate);

            var priorEnd = Assert.IsType<UntilDateEndCondition>(result.PriorRule.EndCondition);
            Assert.Equal(new DateTime(2026, 1, 14), priorEnd.UntilDate);
            Assert.Equal(new DateTime(2026, 1, 1), result.PriorRule.SeriesStartDate);
            Assert.Equal(splitDate, result.NewRule.SeriesStartDate);
            Assert.IsType<NeverEndCondition>(result.NewRule.EndCondition);
        }

        [Fact]
        public void Split_PartitionsExceptionsByDateRelativeToSplit()
        {
            var rule = new DailyIntervalRecurrenceRule { SeriesStartDate = new DateTime(2026, 1, 1) };
            var exceptions = new List<RecurrenceException>
            {
                new() { OriginalOccurrenceDate = new DateTime(2026, 1, 5), IsCancelled = true },
                new() { OriginalOccurrenceDate = new DateTime(2026, 1, 20), IsCancelled = true }
            };

            var result = RecurrenceSplitHelper.Split(rule, exceptions, new DateTime(2026, 1, 15));

            Assert.Equal(new DateTime(2026, 1, 5), Assert.Single(result.PriorExceptions).OriginalOccurrenceDate);
            Assert.Equal(new DateTime(2026, 1, 20), Assert.Single(result.NewExceptions).OriginalOccurrenceDate);
        }

        [Fact]
        public void Split_DoesNotMutateOriginalRuleOrExceptions()
        {
            var rule = new DailyIntervalRecurrenceRule { SeriesStartDate = new DateTime(2026, 1, 1), EndCondition = new NeverEndCondition() };
            var exceptions = new List<RecurrenceException> { new() { OriginalOccurrenceDate = new DateTime(2026, 1, 5) } };

            RecurrenceSplitHelper.Split(rule, exceptions, new DateTime(2026, 1, 15));

            Assert.IsType<NeverEndCondition>(rule.EndCondition);
            Assert.Equal(new DateTime(2026, 1, 1), rule.SeriesStartDate);
        }
    }
}

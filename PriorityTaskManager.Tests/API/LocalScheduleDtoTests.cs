using PriorityTaskManager.API.Local;
using PriorityTaskManager.Models;

namespace PriorityTaskManager.Tests.API;

public class LocalScheduleDtoTests
{
    [Fact]
    public void RecurringScheduleRequestMapsItsRuleAndOccurrenceState()
    {
        var rule = new DailyIntervalRecurrenceRule
        {
            SeriesStartDate = new DateTime(2026, 10, 8),
            IntervalDays = 1,
            EndCondition = new NeverEndCondition()
        };
        var state = new TaskOccurrenceState
        {
            ScheduledDate = new DateTime(2026, 10, 8),
            Status = TaskOccurrenceStatus.Missed,
            CompletionCount = 1
        };
        var request = new LocalTaskRequest(
            Guid.NewGuid(),
            "Review",
            false,
            0,
            5,
            1,
            0,
            null,
            null,
            TimeSpan.FromMinutes(90),
            null,
            false,
            null,
            null,
            false,
            rule,
            Guid.NewGuid(),
            TaskProgressionMode.RollForwardKeepBacklog,
            2,
            [state]);

        var task = request.ToTaskItem();

        Assert.NotSame(rule, task.RecurrenceRule);
        Assert.Equal(rule.SeriesStartDate, task.RecurrenceRule!.SeriesStartDate);
        Assert.Equal(TaskProgressionMode.RollForwardKeepBacklog, task.ProgressionMode);
        Assert.Equal(2, task.RequiredCompletions);
        Assert.Equal(TaskOccurrenceStatus.Missed, Assert.Single(task.OccurrenceStates).Status);
        Assert.Equal(1, task.OccurrenceStates[0].CompletionCount);
    }
}

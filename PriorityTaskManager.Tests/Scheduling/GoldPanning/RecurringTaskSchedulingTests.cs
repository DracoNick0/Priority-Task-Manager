using PriorityTaskManager.Models;
using PriorityTaskManager.Scheduling.GoldPanning;
using PriorityTaskManager.Scheduling.GoldPanning.Stages;
using PriorityTaskManager.Services;
using PriorityTaskManager.Tests.Infrastructure;

namespace PriorityTaskManager.Tests.Scheduling.GoldPanning;

public class RecurringTaskSchedulingTests
{
    [Fact]
    public void ExpandsOccurrencesThroughTheComputedHorizonAndAppliesOccurrenceProgress()
    {
        var now = new DateTime(2026, 10, 8, 8, 0, 0);
        var time = new MockTimeService();
        time.SetCurrentTime(now);
        var series = CreateSeries(new DateTime(2026, 10, 8));
        series.RequiredCompletions = 2;
        series.OccurrenceStates.Add(new TaskOccurrenceState
        {
            ScheduledDate = now.Date,
            CompletionCount = 1,
            Status = TaskOccurrenceStatus.Pending
        });
        var strategy = new GoldPanningStrategy(
            new UserProfile(),
            new List<Event>(),
            time,
            new RecurrenceExpansionService());

        var result = strategy.CalculateUrgency(new List<TaskItem> { series });

        var current = Assert.Single(result.Tasks, task => task.OccurrenceDate == now.Date);
        Assert.Equal(TaskOccurrence.CreateId(series.Id, now.Date), current.Id);
        Assert.Equal(TimeSpan.FromMinutes(30), current.EstimatedDuration);
        Assert.Equal(now.Date, current.DueDate!.Value.Date);
        Assert.Equal(now.Date, current.NotBefore!.Value.Date);
        Assert.NotEmpty(current.ScheduledParts);
        Assert.Contains(result.Tasks, task => task.OccurrenceDate > now.Date);
        Assert.DoesNotContain(result.Tasks, task => task.Id == series.Id);
    }

    [Fact]
    public void KeepsMissedBacklogButPreservesTheDueDateSchedulingInvariant()
    {
        var now = new DateTime(2026, 10, 9, 8, 0, 0);
        var time = new MockTimeService();
        time.SetCurrentTime(now);
        var series = CreateSeries(new DateTime(2026, 10, 7));
        series.OccurrenceStates.Add(new TaskOccurrenceState
        {
            ScheduledDate = new DateTime(2026, 10, 7),
            CompletionCount = 0,
            Status = TaskOccurrenceStatus.Missed
        });
        series.OccurrenceStates.Add(new TaskOccurrenceState
        {
            ScheduledDate = new DateTime(2026, 10, 8),
            CompletionCount = 1,
            Status = TaskOccurrenceStatus.Completed
        });
        var strategy = new GoldPanningStrategy(
            new UserProfile(),
            new List<Event>(),
            time,
            new RecurrenceExpansionService());

        var result = strategy.CalculateUrgency(new List<TaskItem> { series });

        var missed = Assert.Single(result.Tasks, task => task.OccurrenceDate == new DateTime(2026, 10, 7));
        Assert.Empty(missed.ScheduledParts);
        Assert.Contains(result.UnscheduledTasks, task => task.Id == missed.Id);
        Assert.DoesNotContain(result.Tasks, task => task.OccurrenceDate == new DateTime(2026, 10, 8));
        Assert.Contains(result.Tasks, task => task.OccurrenceDate == now.Date);
    }

    [Fact]
    public void KeepsPastPendingOccurrencesInBacklogWhenTheScheduleSnapshotIsStale()
    {
        var now = new DateTime(2026, 10, 15, 11, 0, 0);
        var time = new MockTimeService();
        time.SetCurrentTime(now);
        var startDate = new DateTime(2026, 10, 13);
        var series = CreateSeries(startDate);
        series.DueDate = startDate.AddHours(17);
        series.RecurrenceRule = new WeeklyRecurrenceRule
        {
            SeriesStartDate = startDate,
            DaysOfWeek =
            [
                DayOfWeek.Monday,
                DayOfWeek.Tuesday,
                DayOfWeek.Wednesday,
                DayOfWeek.Thursday,
                DayOfWeek.Friday
            ],
            EndCondition = new AfterOccurrencesEndCondition { OccurrenceCount = 7 }
        };
        series.OccurrenceStates.AddRange(
        [
            new TaskOccurrenceState
            {
                ScheduledDate = new DateTime(2026, 10, 13),
                Status = TaskOccurrenceStatus.Pending
            },
            new TaskOccurrenceState
            {
                ScheduledDate = new DateTime(2026, 10, 14),
                Status = TaskOccurrenceStatus.Pending
            }
        ]);
        var strategy = new GoldPanningStrategy(
            new UserProfile(),
            new List<Event>(),
            time,
            new RecurrenceExpansionService());

        var result = strategy.CalculateUrgency(new List<TaskItem> { series });

        foreach (var missedDate in new[] { new DateTime(2026, 10, 13), new DateTime(2026, 10, 14) })
        {
            var occurrence = Assert.Single(result.Tasks, task => task.OccurrenceDate == missedDate);
            Assert.Equal(TaskOccurrenceStatus.Missed, occurrence.OccurrenceStatus);
            Assert.Empty(occurrence.ScheduledParts);
            Assert.Contains(result.UnscheduledTasks, task => task.Id == occurrence.Id);
        }
    }

    [Fact]
    public void SequentialCatchUpExpandsOnlyTheOldestUnresolvedOccurrence()
    {
        var now = new DateTime(2026, 10, 9, 8, 0, 0);
        var time = new MockTimeService();
        time.SetCurrentTime(now);
        var series = CreateSeries(new DateTime(2026, 10, 7));
        series.ProgressionMode = TaskProgressionMode.SequentialCatchUp;
        series.OccurrenceStates.Add(new TaskOccurrenceState
        {
            ScheduledDate = new DateTime(2026, 10, 7),
            CompletionCount = 0,
            Status = TaskOccurrenceStatus.Skipped
        });
        var strategy = new GoldPanningStrategy(
            new UserProfile(),
            new List<Event>(),
            time,
            new RecurrenceExpansionService());

        var result = strategy.CalculateUrgency(new List<TaskItem> { series });

        Assert.Single(result.Tasks, task => task.OccurrenceDate == new DateTime(2026, 10, 8));
        Assert.DoesNotContain(result.Tasks, task => task.OccurrenceDate == now.Date);
    }

    [Fact]
    public void ExpandsOneOccurrencePastTheHorizonWhenItsPredecessorIsInside()
    {
        var now = new DateTime(2026, 10, 1, 8, 0, 0);
        var horizonEnd = now.Date.AddDays(14);
        var time = new MockTimeService();
        time.SetCurrentTime(now);
        var series = CreateSeries(now.Date.AddDays(10));
        ((DailyIntervalRecurrenceRule)series.RecurrenceRule!).IntervalDays = 10;
        var context = new SchedulingContext();
        context.SharedState["Tasks"] = new List<TaskItem> { series };
        context.SharedState["AvailableScheduleWindow"] = new ScheduleWindow
        {
            HorizonEndDate = horizonEnd
        };

        new TaskOccurrenceExpansionStage(time, new RecurrenceExpansionService()).Act(context);

        var occurrences = Assert.IsType<List<TaskItem>>(context.SharedState["Tasks"])
            .OrderBy(task => task.OccurrenceDate)
            .ToList();
        Assert.Equal(2, occurrences.Count);
        Assert.Equal(now.Date.AddDays(10), occurrences[0].DueDate!.Value.Date);
        Assert.Equal(now.Date.AddDays(10), occurrences[0].NotBefore!.Value.Date);
        Assert.Equal(now.Date.AddDays(20), occurrences[1].DueDate!.Value.Date);
        Assert.Equal(now.Date.AddDays(20), occurrences[1].NotBefore!.Value.Date);
        Assert.DoesNotContain(occurrences, task => task.OccurrenceDate == now.Date.AddDays(30));
    }

    [Fact]
    public void CountLimitedWeeklySeriesExtendsHorizonAndKeepsLookaheadOnItsDueDate()
    {
        var now = new DateTime(2026, 10, 13, 8, 0, 0);
        var time = new MockTimeService();
        time.SetCurrentTime(now);
        var series = CreateSeries(now.Date);
        series.DueDate = now.Date.AddHours(16).AddMinutes(30);
        series.RecurrenceRule = new WeeklyRecurrenceRule
        {
            SeriesStartDate = now.Date,
            DaysOfWeek = new List<DayOfWeek>
            {
                DayOfWeek.Monday,
                DayOfWeek.Tuesday,
                DayOfWeek.Wednesday,
                DayOfWeek.Thursday
            },
            EndCondition = new AfterOccurrencesEndCondition { OccurrenceCount = 7 }
        };
        var context = new SchedulingContext();
        context.SharedState["Tasks"] = new List<TaskItem> { series };
        context.SharedState["UserProfile"] = new UserProfile();
        context.SharedState["Events"] = new List<Event>();
        context.SharedState["TotalAvailableTime"] = TimeSpan.FromHours(1);

        new AvailabilityWindowStage(time, new RecurrenceExpansionService()).Act(context);
        var horizon = Assert.IsType<ScheduleWindow>(
            context.SharedState["AvailableScheduleWindow"]);
        new TaskOccurrenceExpansionStage(time, new RecurrenceExpansionService()).Act(context);

        var occurrences = Assert.IsType<List<TaskItem>>(context.SharedState["Tasks"])
            .OrderBy(task => task.OccurrenceDate)
            .ToList();
        Assert.Equal(7, occurrences.Count);
        Assert.Equal(new DateTime(2026, 10, 22), horizon.HorizonEndDate);
        Assert.All(occurrences, occurrence =>
        {
            Assert.Equal(
                occurrence.OccurrenceDate!.Value.Date.AddHours(16).AddMinutes(30),
                occurrence.DueDate!.Value);
            Assert.Equal(occurrence.DueDate!.Value.Date, occurrence.NotBefore!.Value.Date);
        });
        Assert.Equal(new DateTime(2026, 10, 20, 16, 30, 0), occurrences[4].DueDate!.Value);

        var result = new GoldPanningStrategy(
            new UserProfile(),
            new List<Event>(),
            time,
            new RecurrenceExpansionService())
            .CalculateUrgency(new List<TaskItem> { series });
        var scheduledOccurrences = result.Tasks
            .Where(task => task.OccurrenceDate.HasValue)
            .OrderBy(task => task.OccurrenceDate)
            .ToList();
        Assert.Equal(7, scheduledOccurrences.Count);
        Assert.All(scheduledOccurrences, occurrence =>
        {
            Assert.NotEmpty(occurrence.ScheduledParts);
            Assert.All(
                occurrence.ScheduledParts,
                part => Assert.Equal(
                    occurrence.DueDate!.Value.Date,
                    part.StartTime.Date));
        });
    }

    [Fact]
    public void ZeroDurationRecurringSeriesDoesNotCreateScheduledTimeBlocks()
    {
        var now = new DateTime(2026, 10, 8, 8, 0, 0);
        var time = new MockTimeService();
        time.SetCurrentTime(now);
        var series = CreateSeries(now.Date);
        series.EstimatedDuration = TimeSpan.Zero;
        var strategy = new GoldPanningStrategy(
            new UserProfile(),
            new List<Event>(),
            time,
            new RecurrenceExpansionService());

        var result = strategy.CalculateUrgency(new List<TaskItem> { series });

        Assert.Empty(result.Tasks);
        Assert.Empty(result.UnscheduledTasks);
    }

    private static TaskItem CreateSeries(DateTime startDate)
    {
        var id = Guid.NewGuid();
        return new TaskItem
        {
            Id = id,
            Title = "Recurring review",
            EstimatedDuration = TimeSpan.FromHours(1),
            RecurrenceRule = new DailyIntervalRecurrenceRule
            {
                SeriesStartDate = startDate,
                IntervalDays = 1,
                EndCondition = new NeverEndCondition()
            },
            SeriesId = id,
            OccurrenceStates = new List<TaskOccurrenceState>()
        };
    }
}

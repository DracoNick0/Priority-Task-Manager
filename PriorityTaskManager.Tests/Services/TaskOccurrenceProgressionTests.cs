using PriorityTaskManager.Models;
using PriorityTaskManager.Scheduling.GoldPanning;
using PriorityTaskManager.Services;
using PriorityTaskManager.Tests.Infrastructure;
using Xunit;

namespace PriorityTaskManager.Tests.Services;

public class TaskOccurrenceProgressionTests
{
    [Fact]
    public void RollForwardKeepsEachMissedOccurrenceAndItsProgress()
    {
        var (service, time, listId) = CreateService(new DateTime(2026, 10, 8));
        var task = AddRecurringTask(service, listId, new DateTime(2026, 10, 6), requiredCompletions: 2);

        var occurrences = service.GetTaskOccurrences(listId, new DateTime(2026, 10, 8), new DateTime(2026, 10, 9), time);
        Assert.Equal(2, occurrences.Count(o => o.Status == TaskOccurrenceStatus.Missed));
        Assert.False(occurrences.Single(o => o.ScheduledDate.Date == new DateTime(2026, 10, 8)).IsMissed);

        Assert.True(service.CompleteTaskOccurrence(task.Id, new DateTime(2026, 10, 7), time));

        var missed = service.GetTaskOccurrences(listId, new DateTime(2026, 10, 8), new DateTime(2026, 10, 9), time);
        var partiallyComplete = Assert.Single(missed, o => o.ScheduledDate.Date == new DateTime(2026, 10, 7));
        Assert.Equal(TaskOccurrenceStatus.Missed, partiallyComplete.Status);
        Assert.Equal(1, partiallyComplete.CompletionCount);

        service.CompleteTaskOccurrence(task.Id, new DateTime(2026, 10, 7), time);
        var completed = task.OccurrenceStates.Single(o => o.ScheduledDate.Date == new DateTime(2026, 10, 7));
        Assert.Equal(TaskOccurrenceStatus.Completed, completed.Status);
        Assert.Equal(2, completed.CompletionCount);
    }

    [Fact]
    public void UndoChangesOnlyTheSelectedOccurrenceAndRecalculatesStreak()
    {
        var (service, time, listId) = CreateService(new DateTime(2026, 10, 8));
        var task = AddRecurringTask(
            service,
            listId,
            new DateTime(2026, 10, 6),
            requiredCompletions: 1,
            trackStreak: true);

        service.CompleteTaskOccurrence(task.Id, new DateTime(2026, 10, 6), time);
        service.CompleteTaskOccurrence(task.Id, new DateTime(2026, 10, 7), time);
        Assert.Equal(2, task.CurrentStreak);
        Assert.True(service.UndoTaskOccurrenceCompletion(task.Id, new DateTime(2026, 10, 6), time));

        var persisted = service.GetTaskById(task.Id)!;
        Assert.Equal(0, persisted.OccurrenceStates.Single(o => o.ScheduledDate.Date == new DateTime(2026, 10, 6)).CompletionCount);
        Assert.Equal(TaskOccurrenceStatus.Completed, persisted.OccurrenceStates.Single(o => o.ScheduledDate.Date == new DateTime(2026, 10, 7)).Status);
        Assert.Equal(0, persisted.CurrentStreak);
        Assert.Equal(2, persisted.BestStreak);
    }

    [Fact]
    public void SequentialCatchUpExposesOnlyOldestUnresolvedDate()
    {
        var (service, time, listId) = CreateService(new DateTime(2026, 10, 8));
        var task = AddRecurringTask(
            service,
            listId,
            new DateTime(2026, 10, 6),
            progressionMode: TaskProgressionMode.SequentialCatchUp);

        var first = Assert.Single(service.GetTaskOccurrences(
            listId, new DateTime(2026, 10, 8), new DateTime(2026, 10, 12), time));
        Assert.Equal(new DateTime(2026, 10, 6), first.ScheduledDate.Date);

        service.SkipTaskOccurrence(task.Id, first.ScheduledDate, time);
        var second = Assert.Single(service.GetTaskOccurrences(
            listId, new DateTime(2026, 10, 8), new DateTime(2026, 10, 12), time));
        Assert.Equal(new DateTime(2026, 10, 7), second.ScheduledDate.Date);
    }

    [Fact]
    public void UndoOccurrenceCompletionUnskipsManuallySkippedOccurrence()
    {
        var (service, time, listId) = CreateService(new DateTime(2026, 10, 8));
        var task = AddRecurringTask(service, listId, new DateTime(2026, 10, 8));
        var occurrenceDate = new DateTime(2026, 10, 8);

        Assert.True(service.SkipTaskOccurrence(task.Id, occurrenceDate, time));
        Assert.Equal(
            TaskOccurrenceStatus.Skipped,
            task.OccurrenceStates.Single().Status);

        Assert.True(service.UndoTaskOccurrenceCompletion(task.Id, occurrenceDate, time));

        var state = service.GetTaskById(task.Id)!.OccurrenceStates.Single();
        Assert.Equal(TaskOccurrenceStatus.Pending, state.Status);
        Assert.Equal(0, state.CompletionCount);
        Assert.Null(state.CompletedAt);
        Assert.Contains(
            service.GetTaskOccurrences(listId, occurrenceDate, occurrenceDate, time),
            occurrence => occurrence.Status == TaskOccurrenceStatus.Pending);
    }

    [Fact]
    public void DisregardedDatesBreakStreakAndIndicatorClearsAfterNextCompletion()
    {
        var (service, time, listId) = CreateService(new DateTime(2026, 10, 8));
        var task = AddRecurringTask(
            service,
            listId,
            new DateTime(2026, 10, 6),
            progressionMode: TaskProgressionMode.RollForwardDisregard,
            trackStreak: true);

        service.GetTaskOccurrences(listId, new DateTime(2026, 10, 8), new DateTime(2026, 10, 9), time);
        Assert.True(task.HasUnresolvedMissedIndicator);
        Assert.Equal(2, task.OccurrenceStates.Count(o => o.Status == TaskOccurrenceStatus.Disregarded));

        service.CompleteTaskOccurrence(task.Id, new DateTime(2026, 10, 8), time);

        Assert.False(task.HasUnresolvedMissedIndicator);
        Assert.Equal(1, task.CurrentStreak);
    }

    [Fact]
    public void HidingMissedIndicatorDoesNotChangeOccurrenceProgression()
    {
        var (service, time, listId) = CreateService(new DateTime(2026, 10, 8));
        var task = AddRecurringTask(service, listId, new DateTime(2026, 10, 6));
        task.ShowMissedIndicator = false;

        service.GetTaskOccurrences(listId, new DateTime(2026, 10, 8), new DateTime(2026, 10, 9), time);

        Assert.Equal(2, task.OccurrenceStates.Count(o => o.Status == TaskOccurrenceStatus.Missed));
        Assert.True(task.HasUnresolvedMissedIndicator);
        Assert.False(task.ShowMissedIndicator);
    }

    [Fact]
    public void SchedulingFlowReconcilesMissedDatesWithoutSchedulingTheSeriesTemplate()
    {
        var (service, time, listId) = CreateService(new DateTime(2026, 10, 8));
        var task = AddRecurringTask(service, listId, new DateTime(2026, 10, 6));

        var result = service.GetPrioritizedTasks(listId, time);

        Assert.DoesNotContain(result.Tasks, scheduled => scheduled.Id == task.Id);
        Assert.Equal(2, task.OccurrenceStates.Count(o => o.Status == TaskOccurrenceStatus.Missed));
    }

    [Fact]
    public void EarlyCompletionRemainsAttachedToTheScheduledDate()
    {
        var (service, time, listId) = CreateService(new DateTime(2026, 10, 8));
        var task = AddRecurringTask(service, listId, new DateTime(2026, 10, 8));

        Assert.True(service.CompleteTaskOccurrence(task.Id, new DateTime(2026, 10, 9), time));

        var occurrences = service.GetTaskOccurrences(
            listId, new DateTime(2026, 10, 8), new DateTime(2026, 10, 10), time);
        Assert.Equal(
            new[] { new DateTime(2026, 10, 8), new DateTime(2026, 10, 9), new DateTime(2026, 10, 10) },
            occurrences.Select(o => o.ScheduledDate.Date).ToArray());
        Assert.Equal(
            TaskOccurrenceStatus.Completed,
            occurrences.Single(o => o.ScheduledDate.Date == new DateTime(2026, 10, 9)).Status);
    }

    [Fact]
    public void FiniteSeriesCompletesOnlyAfterAllGeneratedOccurrencesAreResolved()
    {
        var (service, time, listId) = CreateService(new DateTime(2026, 10, 8));
        var task = AddRecurringTask(
            service,
            listId,
            new DateTime(2026, 10, 6),
            endCondition: new UntilDateEndCondition { UntilDate = new DateTime(2026, 10, 7) });

        service.GetTaskOccurrences(listId, new DateTime(2026, 10, 6), new DateTime(2026, 10, 8), time);
        Assert.False(task.IsCompleted);
        service.CompleteTaskOccurrence(task.Id, new DateTime(2026, 10, 6), time);
        Assert.False(task.IsCompleted);
        service.CompleteTaskOccurrence(task.Id, new DateTime(2026, 10, 7), time);

        Assert.True(task.IsCompleted);
    }

    [Fact]
    public void ThisOccurrenceEditStoresAnOverrideByTheOriginalRecurrenceDate()
    {
        var (service, _, listId) = CreateService(new DateTime(2026, 10, 8));
        var task = AddRecurringTask(service, listId, new DateTime(2026, 10, 6));
        var edited = task.Clone();
        edited.Title = "Moved occurrence";
        edited.DueDate = new DateTime(2026, 10, 12);
        edited.Description = "Occurrence-only description";

        Assert.True(service.EditTaskOccurrence(
            task.Id,
            new DateTime(2026, 10, 7),
            edited,
            RecurrenceEditTarget.ThisOccurrence));

        var occurrenceOverride = Assert.Single(task.OccurrenceOverrides);
        Assert.Equal(new DateTime(2026, 10, 7), occurrenceOverride.OriginalOccurrenceDate);
        Assert.Equal(new DateTime(2026, 10, 12), occurrenceOverride.DueDate);
        Assert.Equal("Moved occurrence", occurrenceOverride.Title);
        Assert.Equal("Recurring task", task.Title);
        Assert.Empty(task.RecurrenceExceptions);
    }

    [Fact]
    public void ThisAndFollowingEditSplitsExceptionsOverridesAndOccurrenceProgress()
    {
        var (service, time, listId) = CreateService(new DateTime(2026, 10, 9));
        var task = AddRecurringTask(service, listId, new DateTime(2026, 10, 6));
        service.GetTaskOccurrences(listId, new DateTime(2026, 10, 6), new DateTime(2026, 10, 9), time);
        task.OccurrenceOverrides.Add(new TaskOccurrenceOverride
        {
            OriginalOccurrenceDate = new DateTime(2026, 10, 7),
            Title = "Prior override"
        });
        task.OccurrenceOverrides.Add(new TaskOccurrenceOverride
        {
            OriginalOccurrenceDate = new DateTime(2026, 10, 9),
            Title = "Following override"
        });
        task.RecurrenceExceptions.Add(new RecurrenceException
        {
            OriginalOccurrenceDate = new DateTime(2026, 10, 9),
            IsCancelled = true
        });
        var selectedDate = new DateTime(2026, 10, 8);
        var edited = task.Clone();
        edited.Title = "New series title";
        edited.DueDate = selectedDate;

        Assert.True(service.EditTaskOccurrence(
            task.Id,
            selectedDate,
            edited,
            RecurrenceEditTarget.ThisAndFollowing));

        var prior = service.GetTaskById(task.Id)!;
        var following = service.GetAllTasks(listId).Single(candidate => candidate.Id != task.Id);
        Assert.Equal(new DateTime(2026, 10, 7), ((UntilDateEndCondition)prior.RecurrenceRule!.EndCondition).UntilDate);
        Assert.Equal(selectedDate, following.RecurrenceRule!.SeriesStartDate);
        Assert.Equal(new[] { new DateTime(2026, 10, 6), new DateTime(2026, 10, 7) },
            prior.OccurrenceStates.Select(state => state.ScheduledDate.Date).Order().ToArray());
        Assert.Equal(new[] { selectedDate, new DateTime(2026, 10, 9) },
            following.OccurrenceStates.Select(state => state.ScheduledDate.Date).Order().ToArray());
        Assert.Equal(new DateTime(2026, 10, 7), Assert.Single(prior.OccurrenceOverrides).OriginalOccurrenceDate);
        Assert.Equal(new DateTime(2026, 10, 9), Assert.Single(following.OccurrenceOverrides).OriginalOccurrenceDate);
        Assert.Equal(new DateTime(2026, 10, 9), Assert.Single(following.RecurrenceExceptions).OriginalOccurrenceDate);
    }

    [Fact]
    public void AllOccurrencesEditPrunesRecordsOutsideReplacementPattern()
    {
        var (service, _, listId) = CreateService(new DateTime(2026, 10, 8));
        var series = AddRecurringTask(service, listId, new DateTime(2026, 10, 5));
        var monday = new DateTime(2026, 10, 5);
        var tuesday = new DateTime(2026, 10, 6);
        var wednesday = new DateTime(2026, 10, 7);
        var thursday = new DateTime(2026, 10, 8);
        series.OccurrenceStates.AddRange(
        [
            new TaskOccurrenceState { ScheduledDate = monday, Status = TaskOccurrenceStatus.Completed },
            new TaskOccurrenceState { ScheduledDate = tuesday, Status = TaskOccurrenceStatus.Missed },
            new TaskOccurrenceState { ScheduledDate = wednesday, Status = TaskOccurrenceStatus.Pending }
        ]);
        series.RecurrenceExceptions.AddRange(
        [
            new RecurrenceException { OriginalOccurrenceDate = tuesday, IsCancelled = true },
            new RecurrenceException { OriginalOccurrenceDate = wednesday, IsCancelled = true },
            new RecurrenceException { OriginalOccurrenceDate = thursday, IsCancelled = true }
        ]);
        series.OccurrenceOverrides.AddRange(
        [
            new TaskOccurrenceOverride { OriginalOccurrenceDate = monday, Title = "Monday override" },
            new TaskOccurrenceOverride { OriginalOccurrenceDate = tuesday, Title = "Tuesday override" },
            new TaskOccurrenceOverride { OriginalOccurrenceDate = wednesday, Title = "Wednesday override" }
        ]);
        var edited = series.Clone();
        edited.RecurrenceRule = new WeeklyRecurrenceRule
        {
            SeriesStartDate = monday,
            DaysOfWeek = [DayOfWeek.Monday, DayOfWeek.Wednesday],
            EndCondition = new NeverEndCondition()
        };

        Assert.True(service.EditTaskOccurrence(
            series.Id,
            monday,
            edited,
            RecurrenceEditTarget.AllOccurrences));

        Assert.Equal(
            new[] { monday, wednesday },
            series.OccurrenceStates.Select(state => state.ScheduledDate.Date).Order().ToArray());
        Assert.Equal(
            new[] { wednesday },
            series.RecurrenceExceptions.Select(exception => exception.OriginalOccurrenceDate.Date).ToArray());
        Assert.Equal(
            new[] { monday, wednesday },
            series.OccurrenceOverrides.Select(occurrenceOverride =>
                occurrenceOverride.OriginalOccurrenceDate.Date).Order().ToArray());
    }

    [Fact]
    public void DeletingThisOccurrenceCancelsOnlyItsOriginalDate()
    {
        var (service, time, listId) = CreateService(new DateTime(2026, 10, 8));
        var task = AddRecurringTask(service, listId, new DateTime(2026, 10, 6));

        Assert.True(service.DeleteTaskOccurrence(
            task.Id,
            new DateTime(2026, 10, 7),
            RecurrenceEditTarget.ThisOccurrence));

        Assert.Contains(task.RecurrenceExceptions, exception =>
            exception.OriginalOccurrenceDate.Date == new DateTime(2026, 10, 7) &&
            exception.IsCancelled);
        Assert.DoesNotContain(
            service.GetTaskOccurrences(listId, new DateTime(2026, 10, 6), new DateTime(2026, 10, 8), time),
            occurrence => occurrence.ScheduledDate.Date == new DateTime(2026, 10, 7));
        Assert.Contains(
            service.GetTaskOccurrences(listId, new DateTime(2026, 10, 6), new DateTime(2026, 10, 8), time),
            occurrence => occurrence.ScheduledDate.Date == new DateTime(2026, 10, 8));
    }

    private static (TaskManagerService Service, MockTimeService Time, Guid ListId) CreateService(DateTime now)
    {
        var persistence = new MockPersistenceService();
        var data = persistence.LoadData();
        var time = new MockTimeService();
        time.SetCurrentTime(now);
        var strategy = new GoldPanningStrategy(data.UserProfile, data.Events, time, new RecurrenceExpansionService());
        var service = new TaskManagerService(strategy, persistence, data);
        return (service, time, service.GetActiveListId());
    }

    private static TaskItem AddRecurringTask(
        TaskManagerService service,
        Guid listId,
        DateTime startDate,
        int requiredCompletions = 1,
        TaskProgressionMode progressionMode = TaskProgressionMode.RollForwardKeepBacklog,
        bool trackStreak = false,
        RecurrenceEndCondition? endCondition = null)
    {
        var task = new TaskItem
        {
            Title = "Recurring task",
            ListId = listId,
            RecurrenceRule = new DailyIntervalRecurrenceRule
            {
                SeriesStartDate = startDate,
                IntervalDays = 1,
                EndCondition = endCondition ?? new NeverEndCondition()
            },
            RequiredCompletions = requiredCompletions,
            ProgressionMode = progressionMode,
            TrackStreak = trackStreak
        };
        service.AddTask(task);
        return task;
    }
}

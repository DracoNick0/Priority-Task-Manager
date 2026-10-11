using PriorityTaskManager.API.Local;
using PriorityTaskManager.Models;
using Microsoft.AspNetCore.Http.HttpResults;

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
        var exception = new RecurrenceException
        {
            OriginalOccurrenceDate = new DateTime(2026, 10, 9),
            IsCancelled = true
        };
        var occurrenceOverride = new TaskOccurrenceOverride
        {
            OriginalOccurrenceDate = new DateTime(2026, 10, 8),
            Title = "Moved review",
            DueDate = new DateTime(2026, 10, 12),
            EstimatedDuration = TimeSpan.FromHours(2),
            Importance = 8,
            Complexity = 4
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
            [state],
            RecurrenceExceptions: [exception],
            OccurrenceOverrides: [occurrenceOverride]);

        var task = request.ToTaskItem();

        Assert.NotSame(rule, task.RecurrenceRule);
        Assert.Equal(rule.SeriesStartDate, task.RecurrenceRule!.SeriesStartDate);
        Assert.Equal(TaskProgressionMode.RollForwardKeepBacklog, task.ProgressionMode);
        Assert.Equal(2, task.RequiredCompletions);
        Assert.Equal(TaskOccurrenceStatus.Missed, Assert.Single(task.OccurrenceStates).Status);
        Assert.Equal(1, task.OccurrenceStates[0].CompletionCount);
        Assert.NotSame(exception, Assert.Single(task.RecurrenceExceptions));
        Assert.True(task.RecurrenceExceptions[0].IsCancelled);
        Assert.NotSame(occurrenceOverride, Assert.Single(task.OccurrenceOverrides));
        Assert.Equal("Moved review", task.OccurrenceOverrides[0].Title);
    }

    [Fact]
    public void ScheduleResponseIncludesDetailsForUnscheduledRecurringBacklog()
    {
        var seriesId = Guid.NewGuid();
        var firstMissedDate = new DateTime(2026, 10, 13);
        var secondMissedDate = new DateTime(2026, 10, 14);
        var request = new LocalScheduleRequest(
            [
                new LocalTaskRequest(
                    seriesId,
                    "Review",
                    false,
                    0,
                    5,
                    1,
                    0,
                    firstMissedDate.AddHours(17),
                    null,
                    TimeSpan.FromHours(1),
                    [],
                    false,
                    null,
                    null,
                    false,
                    new WeeklyRecurrenceRule
                    {
                        SeriesStartDate = firstMissedDate,
                        DaysOfWeek =
                        [
                            DayOfWeek.Monday,
                            DayOfWeek.Tuesday,
                            DayOfWeek.Wednesday,
                            DayOfWeek.Thursday,
                            DayOfWeek.Friday
                        ],
                        EndCondition = new AfterOccurrencesEndCondition { OccurrenceCount = 7 }
                    },
                    seriesId,
                    TaskProgressionMode.RollForwardKeepBacklog,
                    1,
                    [
                        new TaskOccurrenceState
                        {
                            ScheduledDate = firstMissedDate,
                            Status = TaskOccurrenceStatus.Pending
                        },
                        new TaskOccurrenceState
                        {
                            ScheduledDate = secondMissedDate,
                            Status = TaskOccurrenceStatus.Pending
                        }
                    ],
                    Guid.NewGuid(),
                    Description: "Review notes")
            ],
            [],
            new LocalUserProfileRequest(
                new TimeOnly(9),
                new TimeOnly(17),
                [DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday],
                SchedulingMode.GoldPanning,
                TimeSpan.FromMinutes(15),
                0.5,
                1,
                2,
                4),
            new DateTime(2026, 10, 15, 11, 0, 0));

        var result = ScheduleComputation.Compute(request);

        var response = Assert.IsType<Ok<LocalScheduleResponse>>(result).Value;
        Assert.NotNull(response);
        var missedOccurrences = response.UnscheduledTasks!
            .Where(task => task.OccurrenceDate == firstMissedDate ||
                task.OccurrenceDate == secondMissedDate)
            .ToList();
        Assert.Equal(2, missedOccurrences.Count);
        foreach (var missedOccurrence in missedOccurrences)
        {
            Assert.Equal(seriesId, missedOccurrence.SeriesId);
            Assert.Equal("Review", missedOccurrence.Title);
            Assert.IsType<WeeklyRecurrenceRule>(missedOccurrence.RecurrenceRule);
            Assert.Equal(TaskOccurrenceStatus.Missed, missedOccurrence.OccurrenceStatus);
            Assert.Contains(missedOccurrence.Id, response.UnscheduledTaskIds);
        }
    }

    [Fact]
    public void SequentialCatchUpProjectsAnOverduePendingOccurrenceAsMissed()
    {
        var seriesId = Guid.NewGuid();
        var occurrenceDate = new DateTime(2026, 10, 12);
        var request = new LocalScheduleRequest(
            [
                new LocalTaskRequest(
                    seriesId,
                    "asdf",
                    false,
                    0,
                    5,
                    5,
                    0,
                    occurrenceDate.AddHours(17),
                    null,
                    TimeSpan.FromHours(1),
                    [],
                    false,
                    null,
                    null,
                    false,
                    new WeeklyRecurrenceRule
                    {
                        SeriesStartDate = occurrenceDate.AddHours(17),
                        DaysOfWeek =
                        [
                            DayOfWeek.Monday,
                            DayOfWeek.Tuesday,
                            DayOfWeek.Wednesday,
                            DayOfWeek.Thursday,
                            DayOfWeek.Friday,
                            DayOfWeek.Saturday,
                            DayOfWeek.Sunday
                        ],
                        EndCondition = new AfterOccurrencesEndCondition { OccurrenceCount = 7 }
                    },
                    seriesId,
                    TaskProgressionMode.SequentialCatchUp,
                    1,
                    [])
            ],
            [],
            new LocalUserProfileRequest(
                new TimeOnly(9),
                new TimeOnly(17),
                [DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday],
                SchedulingMode.GoldPanning,
                TimeSpan.FromMinutes(15),
                0.5,
                1,
                3,
                5),
            new DateTime(2026, 10, 13, 11, 0, 0));

        var result = ScheduleComputation.Compute(request);

        var response = Assert.IsType<Ok<LocalScheduleResponse>>(result).Value;
        var occurrence = Assert.Single(response!.UnscheduledTasks!);
        Assert.Equal(occurrenceDate, occurrence.OccurrenceDate);
        Assert.Equal(TaskOccurrenceStatus.Missed, occurrence.OccurrenceStatus);
        Assert.True(occurrence.HasMissedOccurrence);
        Assert.True(occurrence.IsMissed);
    }
}

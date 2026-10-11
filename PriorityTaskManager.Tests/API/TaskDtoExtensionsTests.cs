using PriorityTaskManager.API.Tasks;
using PriorityTaskManager.Models;

namespace PriorityTaskManager.Tests.API
{
    public class TaskDtoExtensionsTests
    {
        [Fact]
        public void ToNewTaskItem_MapsLinkFromRequest()
        {
            var request = new TaskRequest(
                "Review proposal",
                "Review notes",
                Guid.NewGuid(),
                5,
                null,
                null,
                TimeSpan.FromHours(1),
                new List<Guid>(),
                false,
                1,
                0,
                null,
                null,
                Link: "https://example.com/reference");

            var task = request.ToNewTaskItem();

            Assert.Equal("https://example.com/reference", task.Link);
            Assert.Equal(task.Link, task.ToResponse().Link);
        }

        [Fact]
        public void ToNewTaskItem_MapsRecurringTaskSettings()
        {
            var rule = new DailyIntervalRecurrenceRule
            {
                SeriesStartDate = new DateTime(2026, 10, 8),
                IntervalDays = 2,
                EndCondition = new NeverEndCondition()
            };
            var request = new TaskRequest(
                "Review proposal",
                null,
                Guid.NewGuid(),
                5,
                null,
                null,
                TimeSpan.FromHours(1),
                new List<Guid>(),
                false,
                1,
                0,
                null,
                null,
                RecurrenceRule: rule,
                ProgressionMode: TaskProgressionMode.SequentialCatchUp,
                RequiredCompletions: 3,
                ShowMissedIndicator: false,
                TrackStreak: true);

            var task = request.ToNewTaskItem();

            Assert.Equal(rule.SeriesStartDate, task.RecurrenceRule!.SeriesStartDate);
            Assert.Equal(TaskProgressionMode.SequentialCatchUp, task.ProgressionMode);
            Assert.Equal(3, task.RequiredCompletions);
            Assert.False(task.ShowMissedIndicator);
            Assert.True(task.TrackStreak);
        }

        [Fact]
        public void OccurrenceResponse_MapsPerOccurrenceProgressAndRemainingDuration()
        {
            var task = new TaskItem
            {
                Id = Guid.NewGuid(),
                Title = "Review proposal",
                DueDate = new DateTime(2026, 10, 8, 16, 30, 0),
                EstimatedDuration = TimeSpan.FromMinutes(90),
                RequiredCompletions = 3,
                ShowMissedIndicator = true,
                TrackStreak = true,
                CurrentStreak = 2,
                BestStreak = 4,
                OccurrenceStates =
                [
                    new TaskOccurrenceState
                    {
                        ScheduledDate = new DateTime(2026, 10, 8),
                        Status = TaskOccurrenceStatus.Missed,
                        CompletionCount = 1
                    }
                ]
            };
            var occurrence = new TaskOccurrence(
                task,
                new DateTime(2026, 10, 8),
                TaskOccurrenceStatus.Missed,
                1,
                IsMissed: true,
                CurrentStreak: 2,
                BestStreak: 4);

            var response = occurrence.ToResponse();

            Assert.Equal(task.Id, response.SeriesId);
            Assert.Equal(1.0 / 3.0, response.Progress, 5);
            Assert.Equal(TimeSpan.FromMinutes(60), response.EstimatedDuration);
            Assert.Equal(TaskOccurrenceStatus.Missed, response.Status);
            Assert.Equal(new DateTime(2026, 10, 8, 16, 30, 0), response.DueDate);
            Assert.True(response.HasMissedOccurrence);
            Assert.Equal(2, response.CurrentStreak);
            Assert.Equal(4, response.BestStreak);
        }

        [Fact]
        public void OccurrenceResponse_UsesOverrideFieldsAndKeepsOriginalRecurrenceDate()
        {
            var originalDate = new DateTime(2026, 10, 8);
            var task = new TaskItem
            {
                Id = Guid.NewGuid(),
                SeriesId = Guid.NewGuid(),
                Title = "Series title",
                Importance = 5,
                EffectiveImportance = 5,
                EstimatedDuration = TimeSpan.FromMinutes(90),
                RequiredCompletions = 3,
                RecurrenceRule = new DailyIntervalRecurrenceRule
                {
                    SeriesStartDate = originalDate,
                    IntervalDays = 1,
                    EndCondition = new NeverEndCondition()
                },
                OccurrenceOverrides =
                [
                    new TaskOccurrenceOverride
                    {
                        OriginalOccurrenceDate = originalDate,
                        Title = "Moved title",
                        Description = "Occurrence notes",
                        DueDate = new DateTime(2026, 10, 12),
                        EstimatedDuration = TimeSpan.FromMinutes(120),
                        Importance = 8,
                        Complexity = 4,
                        Link = "https://example.com"
                    }
                ]
            };
            var occurrence = new TaskOccurrence(
                task,
                originalDate,
                TaskOccurrenceStatus.Pending,
                0,
                IsMissed: false,
                CurrentStreak: 0,
                BestStreak: 0);

            var response = occurrence.ToResponse();

            Assert.Equal(originalDate, response.OccurrenceDate);
            Assert.Equal("Moved title", response.Title);
            Assert.Equal(new DateTime(2026, 10, 12), response.DueDate);
            Assert.Equal(8, response.Importance);
            Assert.Equal(8, response.EffectiveImportance);
            Assert.Equal(TimeSpan.FromMinutes(120), response.FullEstimatedDuration);
            Assert.Equal(TimeSpan.FromMinutes(120), response.EstimatedDuration);
            Assert.NotNull(response.RecurrenceRule);
        }

        [Fact]
        public void TaskResponse_ExposesPersistedOccurrenceStatesToTheScheduler()
        {
            var task = new TaskItem
            {
                OccurrenceStates =
                [
                    new TaskOccurrenceState
                    {
                        ScheduledDate = new DateTime(2026, 10, 8),
                        Status = TaskOccurrenceStatus.Missed,
                        CompletionCount = 1
                    }
                ]
            };

            var response = task.ToResponse();

            var state = Assert.Single(response.OccurrenceStates);
            Assert.Equal(new DateTime(2026, 10, 8), state.ScheduledDate);
            Assert.Equal(TaskOccurrenceStatus.Missed, state.Status);
            Assert.Equal(1, state.CompletionCount);
        }
    }
}

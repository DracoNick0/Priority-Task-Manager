using PriorityTaskManager.Models;
using PriorityTaskManager.Services;
using PriorityTaskManager.Tests.Infrastructure;

namespace PriorityTaskManager.Tests.Services
{
    public class TaskMetricsServiceTests
    {
        private readonly TaskMetricsService _service = new();

        [Fact]
        public void FindTargetDayForSlackMeter_WhenWithinWorkday_ShouldReturnCurrentDay()
        {
            var profile = DeterministicTestFixtures.CreateStandardUserProfile();
            var currentTime = new DateTime(2026, 7, 6, 14, 0, 0); // Monday

            var target = _service.FindTargetDayForSlackMeter(currentTime, profile);

            Assert.Equal(currentTime.Date, target);
        }

        [Fact]
        public void FindTargetDayForSlackMeter_WhenAfterWorkday_ShouldReturnNextWorkday()
        {
            var profile = DeterministicTestFixtures.CreateStandardUserProfile();
            var currentTime = new DateTime(2026, 7, 10, 18, 0, 0); // Friday after work hours

            var target = _service.FindTargetDayForSlackMeter(currentTime, profile);

            Assert.Equal(new DateTime(2026, 7, 13), target); // Monday
        }

        [Fact]
        public void CalculateRealisticSlack_WhenNoScheduleOrDueDate_ShouldReturnMaxValue()
        {
            var profile = DeterministicTestFixtures.CreateStandardUserProfile();
            var task = DeterministicTestFixtures.CreateTask("No schedule", dueDate: null);

            var slack = _service.CalculateRealisticSlack(task, profile);

            Assert.Equal(TimeSpan.MaxValue, slack);
        }

        [Fact]
        public void CalculateRealisticSlack_WhenScheduledWithinDay_ShouldUseWorkdayEndForSameDayCalculation()
        {
            var profile = DeterministicTestFixtures.CreateStandardUserProfile();
            var task = DeterministicTestFixtures.CreateTask(
                title: "Focused Work",
                durationHours: 2,
                dueDate: new DateTime(2026, 7, 6, 15, 0, 0));

            task.ScheduledParts = new List<ScheduledChunk>
            {
                new ScheduledChunk
                {
                    StartTime = new DateTime(2026, 7, 6, 10, 0, 0),
                    EndTime = new DateTime(2026, 7, 6, 12, 0, 0)
                }
            };

            var slack = _service.CalculateRealisticSlack(task, profile);

            Assert.Equal(TimeSpan.FromHours(5), slack);
        }

        [Fact]
        public void CalculateRealisticSlack_WithNonContiguousChunksAcrossWeekend_ShouldUseLatestChunkEndAndSkipNonWorkdays()
        {
            var profile = DeterministicTestFixtures.CreateStandardUserProfile();
            var task = DeterministicTestFixtures.CreateTask(
                title: "Split Work",
                durationHours: 3,
                dueDate: new DateTime(2026, 7, 13, 12, 0, 0)); // Monday

            task.ScheduledParts = new List<ScheduledChunk>
            {
                new ScheduledChunk
                {
                    StartTime = new DateTime(2026, 7, 10, 9, 0, 0), // Friday
                    EndTime = new DateTime(2026, 7, 10, 11, 0, 0)
                },
                new ScheduledChunk
                {
                    StartTime = new DateTime(2026, 7, 10, 13, 0, 0), // Friday
                    EndTime = new DateTime(2026, 7, 10, 14, 0, 0)
                }
            };

            var slack = _service.CalculateRealisticSlack(task, profile);

            // Friday 14:00-17:00 (3h) + Saturday/Sunday skipped + Monday 09:00-12:00 (3h) = 6h.
            Assert.Equal(TimeSpan.FromHours(6), slack);
        }

        [Fact]
        public void CalculateActualSlack_ShouldUseLatestScheduledChunkEnd()
        {
            var profile = DeterministicTestFixtures.CreateStandardUserProfile();
            var task = DeterministicTestFixtures.CreateTask(
                title: "Chunks",
                durationHours: 3,
                dueDate: new DateTime(2026, 7, 7, 17, 0, 0));

            task.ScheduledParts = new List<ScheduledChunk>
            {
                new ScheduledChunk
                {
                    StartTime = new DateTime(2026, 7, 7, 9, 0, 0),
                    EndTime = new DateTime(2026, 7, 7, 10, 0, 0)
                },
                new ScheduledChunk
                {
                    StartTime = new DateTime(2026, 7, 7, 13, 0, 0),
                    EndTime = new DateTime(2026, 7, 7, 15, 30, 0)
                }
            };

            var slack = _service.CalculateActualSlack(task, profile);

            Assert.Equal(TimeSpan.FromHours(1.5), slack);
        }

        [Fact]
        public void CalculateActualSlack_WhenDueDateIsAfterWorkEnd_ShouldNotCapToWorkdayEnd()
        {
            var profile = DeterministicTestFixtures.CreateStandardUserProfile();
            var task = DeterministicTestFixtures.CreateTask(
                title: "Late due date",
                durationHours: 3,
                dueDate: new DateTime(2026, 7, 7, 20, 0, 0)); // Work end is 17:00; due date is later.

            task.ScheduledParts = new List<ScheduledChunk>
            {
                new ScheduledChunk
                {
                    StartTime = new DateTime(2026, 7, 7, 13, 0, 0),
                    EndTime = new DateTime(2026, 7, 7, 15, 30, 0)
                }
            };

            var slack = _service.CalculateActualSlack(task, profile);

            // Raw wall-clock gap to the due date - non-working hours after workday end must count as slack.
            Assert.Equal(TimeSpan.FromHours(4.5), slack);
        }
    }
}

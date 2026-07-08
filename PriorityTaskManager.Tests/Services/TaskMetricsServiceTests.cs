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
        public void CalculateActualSlack_ShouldUseLatestScheduledChunkEnd()
        {
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

            var slack = _service.CalculateActualSlack(task);

            Assert.Equal(TimeSpan.FromHours(1.5), slack);
        }
    }
}

using PriorityTaskManager.Models;

namespace PriorityTaskManager.Tests.Infrastructure
{
    /// <summary>
    /// Shared deterministic fixtures for tests that depend on time and scheduling defaults.
    /// </summary>
    public static class DeterministicTestFixtures
    {
        public static readonly DateTime StandardNow = new(2024, 1, 1, 9, 0, 0);

        public static UserProfile CreateStandardUserProfile()
        {
            return new UserProfile
            {
                WorkStartTime = new TimeOnly(9, 0),
                WorkEndTime = new TimeOnly(17, 0),
                WorkDays = new List<DayOfWeek>
                {
                    DayOfWeek.Monday,
                    DayOfWeek.Tuesday,
                    DayOfWeek.Wednesday,
                    DayOfWeek.Thursday,
                    DayOfWeek.Friday
                }
            };
        }

        public static TaskItem CreateTask(
            string title,
            int listId = 1,
            int importance = 3,
            double complexity = 3.0,
            double durationHours = 1,
            DateTime? dueDate = null)
        {
            return new TaskItem
            {
                Title = title,
                ListId = listId,
                Importance = importance,
                EffectiveImportance = importance,
                Complexity = complexity,
                EstimatedDuration = TimeSpan.FromHours(durationHours),
                DueDate = dueDate
            };
        }

        public static MockTimeService CreateMockTimeService(DateTime? now = null)
        {
            var timeService = new MockTimeService();
            timeService.SetCurrentTime(now ?? StandardNow);
            return timeService;
        }
    }
}

using PriorityTaskManager.CLI.Utils;
using PriorityTaskManager.Models;
using PriorityTaskManager.Scheduling.GoldPanning;
using PriorityTaskManager.Services;
using PriorityTaskManager.Tests.Infrastructure;

namespace PriorityTaskManager.Tests.CLI
{
    public class NonInteractiveCommandResultHelperTests
    {
        [Fact]
        public void ParseDisplayIds_WithMixedInput_ShouldCategorizeIdsCorrectly()
        {
            var service = CreateService();
            var first = AddTask(service, "One");

            var result = NonInteractiveCommandResultHelper.ParseDisplayIds(service, new[] { $"{first.DisplayId},abc,999" });

            Assert.True(result.HasInput);
            Assert.Single(result.ValidTasks);
            Assert.Equal(first.DisplayId, result.ValidTasks[0].DisplayId);
            Assert.Contains("abc", result.InvalidTokens);
            Assert.Contains(999, result.NotFoundDisplayIds);
        }

        [Fact]
        public void BuildNoValidIdsMessage_WithNoInput_ShouldIncludeUsage()
        {
            var parseResult = new ParsedTaskIdInput();

            var message = NonInteractiveCommandResultHelper.BuildNoValidIdsMessage(parseResult, "Usage: complete <Id1>,<Id2>,...");

            Assert.Contains("No task IDs provided.", message);
            Assert.Contains("Usage: complete", message);
        }

        [Fact]
        public void AppendParseWarnings_WithInvalidAndMissingIds_ShouldWriteBothWarnings()
        {
            var parseResult = new ParsedTaskIdInput
            {
                HasInput = true
            };
            parseResult.InvalidTokens.Add("bad");
            parseResult.NotFoundDisplayIds.Add(42);

            var builder = new System.Text.StringBuilder();
            NonInteractiveCommandResultHelper.AppendParseWarnings(builder, parseResult);
            var message = builder.ToString();

            Assert.Contains("Invalid IDs: bad.", message);
            Assert.Contains("Not found in active list: 42.", message);
        }

        private static TaskManagerService CreateService()
        {
            var persistence = new MockPersistenceService();
            var timeService = DeterministicTestFixtures.CreateMockTimeService(new DateTime(2026, 7, 8, 9, 0, 0));
            var data = persistence.LoadData();
            var strategy = new GoldPanningStrategy(data.UserProfile, data.Events, timeService);
            return new TaskManagerService(strategy, persistence, data);
        }

        private static TaskItem AddTask(TaskManagerService service, string title)
        {
            var task = new TaskItem
            {
                Title = title,
                ListId = service.GetActiveListId(),
                Importance = 5,
                Complexity = 5,
                EstimatedDuration = TimeSpan.FromHours(1)
            };

            service.AddTask(task);
            return task;
        }
    }
}
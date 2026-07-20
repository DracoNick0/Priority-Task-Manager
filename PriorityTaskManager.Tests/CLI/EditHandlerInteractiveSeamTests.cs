using PriorityTaskManager.CLI.Handlers;
using PriorityTaskManager.CLI.Utils;
using PriorityTaskManager.Models;
using PriorityTaskManager.Scheduling.GoldPanning;
using PriorityTaskManager.Services;
using PriorityTaskManager.Tests.Infrastructure;

namespace PriorityTaskManager.Tests.CLI
{
    public class EditHandlerInteractiveSeamTests
    {
        [Fact]
        public void Execute_InteractiveMode_WhenEscapePressed_ShouldExitWithoutMutatingTask()
        {
            var (service, metrics, snapshotProvider) = CreateContext();
            var task = new TaskItem
            {
                Title = "Original title",
                ListId = service.GetActiveListId(),
                Importance = 5,
                Complexity = 5,
                EstimatedDuration = TimeSpan.FromHours(1)
            };
            service.AddTask(task);
            var fakeConsole = new FakeInteractiveConsoleFacade(new[]
            {
                new ConsoleKeyInfo('\u001b', ConsoleKey.Escape, false, false, false)
            });
            var handler = new EditHandler(snapshotProvider, metrics, fakeConsole);

            handler.ExecuteWithResult(service, new[] { task.DisplayId.ToString() });

            var updated = service.GetTaskById(task.Id);
            Assert.NotNull(updated);
            Assert.Equal("Original title", updated.Title);
            Assert.True(fakeConsole.CursorVisible);
            Assert.True(fakeConsole.DashboardRenderCount >= 1);
        }

        private static (TaskManagerService Service, TaskMetricsService Metrics, ScheduleSnapshotProvider SnapshotProvider) CreateContext()
        {
            var persistence = new MockPersistenceService();
            var timeService = DeterministicTestFixtures.CreateMockTimeService(new DateTime(2026, 7, 8, 9, 0, 0));
            var data = persistence.LoadData();
            var strategy = new GoldPanningStrategy(data.UserProfile, data.Events, timeService);
            var service = new TaskManagerService(strategy, persistence, data);
            var metrics = new TaskMetricsService();
            var snapshotProvider = new ScheduleSnapshotProvider(service, metrics, timeService);
            snapshotProvider.RefreshActiveListSnapshot(out _);

            return (service, metrics, snapshotProvider);
        }

        private sealed class FakeInteractiveConsoleFacade : IInteractiveConsoleFacade
        {
            private readonly Queue<ConsoleKeyInfo> _keys;

            public FakeInteractiveConsoleFacade(IEnumerable<ConsoleKeyInfo> keys)
            {
                _keys = new Queue<ConsoleKeyInfo>(keys);
            }

            public bool CursorVisible { get; set; }

            public int CursorTop => 0;

            public int DashboardRenderCount { get; private set; }

            public ConsoleKeyInfo ReadKey(bool intercept)
            {
                if (_keys.Count == 0)
                {
                    return new ConsoleKeyInfo('\u001b', ConsoleKey.Escape, false, false, false);
                }

                return _keys.Dequeue();
            }

            public string? ReadLine()
            {
                return null;
            }

            public void Write(string text)
            {
            }

            public void WriteLine(string text)
            {
            }

            public void ClearAndRenderDashboard(ScheduleSnapshotProvider snapshotProvider, ITaskMetricsService taskMetricsService)
            {
                DashboardRenderCount++;
            }

            public void DrawMenuItems(IReadOnlyList<string> items, int selectedIndex, int startLine)
            {
            }

            public void UpdateMenuSelection(IReadOnlyList<string> items, int previousIndex, int selectedIndex, int startLine)
            {
            }

            public bool TryPromptInlineInput(int row, string prefix, string initialValue, out string value)
            {
                value = initialValue;
                return false;
            }

            public bool RunToggleSelectionMenu<T>(string title, string instructions, IList<T> selectedItems, IReadOnlyList<T> allItems, Func<T, string> labelSelector)
            {
                return false;
            }

            public void RunAdjustableValueMenu(string title, string instructions, List<ConsoleMenuHelper.AdjustableMenuOption> options)
            {
            }
        }
    }
}

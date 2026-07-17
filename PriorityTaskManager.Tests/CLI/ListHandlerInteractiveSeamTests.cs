using PriorityTaskManager.CLI.Handlers;
using PriorityTaskManager.CLI.Utils;
using PriorityTaskManager.Models;
using PriorityTaskManager.Scheduling.GoldPanning;
using PriorityTaskManager.Services;
using PriorityTaskManager.Tests.Infrastructure;

namespace PriorityTaskManager.Tests.CLI
{
    public class ListHandlerInteractiveSeamTests
    {
        [Fact]
        public void Execute_ListSettingsInteractive_WhenEscapePressed_ShouldCancelWithoutMutatingActiveList()
        {
            var (service, timeService, metrics, snapshotProvider) = CreateContext();
            var activeList = service.GetListById(service.GetActiveListId());
            Assert.NotNull(activeList);
            var originalName = activeList.Name;
            var fakeConsole = new FakeInteractiveConsoleFacade(new[]
            {
                new ConsoleKeyInfo('\u001b', ConsoleKey.Escape, false, false, false)
            });
            var handler = new ListHandler(metrics, timeService, snapshotProvider, fakeConsole);

            handler.Execute(service, new[] { "settings" });

            var updatedList = service.GetListById(service.GetActiveListId());
            Assert.NotNull(updatedList);
            Assert.Equal(originalName, updatedList.Name);
            Assert.True(fakeConsole.CursorVisible);
            Assert.True(fakeConsole.DashboardRenderCount >= 2);
            Assert.Contains(fakeConsole.Lines, line => line.Contains("List settings update cancelled."));
        }

        private static (TaskManagerService Service, MockTimeService TimeService, TaskMetricsService Metrics, ScheduleSnapshotProvider SnapshotProvider) CreateContext()
        {
            var persistence = new MockPersistenceService();
            var timeService = DeterministicTestFixtures.CreateMockTimeService(new DateTime(2026, 7, 8, 9, 0, 0));
            var data = persistence.LoadData();
            var strategy = new GoldPanningStrategy(data.UserProfile, data.Events, timeService);
            var service = new TaskManagerService(strategy, persistence, data);
            var metrics = new TaskMetricsService();
            var snapshotProvider = new ScheduleSnapshotProvider(service, metrics, timeService);
            snapshotProvider.RefreshActiveListSnapshot(out _);

            return (service, timeService, metrics, snapshotProvider);
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

            public List<string> Lines { get; } = new();

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
                Lines.Add(text);
            }

            public void WriteLine(string text)
            {
                Lines.Add(text);
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

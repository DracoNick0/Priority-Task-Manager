using PriorityTaskManager.CLI.Handlers;
using PriorityTaskManager.CLI.Utils;
using PriorityTaskManager.Models;
using PriorityTaskManager.Scheduling.GoldPanning;
using PriorityTaskManager.Services;
using PriorityTaskManager.Tests.Infrastructure;

namespace PriorityTaskManager.Tests.CLI
{
    public class HelpHandlerInteractiveSeamTests
    {
        [Fact]
        public void Execute_WhenEscapePressed_ShouldExitAndRestoreCursorVisibility()
        {
            var (service, metrics, snapshotProvider) = CreateContext();
            var fakeConsole = new FakeInteractiveConsoleFacade(new[]
            {
                new ConsoleKeyInfo('\u001b', ConsoleKey.Escape, false, false, false)
            });
            var handler = new HelpHandler(snapshotProvider, metrics, fakeConsole);

            handler.Execute(service, Array.Empty<string>());

            Assert.True(fakeConsole.CursorVisible);
            Assert.True(fakeConsole.DashboardRenderCount >= 2);
        }

        [Fact]
        public void Execute_WhenSelectingCategory_ShouldRenderCategoryHelpText()
        {
            var (service, metrics, snapshotProvider) = CreateContext();
            var fakeConsole = new FakeInteractiveConsoleFacade(new[]
            {
                new ConsoleKeyInfo('\r', ConsoleKey.Enter, false, false, false),
                new ConsoleKeyInfo('x', ConsoleKey.X, false, false, false),
                new ConsoleKeyInfo('\u001b', ConsoleKey.Escape, false, false, false)
            });
            var handler = new HelpHandler(snapshotProvider, metrics, fakeConsole);

            handler.Execute(service, Array.Empty<string>());

            Assert.Contains(fakeConsole.Lines, line => line.Contains("Task Commands:"));
            Assert.Contains(fakeConsole.Lines, line => line.Contains("add <Title>"));
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
        }
    }
}
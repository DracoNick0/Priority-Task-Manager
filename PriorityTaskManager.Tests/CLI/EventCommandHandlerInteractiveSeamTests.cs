using PriorityTaskManager.CLI.Handlers;
using PriorityTaskManager.CLI.Utils;
using PriorityTaskManager.Models;
using PriorityTaskManager.Scheduling.GoldPanning;
using PriorityTaskManager.Services;
using PriorityTaskManager.Tests.Infrastructure;

namespace PriorityTaskManager.Tests.CLI
{
    public class EventCommandHandlerInteractiveSeamTests
    {
        [Fact]
        public void Execute_EditInteractive_WhenEscapePressed_ShouldExitWithoutMutatingEvent()
        {
            var (service, metrics, snapshotProvider) = CreateContext();
            var existingEvent = new Event
            {
                Name = "Planning",
                StartTime = new DateTime(2026, 7, 8, 10, 0, 0),
                EndTime = new DateTime(2026, 7, 8, 11, 0, 0)
            };
            service.AddEvent(existingEvent);
            var eventId = service.GetAllEvents().First().Id;
            var fakeConsole = new FakeInteractiveConsoleFacade(new[]
            {
                new ConsoleKeyInfo('\u001b', ConsoleKey.Escape, false, false, false)
            });
            var handler = new EventCommandHandler(snapshotProvider, metrics, fakeConsole);

            handler.Execute(service, new[] { "edit", eventId.ToString() });

            var updated = service.GetEvent(eventId);
            Assert.NotNull(updated);
            Assert.Equal("Planning", updated.Name);
            Assert.Contains(fakeConsole.Lines, line => line.Contains("Edit cancelled."));
        }

        [Fact]
        public void Execute_ClearInteractive_WhenDeclined_ShouldKeepEvents()
        {
            var (service, metrics, snapshotProvider) = CreateContext();
            service.AddEvent(new Event
            {
                Name = "Blocker",
                StartTime = new DateTime(2026, 7, 8, 12, 0, 0),
                EndTime = new DateTime(2026, 7, 8, 13, 0, 0)
            });
            var fakeConsole = new FakeInteractiveConsoleFacade(Array.Empty<ConsoleKeyInfo>())
            {
                InputLine = "n"
            };
            var handler = new EventCommandHandler(snapshotProvider, metrics, fakeConsole);

            handler.Execute(service, new[] { "clear" });

            Assert.Single(service.GetAllEvents());
            Assert.Contains(fakeConsole.Lines, line => line.Contains("Operation cancelled."));
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

            public string? InputLine { get; set; }

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
                return InputLine;
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

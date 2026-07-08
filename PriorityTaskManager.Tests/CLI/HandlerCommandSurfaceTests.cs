using PriorityTaskManager.CLI.Handlers;
using PriorityTaskManager.CLI.Utils;
using PriorityTaskManager.Models;
using PriorityTaskManager.Scheduling.GoldPanning;
using PriorityTaskManager.Services;
using PriorityTaskManager.Tests.Infrastructure;

namespace PriorityTaskManager.Tests.CLI
{
    [Collection("CLI Console")]
    public class HandlerCommandSurfaceTests
    {
        [Fact]
        public void DeleteHandler_WithValidDisplayId_ShouldDeleteTask()
        {
            var ctx = CreateContext();
            var task = AddTask(ctx.Service, "Delete me");
            var handler = new DeleteHandler(ctx.SnapshotProvider, ctx.TaskMetricsService);

            handler.Execute(ctx.Service, new[] { task.DisplayId.ToString() });

            Assert.Null(ctx.Service.GetTaskById(task.Id));
        }

        [Fact]
        public void CompleteHandler_WithValidDisplayId_ShouldMarkTaskComplete()
        {
            var ctx = CreateContext();
            var task = AddTask(ctx.Service, "Complete me");
            var handler = new CompleteHandler(ctx.SnapshotProvider, ctx.TaskMetricsService);

            handler.Execute(ctx.Service, new[] { task.DisplayId.ToString() });

            var updated = ctx.Service.GetTaskById(task.Id);
            Assert.NotNull(updated);
            Assert.True(updated.IsCompleted);
        }

        [Fact]
        public void UncompleteHandler_WithValidDisplayId_ShouldMarkTaskIncomplete()
        {
            var ctx = CreateContext();
            var task = AddTask(ctx.Service, "Uncomplete me");
            ctx.Service.MarkTaskAsComplete(task.Id);
            var handler = new UncompleteHandler(ctx.SnapshotProvider, ctx.TaskMetricsService);

            handler.Execute(ctx.Service, new[] { task.DisplayId.ToString() });

            var updated = ctx.Service.GetTaskById(task.Id);
            Assert.NotNull(updated);
            Assert.False(updated.IsCompleted);
        }

        [Fact]
        public void DependHandler_AddThenRemove_ShouldUpdateDependencies()
        {
            var ctx = CreateContext();
            var parent = AddTask(ctx.Service, "Parent");
            var child = AddTask(ctx.Service, "Child");
            var handler = new DependHandler(ctx.SnapshotProvider, ctx.TaskMetricsService);

            handler.Execute(ctx.Service, new[] { "add", child.DisplayId.ToString(), parent.DisplayId.ToString() });
            var afterAdd = ctx.Service.GetTaskById(child.Id);
            Assert.NotNull(afterAdd);
            Assert.Contains(parent.Id, afterAdd.Dependencies);

            handler.Execute(ctx.Service, new[] { "remove", child.DisplayId.ToString(), parent.DisplayId.ToString() });
            var afterRemove = ctx.Service.GetTaskById(child.Id);
            Assert.NotNull(afterRemove);
            Assert.DoesNotContain(parent.Id, afterRemove.Dependencies);
        }

        [Fact]
        public void ViewHandler_WithInvalidArgs_ShouldNotThrow()
        {
            var ctx = CreateContext();
            var handler = new ViewHandler(ctx.SnapshotProvider, ctx.TaskMetricsService);

            var exception = Record.Exception(() => handler.Execute(ctx.Service, new[] { "abc" }));

            Assert.Null(exception);
        }

        [Fact]
        public void ListHandler_CreateAndSwitch_ShouldManageLists()
        {
            var ctx = CreateContext();
            var handler = new ListHandler(ctx.TaskMetricsService, ctx.TimeService, ctx.SnapshotProvider);

            handler.Execute(ctx.Service, new[] { "create", "Roadmap" });
            var created = ctx.Service.GetListByName("Roadmap");
            Assert.NotNull(created);

            handler.Execute(ctx.Service, new[] { "switch", "Roadmap" });
            Assert.Equal(created.Id, ctx.Service.GetActiveListId());
        }

        [Fact]
        public void EditHandler_TargetedTitleUpdate_ShouldPersistChange()
        {
            var ctx = CreateContext();
            var task = AddTask(ctx.Service, "Before");
            var handler = new EditHandler(ctx.SnapshotProvider, ctx.TaskMetricsService);

            handler.Execute(ctx.Service, new[] { task.DisplayId.ToString(), "title", "After" });

            var updated = ctx.Service.GetTaskById(task.Id);
            Assert.NotNull(updated);
            Assert.Equal("After", updated.Title);
        }

        [Fact]
        public void CleanupHandler_WhenNotConfirmed_ShouldKeepCompletedTasks()
        {
            var ctx = CreateContext();
            var task = AddTask(ctx.Service, "Completed");
            ctx.Service.MarkTaskAsComplete(task.Id);
            var handler = new CleanupHandler(ctx.Service, ctx.SnapshotProvider, ctx.TaskMetricsService);

            var originalIn = Console.In;
            try
            {
                Console.SetIn(new StringReader("no\n"));
                handler.Execute(ctx.Service, Array.Empty<string>());
            }
            finally
            {
                Console.SetIn(originalIn);
            }

            Assert.NotNull(ctx.Service.GetTaskById(task.Id));
        }

        [Fact]
        public void SettingsHandler_WithFlags_ShouldUpdateUserProfile()
        {
            var ctx = CreateContext();
            var handler = new SettingsHandler(ctx.TimeService, ctx.SnapshotProvider, ctx.TaskMetricsService);

            handler.Execute(ctx.Service, new[]
            {
                "--default-sort", "duedate",
                "--default-mode", "gold",
                "--working-days", "Monday,Wednesday",
                "--working-hours", "08:30-16:45",
                "--set-slack", "0.4", "1.2", "2.5", "4.0"
            });

            var profile = ctx.Service.GetUserProfile();
            Assert.Equal(SortOption.DueDate, profile.DefaultListSortOption);
            Assert.Equal(SchedulingMode.GoldPanning, profile.SchedulingMode);
            Assert.Equal(new List<DayOfWeek> { DayOfWeek.Monday, DayOfWeek.Wednesday }, profile.WorkDays);
            Assert.Equal(new TimeOnly(8, 30), profile.WorkStartTime);
            Assert.Equal(new TimeOnly(16, 45), profile.WorkEndTime);
            Assert.Equal(0.4, profile.SlackThresholdDire);
            Assert.Equal(1.2, profile.SlackThresholdPressing);
            Assert.Equal(2.5, profile.SlackThresholdFocus);
            Assert.Equal(4.0, profile.SlackThresholdSafe);
        }

        [Fact]
        public void EventCommandHandler_Delete_ShouldRemoveEvent()
        {
            var ctx = CreateContext();
            ctx.Service.AddEvent(new Event
            {
                Name = "Blocker",
                StartTime = new DateTime(2026, 7, 8, 10, 0, 0),
                EndTime = new DateTime(2026, 7, 8, 11, 0, 0)
            });
            var eventId = ctx.Service.GetAllEvents().First().Id;
            var handler = new EventCommandHandler(ctx.SnapshotProvider, ctx.TaskMetricsService);

            handler.Execute(ctx.Service, new[] { "delete", eventId.ToString() });

            Assert.Empty(ctx.Service.GetAllEvents());
        }

        [Fact]
        public void TimeHandler_Now_ShouldClearSimulatedTime()
        {
            var ctx = CreateContext();
            ctx.TimeService.SetSimulatedTime(new DateTime(2026, 7, 8, 12, 0, 0));
            var handler = new TimeHandler(ctx.TimeService, ctx.SnapshotProvider, ctx.TaskMetricsService);

            handler.Execute(ctx.Service, new[] { "now" });

            Assert.False(ctx.TimeService.IsSimulated());
        }

        [Fact]
        public void ModeHandler_Constraint_ShouldUpdateSchedulingMode()
        {
            var ctx = CreateContext();
            var handler = new ModeHandler(ctx.SnapshotProvider, ctx.TaskMetricsService);

            handler.Execute(ctx.Service, new[] { "constraint" });

            Assert.Equal(SchedulingMode.ConstraintOptimization, ctx.Service.GetUserProfile().SchedulingMode);
        }

        [Fact(Skip = "AddHandler always enters interactive prompt flow (PromptForInt/InteractiveDateInput). Covered later with input abstraction refactor.")]
        public void AddHandler_CommandSurfaceDeferred()
        {
        }

        [Fact(Skip = "HelpHandler is fully key-driven (Console.ReadKey loop). Covered later with help-content extraction.")]
        public void HelpHandler_CommandSurfaceDeferred()
        {
        }

        private static TestContext CreateContext()
        {
            var persistence = new MockPersistenceService();
            var timeService = DeterministicTestFixtures.CreateMockTimeService(new DateTime(2026, 7, 8, 9, 0, 0));
            var data = persistence.LoadData();
            var strategy = new GoldPanningStrategy(data.UserProfile, data.Events, timeService);
            var service = new TaskManagerService(strategy, persistence, data);
            var metrics = new TaskMetricsService();
            var snapshotProvider = new ScheduleSnapshotProvider(service, metrics, timeService);
            snapshotProvider.RefreshActiveListSnapshot(out _);

            return new TestContext(service, timeService, metrics, snapshotProvider);
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

        private sealed record TestContext(
            TaskManagerService Service,
            MockTimeService TimeService,
            TaskMetricsService TaskMetricsService,
            ScheduleSnapshotProvider SnapshotProvider);
    }
}

using PriorityTaskManager.CLI.Handlers;
using PriorityTaskManager.CLI.Interfaces;
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
        public void DeleteHandler_WithInvalidDisplayId_ShouldNotDeleteAnyTask()
        {
            var ctx = CreateContext();
            var task = AddTask(ctx.Service, "Keep me");
            var handler = new DeleteHandler(ctx.SnapshotProvider, ctx.TaskMetricsService);

            handler.Execute(ctx.Service, new[] { "not-a-number" });

            Assert.NotNull(ctx.Service.GetTaskById(task.Id));
            Assert.Single(ctx.Service.GetAllTasks(ctx.Service.GetActiveListId()));
        }

        [Fact]
        public void DeleteHandler_ResultPath_WithValidDisplayId_ShouldRequestDashboardRefresh()
        {
            var ctx = CreateContext();
            var task = AddTask(ctx.Service, "Delete via result");
            var handler = new DeleteHandler(ctx.SnapshotProvider, ctx.TaskMetricsService);

            var result = ((ICommandResultHandler)handler).ExecuteWithResult(ctx.Service, new[] { task.DisplayId.ToString() });

            Assert.Equal(CommandResultStatus.Success, result.Status);
            Assert.True(result.ShouldRefreshDashboard);
            Assert.Contains("Deleted 1 task(s)", result.Message);
            Assert.Null(ctx.Service.GetTaskById(task.Id));
        }

        [Fact]
        public void DeleteHandler_ResultPath_WithMissingArgs_ShouldReturnUsageWithoutRefresh()
        {
            var ctx = CreateContext();
            var handler = new DeleteHandler(ctx.SnapshotProvider, ctx.TaskMetricsService);

            var result = ((ICommandResultHandler)handler).ExecuteWithResult(ctx.Service, Array.Empty<string>());

            Assert.Equal(CommandResultStatus.Usage, result.Status);
            Assert.False(result.ShouldRefreshDashboard);
            Assert.Contains("Usage: delete", result.Message);
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
        public void CompleteHandler_WithNoArgs_ShouldNotMutateTaskState()
        {
            var ctx = CreateContext();
            var task = AddTask(ctx.Service, "Still incomplete");
            var handler = new CompleteHandler(ctx.SnapshotProvider, ctx.TaskMetricsService);

            handler.Execute(ctx.Service, Array.Empty<string>());

            var updated = ctx.Service.GetTaskById(task.Id);
            Assert.NotNull(updated);
            Assert.False(updated.IsCompleted);
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
        public void UncompleteHandler_WithNoArgs_ShouldNotMutateTaskState()
        {
            var ctx = CreateContext();
            var task = AddTask(ctx.Service, "Remain complete");
            ctx.Service.MarkTaskAsComplete(task.Id);
            var handler = new UncompleteHandler(ctx.SnapshotProvider, ctx.TaskMetricsService);

            handler.Execute(ctx.Service, Array.Empty<string>());

            var updated = ctx.Service.GetTaskById(task.Id);
            Assert.NotNull(updated);
            Assert.True(updated.IsCompleted);
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
        public void DependHandler_AddSelfDependency_ShouldNotAddDependency()
        {
            var ctx = CreateContext();
            var task = AddTask(ctx.Service, "Self");
            var handler = new DependHandler(ctx.SnapshotProvider, ctx.TaskMetricsService);

            handler.Execute(ctx.Service, new[] { "add", task.DisplayId.ToString(), task.DisplayId.ToString() });

            var updated = ctx.Service.GetTaskById(task.Id);
            Assert.NotNull(updated);
            Assert.Empty(updated.Dependencies);
        }

        [Fact]
        public void DependHandler_AddDuplicateDependency_ShouldRemainSingleEntry()
        {
            var ctx = CreateContext();
            var parent = AddTask(ctx.Service, "Parent");
            var child = AddTask(ctx.Service, "Child");
            var handler = new DependHandler(ctx.SnapshotProvider, ctx.TaskMetricsService);

            handler.Execute(ctx.Service, new[] { "add", child.DisplayId.ToString(), parent.DisplayId.ToString() });
            handler.Execute(ctx.Service, new[] { "add", child.DisplayId.ToString(), parent.DisplayId.ToString() });

            var updated = ctx.Service.GetTaskById(child.Id);
            Assert.NotNull(updated);
            Assert.Equal(1, updated.Dependencies.Count(dep => dep == parent.Id));
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
        public void ListHandler_CreateDuplicateName_ShouldNotCreateSecondList()
        {
            var ctx = CreateContext();
            var handler = new ListHandler(ctx.TaskMetricsService, ctx.TimeService, ctx.SnapshotProvider);

            handler.Execute(ctx.Service, new[] { "create", "Roadmap" });
            handler.Execute(ctx.Service, new[] { "create", "Roadmap" });

            var roadmapLists = ctx.Service.GetAllLists().Where(l => l.Name.Equals("Roadmap", StringComparison.OrdinalIgnoreCase)).ToList();
            Assert.Single(roadmapLists);
        }

        [Fact]
        public void ListHandler_SwitchToUnknownList_ShouldKeepActiveList()
        {
            var ctx = CreateContext();
            var handler = new ListHandler(ctx.TaskMetricsService, ctx.TimeService, ctx.SnapshotProvider);
            var originalActiveListId = ctx.Service.GetActiveListId();

            handler.Execute(ctx.Service, new[] { "switch", "DoesNotExist" });

            Assert.Equal(originalActiveListId, ctx.Service.GetActiveListId());
        }

        [Fact]
        public void ListHandler_DeleteCancelled_ShouldKeepList()
        {
            var ctx = CreateContext();
            var handler = new ListHandler(ctx.TaskMetricsService, ctx.TimeService, ctx.SnapshotProvider);
            handler.Execute(ctx.Service, new[] { "create", "Temporary" });
            Assert.NotNull(ctx.Service.GetListByName("Temporary"));

            var originalIn = Console.In;
            try
            {
                Console.SetIn(new StringReader("n\n"));
                handler.Execute(ctx.Service, new[] { "delete", "Temporary" });
            }
            finally
            {
                Console.SetIn(originalIn);
            }

            Assert.NotNull(ctx.Service.GetListByName("Temporary"));
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
        public void EditHandler_TargetedImportanceUpdate_ShouldClampToValidRange()
        {
            var ctx = CreateContext();
            var task = AddTask(ctx.Service, "Clamp me");
            var handler = new EditHandler(ctx.SnapshotProvider, ctx.TaskMetricsService);

            handler.Execute(ctx.Service, new[] { task.DisplayId.ToString(), "importance", "99" });

            var updated = ctx.Service.GetTaskById(task.Id);
            Assert.NotNull(updated);
            Assert.Equal(10, updated.Importance);
        }

        [Fact]
        public void EditHandler_UnknownAttribute_ShouldNotMutateTask()
        {
            var ctx = CreateContext();
            var task = AddTask(ctx.Service, "Original");
            var before = ctx.Service.GetTaskById(task.Id);
            Assert.NotNull(before);
            var originalTitle = before.Title;
            var originalImportance = before.Importance;
            var handler = new EditHandler(ctx.SnapshotProvider, ctx.TaskMetricsService);

            handler.Execute(ctx.Service, new[] { task.DisplayId.ToString(), "unknown-attribute", "new value" });

            var updated = ctx.Service.GetTaskById(task.Id);
            Assert.NotNull(updated);
            Assert.Equal(originalTitle, updated.Title);
            Assert.Equal(originalImportance, updated.Importance);
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
        public void CleanupHandler_WhenConfirmed_ShouldDeleteCompletedTasksOnly()
        {
            var ctx = CreateContext();
            var completed = AddTask(ctx.Service, "Completed");
            var remaining = AddTask(ctx.Service, "Remaining");
            ctx.Service.MarkTaskAsComplete(completed.Id);
            var handler = new CleanupHandler(ctx.Service, ctx.SnapshotProvider, ctx.TaskMetricsService);

            var originalIn = Console.In;
            try
            {
                Console.SetIn(new StringReader("confirm\n"));
                handler.Execute(ctx.Service, Array.Empty<string>());
            }
            finally
            {
                Console.SetIn(originalIn);
            }

            Assert.Null(ctx.Service.GetTaskById(completed.Id));
            Assert.NotNull(ctx.Service.GetTaskById(remaining.Id));
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
        public void SettingsHandler_WithInvalidWorkingHours_ShouldLeaveHoursUnchanged()
        {
            var ctx = CreateContext();
            var handler = new SettingsHandler(ctx.TimeService, ctx.SnapshotProvider, ctx.TaskMetricsService);
            var before = ctx.Service.GetUserProfile();
            var originalStart = before.WorkStartTime;
            var originalEnd = before.WorkEndTime;

            handler.Execute(ctx.Service, new[] { "--working-hours", "invalid" });

            var profile = ctx.Service.GetUserProfile();
            Assert.Equal(originalStart, profile.WorkStartTime);
            Assert.Equal(originalEnd, profile.WorkEndTime);
        }

        [Fact]
        public void SettingsHandler_WithInvalidDefaultMode_ShouldLeaveModeUnchanged()
        {
            var ctx = CreateContext();
            var handler = new SettingsHandler(ctx.TimeService, ctx.SnapshotProvider, ctx.TaskMetricsService);
            var originalMode = ctx.Service.GetUserProfile().SchedulingMode;

            handler.Execute(ctx.Service, new[] { "--default-mode", "unsupported" });

            Assert.Equal(originalMode, ctx.Service.GetUserProfile().SchedulingMode);
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
        public void EventCommandHandler_DeleteMixedValidAndInvalidIds_ShouldDeleteOnlyValidEvents()
        {
            var ctx = CreateContext();
            ctx.Service.AddEvent(new Event
            {
                Name = "Morning",
                StartTime = new DateTime(2026, 7, 8, 9, 0, 0),
                EndTime = new DateTime(2026, 7, 8, 10, 0, 0)
            });
            ctx.Service.AddEvent(new Event
            {
                Name = "Afternoon",
                StartTime = new DateTime(2026, 7, 8, 14, 0, 0),
                EndTime = new DateTime(2026, 7, 8, 15, 0, 0)
            });

            var eventIds = ctx.Service.GetAllEvents().Select(e => e.Id).OrderBy(id => id).ToList();
            var firstId = eventIds[0];
            var secondId = eventIds[1];
            var handler = new EventCommandHandler(ctx.SnapshotProvider, ctx.TaskMetricsService);

            handler.Execute(ctx.Service, new[] { "delete", $"{firstId},not-a-number,{secondId + 999}" });

            var remaining = ctx.Service.GetAllEvents().ToList();
            Assert.Single(remaining);
            Assert.Equal(secondId, remaining[0].Id);
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
        public void TimeHandler_UnknownCommand_ShouldNotChangeSimulatedState()
        {
            var ctx = CreateContext();
            ctx.TimeService.SetSimulatedTime(new DateTime(2026, 7, 8, 12, 0, 0));
            var handler = new TimeHandler(ctx.TimeService, ctx.SnapshotProvider, ctx.TaskMetricsService);

            handler.Execute(ctx.Service, new[] { "unknown-subcommand" });

            Assert.True(ctx.TimeService.IsSimulated());
        }

        [Fact]
        public void ModeHandler_Constraint_ShouldUpdateSchedulingMode()
        {
            var ctx = CreateContext();
            var handler = new ModeHandler(ctx.SnapshotProvider, ctx.TaskMetricsService);

            handler.Execute(ctx.Service, new[] { "constraint" });

            Assert.Equal(SchedulingMode.ConstraintOptimization, ctx.Service.GetUserProfile().SchedulingMode);
        }

        [Fact]
        public void ModeHandler_WithAliasV1_ShouldUpdateSchedulingModeToConstraint()
        {
            var ctx = CreateContext();
            var handler = new ModeHandler(ctx.SnapshotProvider, ctx.TaskMetricsService);

            handler.Execute(ctx.Service, new[] { "v1" });

            Assert.Equal(SchedulingMode.ConstraintOptimization, ctx.Service.GetUserProfile().SchedulingMode);
        }

        [Fact]
        public void ModeHandler_UnknownMode_ShouldLeaveSchedulingModeUnchanged()
        {
            var ctx = CreateContext();
            var handler = new ModeHandler(ctx.SnapshotProvider, ctx.TaskMetricsService);
            var original = ctx.Service.GetUserProfile().SchedulingMode;

            handler.Execute(ctx.Service, new[] { "not-a-mode" });

            Assert.Equal(original, ctx.Service.GetUserProfile().SchedulingMode);
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

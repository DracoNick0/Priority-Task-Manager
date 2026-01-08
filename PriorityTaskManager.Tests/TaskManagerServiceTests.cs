using Xunit;
using PriorityTaskManager.Models;
using PriorityTaskManager.Services;
using PriorityTaskManager.MCP;

namespace PriorityTaskManager.Tests
{
    public class TaskManagerServiceTests
    {
        // Base class for TaskManagerService tests to share setup logic
        public abstract class TaskManagerServiceTestBase
        {
            protected readonly TaskManagerService _TMS;
            protected readonly MockPersistenceService _persistenceService;
            protected readonly MockTimeService _timeService;

            protected TaskManagerServiceTestBase()
            {
                _persistenceService = new MockPersistenceService();
                _timeService = new MockTimeService();
                var initialData = _persistenceService.LoadData();
                var urgencyStrategy = new MultiAgentUrgencyStrategy(initialData.UserProfile, initialData.Events, _timeService);
                _TMS = new TaskManagerService(urgencyStrategy, _persistenceService, initialData);
            }
        }

        public class GeneralTests : TaskManagerServiceTestBase
        {
            /*
             * === Test Coverage for General Service Behavior ===
             * [✓] Constructor: Creates a default 'General' list if none exist.
             * [✓] GetTaskCount: Returns the correct total number of tasks.
             * [✓] GetAllTasks (no args): Returns all tasks from all lists.
            */

            [Fact]
            public void Constructor_ShouldCreateDefaultList_WhenNoneExist()
            {
                // Arrange
                var persistenceService = new MockPersistenceService();
                persistenceService.Data.Lists.Clear(); // Ensure no lists exist
                persistenceService.Data.NextListId = 1;
                var timeService = new MockTimeService();
                var urgencyStrategy = new MultiAgentUrgencyStrategy(persistenceService.Data.UserProfile, persistenceService.Data.Events, timeService);

                // Act
                var tms = new TaskManagerService(urgencyStrategy, persistenceService, persistenceService.Data);
                var lists = tms.GetAllLists();

                // Assert
                Assert.Single(lists);
                Assert.Equal("General", lists.First().Name);
                Assert.Equal(1, lists.First().Id);
            }

            [Fact]
            public void GetAllTasks_NoArgument_ShouldReturnAllTasksFromAllLists()
            {
                // Arrange
                var list1 = new TaskList { Name = "List One" };
                _TMS.AddList(list1);
                var list2 = new TaskList { Name = "List Two" };
                _TMS.AddList(list2);

                _TMS.AddTask(new TaskItem { Title = "Task 1", ListId = 1 }); // Default "General" list
                _TMS.AddTask(new TaskItem { Title = "Task 2", ListId = list1.Id });
                _TMS.AddTask(new TaskItem { Title = "Task 3", ListId = list2.Id });

                // Act
                var allTasks = _TMS.GetAllTasks();

                // Assert
                Assert.Equal(3, allTasks.Count);
            }

            [Fact]
            public void GetTaskCount_ShouldReturnCorrectNumberOfTasks()
            {
                // Arrange
                Assert.Equal(0, _TMS.GetTaskCount()); // Initial count
                _TMS.AddTask(new TaskItem { Title = "Task 1", ListId = 1 });
                _TMS.AddTask(new TaskItem { Title = "Task 2", ListId = 1 });

                // Act
                var count = _TMS.GetTaskCount();

                // Assert
                Assert.Equal(2, count);
            }
        }

        public class TaskManagementTests : TaskManagerServiceTestBase
        {
            /*
             * === Test Coverage for Task CRUD and Status ===
             * [✓] AddTask: Happy path (valid title).
             * [✓] AddTask: Error condition (empty title).
             * [✓] GetTaskById: Happy path (task exists).
             * [✓] GetTaskById: Edge case (task does not exist).
             * [✓] GetTaskByDisplayId: Happy path (task exists).
             * [✓] UpdateTask: Happy path (task exists).
             * [✓] UpdateTask: Error condition (empty title).
             * [✓] UpdateTask: Edge case (task does not exist).
             * [✓] DeleteTask: Happy path (task exists).
             * [✓] DeleteTask: Edge case (task does not exist).
             * [✓] DeleteTasks (Bulk): Happy path.
             * [✓] MarkTaskAsComplete: Happy path.
             * [✓] MarkTaskAsIncomplete: Happy path.
            */

            [Fact]
            public void AddTask_WithValidTitle_ShouldAddTaskToContainer()
            {
                // Arrange
                var task = new TaskItem { Title = "Test Task", ListId = 1 };

                // Act
                _TMS.AddTask(task);
                var allTasks = _TMS.GetAllTasks(1);

                // Assert
                Assert.Single(allTasks);
                Assert.Equal("Test Task", allTasks.First().Title);
            }

            [Fact]
            public void AddTask_WithEmptyTitle_ShouldThrowArgumentException()
            {
                // Arrange
                var task = new TaskItem { Title = "", ListId = 1 };

                // Act & Assert
                Assert.Throws<ArgumentException>(() => _TMS.AddTask(task));
            }

            [Fact]
            public void GetTaskById_WhenTaskExists_ShouldReturnTask()
            {
                // Arrange
                var task = new TaskItem { Title = "Test Task", ListId = 1 };
                _TMS.AddTask(task); // Adds the task and assigns an ID

                // Act
                var retrievedTask = _TMS.GetTaskById(task.Id);

                // Assert
                Assert.NotNull(retrievedTask);
                Assert.Equal(task.Id, retrievedTask.Id);
            }

            [Fact]
            public void GetTaskById_WhenTaskDoesNotExist_ShouldReturnNull()
            {
                // Act
                var retrievedTask = _TMS.GetTaskById(999);

                // Assert
                Assert.Null(retrievedTask);
            }

            [Fact]
            public void UpdateTask_WhenTaskExists_ShouldUpdateDetails()
            {
                // Arrange
                var task = new TaskItem { Title = "Original Title", ListId = 1 };
                _TMS.AddTask(task);
                var updatedTask = task.Clone();
                updatedTask.Title = "Updated Title";

                // Act
                var result = _TMS.UpdateTask(updatedTask);
                var retrievedTask = _TMS.GetTaskById(task.Id);

                // Assert
                Assert.True(result);
                Assert.NotNull(retrievedTask);
                Assert.Equal("Updated Title", retrievedTask.Title);
            }

            [Fact]
            public void UpdateTask_WhenTaskDoesNotExist_ShouldReturnFalse()
            {
                // Arrange
                var task = new TaskItem { Id = 999, Title = "Non-existent task" };

                // Act
                var result = _TMS.UpdateTask(task);

                // Assert
                Assert.False(result);
            }

            [Fact]
            public void DeleteTask_WhenTaskExists_ShouldRemoveTask()
            {
                // Arrange
                var task = new TaskItem { Title = "Task to delete", ListId = 1 };
                _TMS.AddTask(task);
                var taskId = task.Id;
                Assert.Single(_TMS.GetAllTasks(1)); // Pre-condition check

                // Act
                var result = _TMS.DeleteTask(taskId);
                var allTasks = _TMS.GetAllTasks(1);

                // Assert
                Assert.True(result);
                Assert.Empty(allTasks);
            }

            [Fact]
            public void DeleteTask_WhenTaskDoesNotExist_ShouldReturnFalse()
            {
                // Act
                var result = _TMS.DeleteTask(999);

                // Assert
                Assert.False(result);
            }

            [Fact]
            public void MarkTaskAsComplete_WhenTaskExists_ShouldSetIsCompletedToTrue()
            {
                // Arrange
                var task = new TaskItem { Title = "Test", ListId = 1, IsCompleted = false };
                _TMS.AddTask(task);

                // Act
                var result = _TMS.MarkTaskAsComplete(task.Id);
                var retrievedTask = _TMS.GetTaskById(task.Id);

                // Assert
                Assert.True(result);
                Assert.NotNull(retrievedTask);
                Assert.True(retrievedTask.IsCompleted);
            }

            [Fact]
            public void MarkTaskAsIncomplete_WhenTaskExists_ShouldSetIsCompletedToFalse()
            {
                // Arrange
                var task = new TaskItem { Title = "Test", ListId = 1, IsCompleted = true };
                _TMS.AddTask(task);

                // Act
                var result = _TMS.MarkTaskAsIncomplete(task.Id);
                var retrievedTask = _TMS.GetTaskById(task.Id);

                // Assert
                Assert.True(result);
                Assert.NotNull(retrievedTask);
                Assert.False(retrievedTask.IsCompleted);
            }

            [Fact]
            public void DeleteTasks_WithMultipleTasks_ShouldRemoveOnlySpecifiedTasks()
            {
                // Arrange
                var task1 = new TaskItem { Title = "Task 1", ListId = 1 };
                var task2 = new TaskItem { Title = "Task 2", ListId = 1 };
                var task3 = new TaskItem { Title = "Task 3", ListId = 1 };
                _TMS.AddTask(task1);
                _TMS.AddTask(task2);
                _TMS.AddTask(task3);

                var tasksToDelete = new List<TaskItem> { task1, task3 };

                // Act
                _TMS.DeleteTasks(tasksToDelete);
                var remainingTasks = _TMS.GetAllTasks(1).ToList();

                // Assert
                Assert.Single(remainingTasks);
                Assert.Equal(task2.Id, remainingTasks.First().Id);
            }

            [Fact]
            public void GetTaskByDisplayId_WhenTaskExists_ShouldReturnCorrectTask()
            {
                // Arrange
                var list1 = new TaskList { Name = "List 1" };
                _TMS.AddList(list1);
                var task1 = new TaskItem { Title = "Task 1", ListId = list1.Id };
                _TMS.AddTask(task1); // DisplayId should be 1

                var list2 = new TaskList { Name = "List 2" };
                _TMS.AddList(list2);
                var task2 = new TaskItem { Title = "Task 2", ListId = list2.Id };
                _TMS.AddTask(task2); // DisplayId should be 2

                // Act
                var retrievedTask = _TMS.GetTaskByDisplayId(task1.DisplayId, list1.Id);

                // Assert
                Assert.NotNull(retrievedTask);
                Assert.Equal(task1.Id, retrievedTask.Id);
                Assert.Equal(task1.DisplayId, retrievedTask.DisplayId);
            }

            [Fact]
            public void UpdateTask_WithEmptyTitle_ShouldThrowArgumentException()
            {
                // Arrange
                var task = new TaskItem { Title = "A valid title", ListId = 1 };
                _TMS.AddTask(task);

                // Act & Assert
                task.Title = " ";
                Assert.Throws<ArgumentException>(() => _TMS.UpdateTask(task));
            }
        }

        public class ListManagementTests : TaskManagerServiceTestBase
        {
            /*
             * === Test Coverage for List Management ===
             * [✓] AddList: Happy path (unique name).
             * [✓] AddList: Error condition (duplicate name).
             * [✓] DeleteList: Happy path (removes list and its tasks).
             * [✓] UpdateList: Happy path (updates sort option).
             * [✓] SetActiveListId: Happy path (list exists).
             * [✓] SetActiveListId: Error condition (list does not exist).
            */

            [Fact]
            public void AddList_WithUniqueName_ShouldAddList()
            {
                // Arrange
                var list = new TaskList { Name = "New List" };

                // Act
                _TMS.AddList(list);
                var allLists = _TMS.GetAllLists();

                // Assert
                Assert.Contains(allLists, l => l.Name == "New List");
            }

            [Fact]
            public void AddList_WithDuplicateName_ShouldThrowInvalidOperationException()
            {
                // Arrange
                var list1 = new TaskList { Name = "Duplicate List" };
                var list2 = new TaskList { Name = "Duplicate List" };
                _TMS.AddList(list1);

                // Act & Assert
                Assert.Throws<InvalidOperationException>(() => _TMS.AddList(list2));
            }

            [Fact]
            public void DeleteList_WhenListExists_ShouldRemoveListAndAssociatedTasks()
            {
                // Arrange
                var list = new TaskList { Name = "List to Delete" };
                _TMS.AddList(list);
                var task = new TaskItem { Title = "Task in list", ListId = list.Id };
                _TMS.AddTask(task);

                Assert.Single(_TMS.GetAllTasks(list.Id)); // Pre-condition

                // Act
                _TMS.DeleteList(list.Name);
                var deletedList = _TMS.GetListByName(list.Name);
                var tasksInDeletedList = _TMS.GetAllTasks(list.Id);

                // Assert
                Assert.Null(deletedList);
                Assert.Empty(tasksInDeletedList);
            }

            [Fact]
            public void UpdateList_WhenListExists_ShouldUpdateSortOption()
            {
                // Arrange
                var list = new TaskList { Name = "Sortable List", SortOption = SortOption.Default };
                _TMS.AddList(list);

                // Act
                list.SortOption = SortOption.DueDate;
                _TMS.UpdateList(list);
                var updatedList = _TMS.GetListByName("Sortable List");

                // Assert
                Assert.NotNull(updatedList);
                Assert.Equal(SortOption.DueDate, updatedList.SortOption);
            }

            [Fact]
            public void SetActiveListId_WhenListExists_ShouldUpdateActiveList()
            {
                // Arrange
                var list = new TaskList { Name = "New Active List" };
                _TMS.AddList(list); // This will assign an ID

                // Act
                _TMS.SetActiveListId(list.Id);
                var activeId = _TMS.GetActiveListId();

                // Assert
                Assert.Equal(list.Id, activeId);
            }

            [Fact]
            public void SetActiveListId_WhenListDoesNotExist_ShouldThrowArgumentException()
            {
                // Act & Assert
                Assert.Throws<ArgumentException>(() => _TMS.SetActiveListId(999));
            }
        }

        public class EventManagementTests : TaskManagerServiceTestBase
        {
            /*
             * === Test Coverage for Event Management ===
             * [✓] AddEvent: Happy path.
             * [✓] DeleteEvent: Happy path.
             * [✓] UpdateEvent: Happy path.
            */

            [Fact]
            public void AddEvent_WithValidData_ShouldAddEvent()
            {
                // Arrange
                var newEvent = new Event { Name = "Team Meeting", StartTime = DateTime.Now, EndTime = DateTime.Now.AddHours(1) };

                // Act
                _TMS.AddEvent(newEvent);
                var allEvents = _TMS.GetAllEvents();

                // Assert
                Assert.Single(allEvents);
                Assert.Equal("Team Meeting", allEvents.First().Name);
            }

            [Fact]
            public void DeleteEvent_WhenEventExists_ShouldRemoveEvent()
            {
                // Arrange
                var newEvent = new Event { Name = "Event to delete" };
                _TMS.AddEvent(newEvent);
                var eventId = newEvent.Id;
                Assert.Single(_TMS.GetAllEvents());

                // Act
                var result = _TMS.DeleteEvent(eventId);

                // Assert
                Assert.True(result);
                Assert.Empty(_TMS.GetAllEvents());
            }

            [Fact]
            public void UpdateEvent_WhenEventExists_ShouldUpdateEventDetails()
            {
                // Arrange
                var newEvent = new Event { Name = "Old Name", StartTime = DateTime.Now };
                _TMS.AddEvent(newEvent);

                // Act
                newEvent.Name = "New Name";
                _TMS.UpdateEvent(newEvent);
                var updatedEvent = _TMS.GetEvent(newEvent.Id);

                // Assert
                Assert.NotNull(updatedEvent);
                Assert.Equal("New Name", updatedEvent.Name);
            }
        }

        public class DependencyManagementTests : TaskManagerServiceTestBase
        {
            /*
             * === Test Coverage for Dependency Management ===
             * [✓] UpdateTask: Error condition (direct circular dependency).
             * [✓] UpdateTask: Error condition (transitive circular dependency).
             * [✓] UpdateTask: Happy path (valid dependency).
            */

            [Fact]
            public void UpdateTask_WithDirectCircularDependency_ShouldThrowInvalidOperationException()
            {
                // Arrange
                var taskA = new TaskItem { Title = "Task A", ListId = 1 };
                var taskB = new TaskItem { Title = "Task B", ListId = 1 };
                _TMS.AddTask(taskA);
                _TMS.AddTask(taskB);

                // Create dependency B -> A
                taskB.Dependencies.Add(taskA.Id);
                _TMS.UpdateTask(taskB);

                // Act & Assert: Try to create dependency A -> B, which should fail
                taskA.Dependencies.Add(taskB.Id);
                Assert.Throws<InvalidOperationException>(() => _TMS.UpdateTask(taskA));
            }

            [Fact]
            public void UpdateTask_WithTransitiveCircularDependency_ShouldThrowInvalidOperationException()
            {
                // Arrange
                var taskA = new TaskItem { Title = "Task A", ListId = 1 };
                var taskB = new TaskItem { Title = "Task B", ListId = 1 };
                var taskC = new TaskItem { Title = "Task C", ListId = 1 };
                _TMS.AddTask(taskA);
                _TMS.AddTask(taskB);
                _TMS.AddTask(taskC);

                // Create chain C -> B -> A
                taskB.Dependencies.Add(taskA.Id);
                _TMS.UpdateTask(taskB);
                taskC.Dependencies.Add(taskB.Id);
                _TMS.UpdateTask(taskC);

                // Act & Assert: Try to create dependency A -> C, which completes the cycle and should fail
                taskA.Dependencies.Add(taskC.Id);
                Assert.Throws<InvalidOperationException>(() => _TMS.UpdateTask(taskA));
            }

            [Fact]
            public void UpdateTask_WithValidDependency_ShouldSucceed()
            {
                // Arrange
                var taskA = new TaskItem { Title = "Task A", ListId = 1 };
                var taskB = new TaskItem { Title = "Task B", ListId = 1 };
                _TMS.AddTask(taskA);
                _TMS.AddTask(taskB);

                // Act
                taskB.Dependencies.Add(taskA.Id);
                var exception = Record.Exception(() => _TMS.UpdateTask(taskB));

                // Assert
                Assert.Null(exception);
                var retrievedTaskB = _TMS.GetTaskById(taskB.Id);
                Assert.NotNull(retrievedTaskB);
                Assert.Contains(taskA.Id, retrievedTaskB.Dependencies);
            }
        }

        public class UserProfileTests : TaskManagerServiceTestBase
        {
            /*
             * === Test Coverage for User Profile ===
             * [✓] UpdateUserProfile: Happy path (persists changes).
            */

            [Fact]
            public void UpdateUserProfile_ShouldPersistChanges()
            {
                // Arrange
                var originalProfile = _TMS.GetUserProfile();
                var newStartTime = new TimeOnly(8, 30);
                Assert.NotEqual(newStartTime, originalProfile.WorkStartTime); // Pre-condition

                // Act
                var profileToUpdate = originalProfile;
                profileToUpdate.WorkStartTime = newStartTime;
                _TMS.UpdateUserProfile(profileToUpdate);
                var updatedProfile = _TMS.GetUserProfile();

                // Assert
                Assert.Equal(newStartTime, updatedProfile.WorkStartTime);
            }
        }
    }
}

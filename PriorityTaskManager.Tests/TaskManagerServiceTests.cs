using System;
using System.IO;
using PriorityTaskManager.Models;
using PriorityTaskManager.Services;
using Xunit;

namespace PriorityTaskManager.Tests
{
    /// <summary>
    /// Contains all unit tests for the TaskManagerService class.
    /// </summary>
    public class TaskFunctionalityTests : IDisposable
    {
        private readonly TaskManagerService _service;
        private readonly string _uniqueTestId = Guid.NewGuid().ToString();
        private string TestTasksFile => $"test_tasks_{_uniqueTestId}.json";
        private string TestListsFile => $"test_lists_{_uniqueTestId}.json";

        public TaskFunctionalityTests()
        {
            File.Delete(TestTasksFile);
            File.Delete(TestListsFile);
            _service = new TaskManagerService(new SingleAgentStrategy(), TestTasksFile, TestListsFile);
        }

        public void Dispose()
        {
            File.Delete(TestTasksFile);
            File.Delete(TestListsFile);
        }

        /// <summary>
        /// Verifies that CalculateUrgency sets the urgency score to 0 for completed tasks.
        /// </summary>
        [Fact]
        public void CalculateUrgency_ShouldBeZero_ForCompletedTask()
        {
            var task = new TaskItem { Title = "Done", EstimatedDuration = TimeSpan.FromHours(2), Progress = 1.0, DueDate = DateTime.Today.AddDays(1), IsCompleted = true, ListName = "General" };
            _service.AddTask(task);
            _service.CalculateUrgencyForAllTasks();
            Assert.Equal(0, task.UrgencyScore);
        }
        
        /// <summary>
        /// Verifies that CalculateUrgency prioritizes the first task in a dependency chain.
        /// </summary>
        [Fact]
        public void CalculateUrgency_ShouldPrioritizeFirstTaskInDependencyChain()
        {
            var taskA = new TaskItem { Title = "A", EstimatedDuration = TimeSpan.FromHours(10), Progress = 0.0, DueDate = DateTime.Today.AddDays(10), ListName = "General" };
            var taskB = new TaskItem { Title = "B", EstimatedDuration = TimeSpan.FromHours(5), Progress = 0.0, DueDate = DateTime.Today.AddDays(10), ListName = "General" };
            var taskC = new TaskItem { Title = "C", EstimatedDuration = TimeSpan.FromHours(2), Progress = 0.0, DueDate = DateTime.Today.AddDays(10), ListName = "General" };
            _service.AddTask(taskA);
            _service.AddTask(taskB);
            _service.AddTask(taskC);
            taskB.Dependencies.Add(taskA.Id);
            taskC.Dependencies.Add(taskB.Id);
            _service.CalculateUrgencyForAllTasks();
            Assert.True(taskA.UrgencyScore > taskB.UrgencyScore);
            Assert.True(taskB.UrgencyScore > taskC.UrgencyScore);
        }
        
        /// <summary>
        /// Verifies that adding a task increases the task count.
        /// </summary>
        [Fact]
        public void AddTask_ShouldIncreaseTaskCount()
        {
            // Ensure a clean state for this test
            // Remove all existing tasks
            foreach (var existing in _service.GetAllTasks(1)) // Specify the default list ID
            {
                _service.DeleteTask(existing.Id);
            }
            var task = new TaskItem
            {
                Title = "Test Task",
                Description = "Test Description",
                Importance = 8,
                DueDate = DateTime.Now.AddDays(1),
                IsCompleted = false,
                ListId = 1 // Assign the default list ID
            };

            _service.AddTask(task);

            Assert.Single(_service.GetAllTasks(1)); // Verify against the default list ID
        }

        /// <summary>
        /// Verifies that GetTaskById returns the correct task when it exists.
        /// </summary>
        [Fact]
        public void GetTaskById_ShouldReturnCorrectTask_WhenTaskExists()
        {
            var task = new TaskItem { Title = "A", Description = "B", Importance = 2, DueDate = DateTime.Now, IsCompleted = false, ListId = 1 };
            _service.AddTask(task);
            var found = _service.GetTaskById(task.Id);
            Assert.NotNull(found);
            Assert.Equal(task.Title, found.Title);
        }

        /// <summary>
        /// Verifies that GetTaskById returns null when the requested task ID does not exist.
        /// </summary>
        [Fact]
        public void GetTaskById_ShouldReturnNull_WhenTaskDoesNotExist()
        {
            var result = _service.GetTaskById(999);
            Assert.Null(result);
        }

        /// <summary>
        /// Verifies that UpdateTask changes the properties of a task when it exists.
        /// </summary>
        [Fact]
        public void UpdateTask_ShouldChangeTaskProperties_WhenTaskExists()
        {
            var task = new TaskItem { Title = "Old", Description = "Old", Importance = 2, DueDate = DateTime.Now, IsCompleted = false };
            _service.AddTask(task);
            var updated = new TaskItem { Id = task.Id, Title = "New", Description = "New", Importance = 9, DueDate = DateTime.Now.AddDays(1), IsCompleted = true };
            var result = _service.UpdateTask(updated);
            Assert.True(result);
            var found = _service.GetTaskById(task.Id);
            Assert.NotNull(found);
            Assert.Equal("New", found.Title);
            Assert.Equal(9, found.Importance);
        }

        /// <summary>
        /// Verifies that DeleteTask removes a task from the list when it exists.
        /// </summary>
        [Fact]
        public void DeleteTask_ShouldRemoveTaskFromList_WhenTaskExists()
        {
            var task = new TaskItem { Title = "Delete", Description = "Delete", Importance = 5, DueDate = DateTime.Now, IsCompleted = false };
            _service.AddTask(task);
            var result = _service.DeleteTask(task.Id);
            Assert.True(result);
            Assert.Null(_service.GetTaskById(task.Id));
        }

        /// <summary>
        /// Verifies that MarkTaskAsComplete sets the IsCompleted property to true for a task when it exists.
        /// </summary>
        [Fact]
        public void MarkTaskAsComplete_ShouldSetIsCompletedToTrue_WhenTaskExists()
        {
            var task = new TaskItem { Title = "Complete", Description = "Complete", Importance = 7, DueDate = DateTime.Now, IsCompleted = false };
            _service.AddTask(task);
            var result = _service.MarkTaskAsComplete(task.Id);
            Assert.True(result);
            var found = _service.GetTaskById(task.Id);
            Assert.NotNull(found);
            Assert.True(found.IsCompleted);
        }

        /// <summary>
        /// Verifies that MarkTaskAsIncomplete sets the IsCompleted property to false for a task when it exists.
        /// </summary>
        [Fact]
        public void MarkTaskAsIncomplete_ShouldSetIsCompletedToFalse_WhenTaskExists()
        {
            // Arrange
            var task = new TaskItem { Title = "Test Task", IsCompleted = true };
            _service.AddTask(task);

            // Act
            var result = _service.MarkTaskAsIncomplete(task.Id);

            // Assert
            Assert.True(result);
            Assert.False(_service.GetTaskById(task.Id)?.IsCompleted);
        }
        
        /// <summary>
        /// Verifies that AddTask throws ArgumentException when the title is null, empty, or whitespace.
        /// </summary>
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void AddTask_ShouldThrowArgumentException_WhenTitleIsEmpty(string invalidTitle)
        {
            // The following should throw ArgumentException due to invalid title
            Assert.Throws<ArgumentException>(() =>
            {
                var task = new TaskItem { Title = invalidTitle, Description = "desc", Importance = 5, DueDate = DateTime.Now };
                _service.AddTask(task);
            });
        }
    
        /// <summary>
        /// Verifies that UpdateTask throws ArgumentException when the title is null, empty, or whitespace.
        /// </summary>
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void UpdateTask_ShouldThrowArgumentException_WhenTitleIsEmpty(string invalidTitle)
        {
            var validTask = new TaskItem { Title = "Valid", Description = "desc", Importance = 5, DueDate = DateTime.Now };
            _service.AddTask(validTask);
            // The following should throw ArgumentException due to invalid title
            Assert.Throws<ArgumentException>(() =>
            {
                var updatedTask = new TaskItem { Id = validTask.Id, Title = invalidTitle, Description = "desc", Importance = 5, DueDate = DateTime.Now };
                _service.UpdateTask(updatedTask);
            });
        }

        /// <summary>
        /// Verifies that UpdateTask throws InvalidOperationException when a direct circular dependency is created.
        /// </summary>
        [Fact]
        public void UpdateTask_ShouldThrowInvalidOperationException_WhenDirectCircularDependencyIsCreated()
        {
            var taskA = new TaskItem { Title = "A", Importance = 1, DueDate = DateTime.Now };
            var taskB = new TaskItem { Title = "B", Importance = 1, DueDate = DateTime.Now };
            _service.AddTask(taskA);
            _service.AddTask(taskB);

            // Make B depend on A
            taskB.Dependencies.Add(taskA.Id);
            _service.UpdateTask(taskB);

            // Attempt to make A depend on B (should throw)
            taskA.Dependencies.Add(taskB.Id);
            Assert.Throws<InvalidOperationException>(() => _service.UpdateTask(taskA));
        }

        /// <summary>
        /// Verifies that UpdateTask throws InvalidOperationException when a transitive circular dependency is created.
        /// </summary>
        [Fact]
        public void UpdateTask_ShouldThrowInvalidOperationException_WhenTransitiveCircularDependencyIsCreated()
        {
            var taskA = new TaskItem { Title = "A", Importance = 1, DueDate = DateTime.Now };
            var taskB = new TaskItem { Title = "B", Importance = 1, DueDate = DateTime.Now };
            var taskC = new TaskItem { Title = "C", Importance = 1, DueDate = DateTime.Now };
            _service.AddTask(taskA);
            _service.AddTask(taskB);
            _service.AddTask(taskC);

            // Create dependency chain: C -> B -> A
            taskC.Dependencies.Add(taskB.Id);
            _service.UpdateTask(taskC);
            taskB.Dependencies.Add(taskA.Id);
            _service.UpdateTask(taskB);

            // Attempt to make A depend on C (should throw)
            taskA.Dependencies.Add(taskC.Id);
            Assert.Throws<InvalidOperationException>(() => _service.UpdateTask(taskA));
        }
    }
}

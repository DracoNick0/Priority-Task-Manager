using System;
using PriorityTaskManager.Models;
using PriorityTaskManager.Services;
using Xunit;

namespace PriorityTaskManager.Tests
{
    /// <summary>
    /// Contains all unit tests for the TaskManagerService class.
    /// </summary>
    public class TaskManagerServiceTests
    {
        /// <summary>
        /// Verifies that CalculateUrgency sets the urgency score to 0 for completed tasks.
        /// </summary>
        [Fact]
        public void CalculateUrgency_ShouldBeZero_ForCompletedTask()
        {
            var service = new TaskManagerService();
            var task = new TaskItem { Title = "Done", EstimatedDuration = TimeSpan.FromHours(2), Progress = 1.0, DueDate = DateTime.Today.AddDays(1), IsCompleted = true };
            service.AddTask(task);
            service.CalculateUrgencyForAllTasks();
            Assert.Equal(0, task.UrgencyScore);
        }
        
        /// <summary>
        /// Verifies that CalculateUrgency prioritizes the first task in a dependency chain.
        /// </summary>
        [Fact]
        public void CalculateUrgency_ShouldPrioritizeFirstTaskInDependencyChain()
        {
            var service = new TaskManagerService();
            var taskA = new TaskItem { Title = "A", EstimatedDuration = TimeSpan.FromHours(10), Progress = 0.0, DueDate = DateTime.Today.AddDays(10) };
            var taskB = new TaskItem { Title = "B", EstimatedDuration = TimeSpan.FromHours(5), Progress = 0.0, DueDate = DateTime.Today.AddDays(10) };
            var taskC = new TaskItem { Title = "C", EstimatedDuration = TimeSpan.FromHours(2), Progress = 0.0, DueDate = DateTime.Today.AddDays(10) };
            service.AddTask(taskA);
            service.AddTask(taskB);
            service.AddTask(taskC);
            taskB.Dependencies.Add(taskA.Id);
            taskC.Dependencies.Add(taskB.Id);
            service.CalculateUrgencyForAllTasks();
            Assert.True(taskA.UrgencyScore > taskB.UrgencyScore);
            Assert.True(taskB.UrgencyScore > taskC.UrgencyScore);
        }
        
        /// <summary>
        /// Verifies that adding a task increases the task count.
        /// </summary>
        [Fact]
        public void AddTask_ShouldIncreaseTaskCount()
        {
            var service = new TaskManagerService();
            // Ensure a clean state for this test
            // Remove all existing tasks
            foreach (var existing in service.GetAllTasks())
            {
                service.DeleteTask(existing.Id);
            }
            var task = new TaskItem
            {
                Title = "Test Task",
                Description = "Test Description",
                Importance = 8,
                DueDate = DateTime.Now.AddDays(1),
                IsCompleted = false
            };

            service.AddTask(task);

            Assert.Equal(1, service.GetTaskCount());
        }

        /// <summary>
        /// Verifies that GetTaskById returns the correct task when it exists.
        /// </summary>
        [Fact]
        public void GetTaskById_ShouldReturnCorrectTask_WhenTaskExists()
        {
            var service = new TaskManagerService();
            var task = new TaskItem { Title = "A", Description = "B", Importance = 2, DueDate = DateTime.Now, IsCompleted = false };
            service.AddTask(task);
            var found = service.GetTaskById(task.Id);
            Assert.NotNull(found);
            Assert.Equal(task.Title, found.Title);
        }

        /// <summary>
        /// Verifies that GetTaskById returns null when the requested task ID does not exist.
        /// </summary>
        [Fact]
        public void GetTaskById_ShouldReturnNull_WhenTaskDoesNotExist()
        {
            var service = new TaskManagerService();
            var result = service.GetTaskById(999);
            Assert.Null(result);
        }

        /// <summary>
        /// Verifies that UpdateTask changes the properties of a task when it exists.
        /// </summary>
        [Fact]
        public void UpdateTask_ShouldChangeTaskProperties_WhenTaskExists()
        {
            var service = new TaskManagerService();
            var task = new TaskItem { Title = "Old", Description = "Old", Importance = 2, DueDate = DateTime.Now, IsCompleted = false };
            service.AddTask(task);
            var updated = new TaskItem { Id = task.Id, Title = "New", Description = "New", Importance = 9, DueDate = DateTime.Now.AddDays(1), IsCompleted = true };
            var result = service.UpdateTask(updated);
            Assert.True(result);
            var found = service.GetTaskById(task.Id);
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
            var service = new TaskManagerService();
            var task = new TaskItem { Title = "Delete", Description = "Delete", Importance = 5, DueDate = DateTime.Now, IsCompleted = false };
            service.AddTask(task);
            var result = service.DeleteTask(task.Id);
            Assert.True(result);
            Assert.Null(service.GetTaskById(task.Id));
        }

        /// <summary>
        /// Verifies that MarkTaskAsComplete sets the IsCompleted property to true for a task when it exists.
        /// </summary>
        [Fact]
        public void MarkTaskAsComplete_ShouldSetIsCompletedToTrue_WhenTaskExists()
        {
            var service = new TaskManagerService();
            var task = new TaskItem { Title = "Complete", Description = "Complete", Importance = 7, DueDate = DateTime.Now, IsCompleted = false };
            service.AddTask(task);
            var result = service.MarkTaskAsComplete(task.Id);
            Assert.True(result);
            var found = service.GetTaskById(task.Id);
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
            var service = new TaskManagerService();
            var task = new TaskItem { Title = "Test Task", IsCompleted = true };
            service.AddTask(task);

            // Act
            var result = service.MarkTaskAsIncomplete(task.Id);

            // Assert
            Assert.True(result);
            Assert.False(service.GetTaskById(task.Id)?.IsCompleted);
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
                var service = new TaskManagerService();
                // The following should throw ArgumentException due to invalid title
                Assert.Throws<ArgumentException>(() =>
                {
                    var task = new TaskItem { Title = invalidTitle, Description = "desc", Importance = 5, DueDate = DateTime.Now };
                    service.AddTask(task);
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
                var service = new TaskManagerService();
                var validTask = new TaskItem { Title = "Valid", Description = "desc", Importance = 5, DueDate = DateTime.Now };
                service.AddTask(validTask);
                // The following should throw ArgumentException due to invalid title
                Assert.Throws<ArgumentException>(() =>
                {
                    var updatedTask = new TaskItem { Id = validTask.Id, Title = invalidTitle, Description = "desc", Importance = 5, DueDate = DateTime.Now };
                    service.UpdateTask(updatedTask);
                });
            }
    }
}

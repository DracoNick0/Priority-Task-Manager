using System;
using PriorityTaskManager.Models;
using PriorityTaskManager.Services;
using Xunit;

namespace PriorityTaskManager.Tests
{
    public class TaskManagerServiceTests
    {
        [Fact]
        public void CalculateUrgency_ShouldBeZero_ForCompletedTask()
        {
            // Arrange
            var service = new TaskManagerService();
            var task = new TaskItem { Title = "Done", EstimatedDuration = TimeSpan.FromHours(2), Progress = 1.0, DueDate = DateTime.Today.AddDays(1), IsCompleted = true };

            // Act
            service.AddTask(task);
            service.CalculateUrgencyForAllTasks();

            // Assert
            Assert.Equal(0, task.UrgencyScore);
        }

        [Fact]
        public void CalculateUrgency_ShouldPrioritizeFirstTaskInDependencyChain()
        {
            // Arrange
            var service = new TaskManagerService();
            var taskA = new TaskItem { Title = "A", EstimatedDuration = TimeSpan.FromHours(10), Progress = 0.0, DueDate = DateTime.Today.AddDays(10) };
            var taskB = new TaskItem { Title = "B", EstimatedDuration = TimeSpan.FromHours(5), Progress = 0.0, DueDate = DateTime.Today.AddDays(10) };
            var taskC = new TaskItem { Title = "C", EstimatedDuration = TimeSpan.FromHours(2), Progress = 0.0, DueDate = DateTime.Today.AddDays(10) };

            // Act
            service.AddTask(taskA);
            service.AddTask(taskB);
            service.AddTask(taskC);
            taskB.Dependencies.Add(taskA.Id);
            taskC.Dependencies.Add(taskB.Id);
            service.CalculateUrgencyForAllTasks();

            // Assert
            Assert.True(taskA.UrgencyScore > taskB.UrgencyScore);
            Assert.True(taskB.UrgencyScore > taskC.UrgencyScore);
        }

        [Fact]
        public void AddTask_ShouldIncreaseTaskCount()
        {
            // Arrange
            var service = new TaskManagerService();
            var task = new TaskItem
            {
                Title = "Test Task",
                Description = "Test Description",
                Importance = 8,
                DueDate = DateTime.Now.AddDays(1),
                IsCompleted = false
            };

            // Act
            service.AddTask(task);

            // Assert
            Assert.Equal(1, service.GetTaskCount());
        }

        [Fact]
        public void GetTaskById_ShouldReturnCorrectTask_WhenTaskExists()
        {
            // Arrange
            var service = new TaskManagerService();
            var task = new TaskItem { Title = "A", Description = "B", Importance = 2, DueDate = DateTime.Now, IsCompleted = false };

            // Act
            service.AddTask(task);
            var found = service.GetTaskById(task.Id);

            // Assert
            Assert.NotNull(found);
            Assert.Equal(task.Title, found.Title);
        }

        [Fact]
        public void GetTaskById_ShouldReturnNull_WhenTaskDoesNotExist()
        {
            // Arrange
            var service = new TaskManagerService();

            // Act
            var result = service.GetTaskById(999);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void UpdateTask_ShouldChangeTaskProperties_WhenTaskExists()
        {
            // Arrange
            var service = new TaskManagerService();
            var task = new TaskItem { Title = "Old", Description = "Old", Importance = 2, DueDate = DateTime.Now, IsCompleted = false };
            service.AddTask(task);
            var updated = new TaskItem { Id = task.Id, Title = "New", Description = "New", Importance = 9, DueDate = DateTime.Now.AddDays(1), IsCompleted = true };

            // Act
            var result = service.UpdateTask(updated);
            var found = service.GetTaskById(task.Id);

            // Assert
            Assert.True(result);
            Assert.NotNull(found);
            Assert.Equal("New", found.Title);
            Assert.Equal(9, found.Importance);
        }

        [Fact]
        public void DeleteTask_ShouldRemoveTaskFromList_WhenTaskExists()
        {
            // Arrange
            var service = new TaskManagerService();
            var task = new TaskItem { Title = "Delete", Description = "Delete", Importance = 5, DueDate = DateTime.Now, IsCompleted = false };
            service.AddTask(task);

            // Act
            var result = service.DeleteTask(task.Id);

            // Assert
            Assert.True(result);
            Assert.Null(service.GetTaskById(task.Id));
        }

        [Fact]
        public void MarkTaskAsComplete_ShouldSetIsCompletedToTrue_WhenTaskExists()
        {
            // Arrange
            var service = new TaskManagerService();
            var task = new TaskItem { Title = "Complete", Description = "Complete", Importance = 7, DueDate = DateTime.Now, IsCompleted = false };
            service.AddTask(task);

            // Act
            var result = service.MarkTaskAsComplete(task.Id);
            var found = service.GetTaskById(task.Id);

            // Assert
            Assert.True(result);
            Assert.NotNull(found);
            Assert.True(found.IsCompleted);
        }

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
    }
}

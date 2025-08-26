using System;
using PriorityTaskManager.Models;
using PriorityTaskManager.Services;
using Xunit;

namespace PriorityTaskManager.Tests
{
    public class TaskManagerServiceTests
    {
        [Fact]
        public void AddTask_ShouldIncreaseTaskCount()
        {
            // Arrange
            var service = new TaskManagerService();
            var task = new TaskItem
            {
                Title = "Test Task",
                Description = "Test Description",
                Importance = ImportanceLevel.High,
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
            var service = new TaskManagerService();
            var task = new TaskItem { Title = "A", Description = "B", Importance = ImportanceLevel.Low, DueDate = DateTime.Now, IsCompleted = false };
            service.AddTask(task);
            var found = service.GetTaskById(task.Id);
            Assert.NotNull(found);
            Assert.Equal(task.Title, found.Title);
        }

        [Fact]
        public void GetTaskById_ShouldReturnNull_WhenTaskDoesNotExist()
        {
            var service = new TaskManagerService();
            var result = service.GetTaskById(999);
            Assert.Null(result);
        }

        [Fact]
        public void UpdateTask_ShouldChangeTaskProperties_WhenTaskExists()
        {
            var service = new TaskManagerService();
            var task = new TaskItem { Title = "Old", Description = "Old", Importance = ImportanceLevel.Low, DueDate = DateTime.Now, IsCompleted = false };
            service.AddTask(task);
            var updated = new TaskItem { Id = task.Id, Title = "New", Description = "New", Importance = ImportanceLevel.High, DueDate = DateTime.Now.AddDays(1), IsCompleted = true };
            var result = service.UpdateTask(updated);
            Assert.True(result);
            var found = service.GetTaskById(task.Id);
            Assert.Equal("New", found.Title);
            Assert.Equal(ImportanceLevel.High, found.Importance);
        }

        [Fact]
        public void DeleteTask_ShouldRemoveTaskFromList_WhenTaskExists()
        {
            var service = new TaskManagerService();
            var task = new TaskItem { Title = "Delete", Description = "Delete", Importance = ImportanceLevel.Medium, DueDate = DateTime.Now, IsCompleted = false };
            service.AddTask(task);
            var result = service.DeleteTask(task.Id);
            Assert.True(result);
            Assert.Null(service.GetTaskById(task.Id));
        }
    }
}

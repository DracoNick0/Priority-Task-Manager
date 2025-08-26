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
    }
}

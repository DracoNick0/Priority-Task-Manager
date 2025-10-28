using System;
using System.IO;
using System.Text.Json;
using PriorityTaskManager.Models;
using PriorityTaskManager.Services;
using Xunit;

namespace PriorityTaskManager.Tests
{
    public class TaskModelPersistenceTests
    {
        [Fact]
        public void NewTaskItemProperties_ArePersistedCorrectly()
        {
            // Arrange
            var strategy = new SingleAgentStrategy();
            var mockPersistence = new MockPersistenceService();
            var service = new TaskManagerService(strategy, mockPersistence);

            var now = DateTime.UtcNow;
            var task = new TaskItem
            {
                Title = "Test Advanced Properties",
                IsPinned = true,
                Complexity = 5.0,
                Points = 10.0,
                BeforePadding = TimeSpan.FromMinutes(10),
                AfterPadding = TimeSpan.FromMinutes(5),
                ScheduledStartTime = now,
                ScheduledEndTime = now.AddHours(2)
            };

            // Act
            service.AddTask(task);
            // Simulate reload by creating a new service with the same mockPersistence
            var serviceReloaded = new TaskManagerService(strategy, mockPersistence);
            var loadedTask = serviceReloaded.GetTaskById(1);

            // Assert
            Assert.NotNull(loadedTask);
            Assert.Equal(task.IsPinned, loadedTask.IsPinned);
            Assert.Equal(task.Complexity, loadedTask.Complexity);
            Assert.Equal(task.Points, loadedTask.Points);
            Assert.Equal(task.BeforePadding, loadedTask.BeforePadding);
            Assert.Equal(task.AfterPadding, loadedTask.AfterPadding);
            Assert.Equal(task.ScheduledStartTime, loadedTask.ScheduledStartTime);
            Assert.Equal(task.ScheduledEndTime, loadedTask.ScheduledEndTime);
        }
    }
}

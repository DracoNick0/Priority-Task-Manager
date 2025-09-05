using System;
using System.Collections.Generic;
using PriorityTaskManager.Models;
using PriorityTaskManager.Services;
using Xunit;

namespace PriorityTaskManager.Tests
{
    /// <summary>
    /// Unit tests for the UrgencyService.
    /// </summary>
    public class UrgencyServiceTests
    {
        private readonly UrgencyService _service;

        public UrgencyServiceTests()
        {
            _service = new UrgencyService();
        }

        /// <summary>
        /// Verifies that CalculateUrgency sets the urgency score to 0 for completed tasks.
        /// </summary>
        [Fact]
        public void CalculateUrgency_ShouldBeZero_ForCompletedTask()
        {
            var task = new TaskItem { Title = "Done", EstimatedDuration = TimeSpan.FromHours(2), Progress = 1.0, DueDate = DateTime.Today.AddDays(1), IsCompleted = true, ListName = "General" };
            var tasks = new List<TaskItem> { task };
            _service.CalculateAndApplyUrgency(tasks);
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
            taskB.Dependencies.Add(taskA.Id);
            taskC.Dependencies.Add(taskB.Id);
            var tasks = new List<TaskItem> { taskA, taskB, taskC };
            _service.CalculateAndApplyUrgency(tasks);
            Assert.True(taskA.UrgencyScore > taskB.UrgencyScore);
            Assert.True(taskB.UrgencyScore > taskC.UrgencyScore);
        }
    }
}

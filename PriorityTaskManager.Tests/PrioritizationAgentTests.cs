using System;
using System.Collections.Generic;
using Xunit;
using PriorityTaskManager.Models;
using PriorityTaskManager.MCP;
using PriorityTaskManager.Services.Agents;

namespace PriorityTaskManager.Tests
{
    public class PrioritizationAgentTests
    {
        [Fact]
        public void Act_ShouldScheduleTasks_OrderedByDueDate_WithinContinuousTimeBlock()
        {
            // Arrange
            var taskA = new TaskItem
            {
                Title = "Task A",
                DueDate = DateTime.Today.AddDays(1).AddHours(17), // Tomorrow at 5 PM
                Importance = 10,
                EstimatedDuration = TimeSpan.FromHours(3)
            };
            var taskB = new TaskItem
            {
                Title = "Task B",
                DueDate = DateTime.Today.AddDays(1).AddHours(12), // Tomorrow at 12 PM
                Importance = 5,
                EstimatedDuration = TimeSpan.FromHours(2)
            };
            var tasks = new List<TaskItem> { taskA, taskB };

            var context = new MCPContext();
            context.SharedState["Tasks"] = tasks;
            context.SharedState["TotalAvailableTime"] = TimeSpan.FromHours(40);

            var agent = new PrioritizationAgent();

            // Act
            agent.Act(context);

            // Assert
            Assert.True(context.SharedState.ContainsKey("Tasks"));
            var result = context.SharedState["Tasks"] as List<TaskItem>;
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
            // Should be ordered by DueDate ascending: Task B, Task A
            Assert.Equal("Task B", result[0].Title);
            Assert.Equal("Task A", result[1].Title);
        }
    }
}

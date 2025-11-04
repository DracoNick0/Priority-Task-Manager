using System;
using System.Collections.Generic;
using System.Linq;
using PriorityTaskManager.MCP;
using PriorityTaskManager.Models;
using PriorityTaskManager.Services.Agents;
using Xunit;

namespace PriorityTaskManager.Tests
{
    public class UserContextAgentTests
    {
        [Fact]
        public void Act_ShouldReorderTasks_BasedOnComplexity_WithinSingleDay()
        {
            // Arrange
            var today = DateTime.Today;
            var taskA = new TaskItem
            {
                Id = 1,
                Title = "Task A",
                Complexity = 2,
                ScheduledStartTime = today.AddHours(9),
                ScheduledEndTime = today.AddHours(10)
            };
            var taskB = new TaskItem
            {
                Id = 2,
                Title = "Task B",
                Complexity = 10,
                ScheduledStartTime = today.AddHours(10),
                ScheduledEndTime = today.AddHours(11)
            };
            var tasks = new List<TaskItem> { taskA, taskB };

            var context = new MCPContext();
            context.SharedState["Tasks"] = tasks;
            context.SharedState["UserProfile"] = new UserProfile();

            var agent = new UserContextAgent();

            // Act
            agent.Act(context);

            // Assert
            var resultTasks = context.SharedState["Tasks"] as List<TaskItem>;
            Assert.NotNull(resultTasks);
            Assert.Equal(2, resultTasks.Count);
            // Task B (high complexity) should be first
            Assert.Equal("Task B", resultTasks[0].Title);
            Assert.Equal("Task A", resultTasks[1].Title);
            // Start times should be swapped
            Assert.Equal(today.AddHours(9), resultTasks[0].ScheduledStartTime);
            Assert.Equal(today.AddHours(10), resultTasks[1].ScheduledStartTime);
        }
    }
}

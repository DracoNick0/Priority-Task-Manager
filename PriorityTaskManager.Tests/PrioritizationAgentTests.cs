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
        // Simple helper to translate TimeSpan offset into DateTime for a workday (9 AM - 5 PM)
        private class WorkdayTimeHelper
        {
            private readonly DateTime _workdayStart;
            public WorkdayTimeHelper(DateTime workdayStart)
            {
                _workdayStart = workdayStart.Date.AddHours(9); // 9 AM
            }
            public DateTime OffsetToDateTime(TimeSpan offset)
            {
                return _workdayStart.Add(offset);
            }
        }
        
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

        [Fact]
        public void Act_ShouldRemoveLowerImportanceTask_WhenDueDateIsViolated()
        {
            // Arrange
            var helper = new WorkdayTimeHelper(DateTime.Today);
            var dueDate = helper.OffsetToDateTime(TimeSpan.FromHours(8)); // Today at 5 PM

            var taskA = new TaskItem
            {
                Title = "Task A",
                Importance = 5,
                EstimatedDuration = TimeSpan.FromHours(5),
                DueDate = dueDate
            };
            var taskB = new TaskItem
            {
                Title = "Task B",
                Importance = 10,
                EstimatedDuration = TimeSpan.FromHours(4),
                DueDate = dueDate
            };
            var tasks = new List<TaskItem> { taskA, taskB };

            var context = new MCPContext();
            context.SharedState["Tasks"] = tasks;
            context.SharedState["TotalAvailableTime"] = TimeSpan.FromHours(8);

            var agent = new PrioritizationAgent();

            // Act
            agent.Act(context);

            // Assert
            Assert.True(context.SharedState.ContainsKey("Tasks"));
            var result = context.SharedState["Tasks"] as List<TaskItem>;
            Assert.NotNull(result);
            Assert.Single(result); // Should only schedule the higher importance task
            Assert.Equal("Task B", result[0].Title);
        }
    }
}

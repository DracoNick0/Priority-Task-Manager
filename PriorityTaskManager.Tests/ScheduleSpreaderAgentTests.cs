using System;
using System.Collections.Generic;
using PriorityTaskManager.Models;
using PriorityTaskManager.MCP;
using PriorityTaskManager.Services.Agents;
using Xunit;

namespace PriorityTaskManager.Tests
{
    public class ScheduleSpreaderAgentTests
    {
        [Fact]
        public void Act_ShouldSpaceTasks_BasedOnBreatherDuration()
        {
            // Arrange
            var today = DateTime.Today;
            var userProfile = new UserProfile
            {
                DesiredBreatherDuration = TimeSpan.FromMinutes(15)
            };
            var taskA = new TaskItem
            {
                Title = "Task A",
                ScheduledStartTime = today.AddHours(9),
                ScheduledEndTime = today.AddHours(10),
                DueDate = today.AddYears(1)
            };
            var taskB = new TaskItem
            {
                Title = "Task B",
                ScheduledStartTime = today.AddHours(10),
                ScheduledEndTime = today.AddHours(11),
                DueDate = today.AddYears(1)
            };
            var tasks = new List<TaskItem> { taskA, taskB };

            var context = new MCPContext();
            context.SharedState["UserProfile"] = userProfile;
            context.SharedState["Tasks"] = tasks;

            var agent = new ScheduleSpreaderAgent();

            // Act
            agent.Act(context);

            // Assert
            var resultTasks = context.SharedState["Tasks"] as List<TaskItem>;
            Assert.NotNull(resultTasks);
            Assert.Equal(2, resultTasks.Count);
            // Task A should remain unchanged
            Assert.Equal(today.AddHours(9), resultTasks[0].ScheduledStartTime);
            Assert.Equal(today.AddHours(10), resultTasks[0].ScheduledEndTime);
            // Task B should be pushed forward by 15 minutes
            Assert.Equal(today.AddHours(10).AddMinutes(15), resultTasks[1].ScheduledStartTime);
            Assert.Equal(today.AddHours(11).AddMinutes(15), resultTasks[1].ScheduledEndTime);
        }
    }
}

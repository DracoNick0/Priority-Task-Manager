using System;
using System.Collections.Generic;
using PriorityTaskManager.Models;
using PriorityTaskManager.MCP;
using PriorityTaskManager.Services.Agents;
using Xunit;

namespace PriorityTaskManager.Tests
{
    public class TaskAnalyzerAgentTests
    {
        [Fact]
        public void Act_ShouldApplyDefaults_WhenPropertiesAreMissing()
        {
            // Arrange
            var agent = new TaskAnalyzerAgent();
            var task = new TaskItem
            {
                Importance = 0,
                EstimatedDuration = TimeSpan.Zero,
                IsPinned = false, // default
                Complexity = 0.0
            };
            var context = new MCPContext();
            context.SharedState["Tasks"] = new List<TaskItem> { task };

            // Act
            var result = agent.Act(context);
            var tasks = result.SharedState["Tasks"] as List<TaskItem>;
            var processed = tasks![0];

            // Assert
            Assert.Equal(1, processed.Importance);
            Assert.Equal(TimeSpan.FromHours(1), processed.EstimatedDuration);
            Assert.False(processed.IsPinned);
            Assert.Equal(1.0, processed.Complexity);
        }

        [Fact]
        public void Act_ShouldPreserveUserValues_WhenPropertiesAreProvided()
        {
            // Arrange
            var agent = new TaskAnalyzerAgent();
            var task = new TaskItem
            {
                Importance = 5,
                EstimatedDuration = TimeSpan.FromMinutes(30),
                IsPinned = true,
                Complexity = 3.5
            };
            var context = new MCPContext();
            context.SharedState["Tasks"] = new List<TaskItem> { task };

            // Act
            var result = agent.Act(context);
            var tasks = result.SharedState["Tasks"] as List<TaskItem>;
            var processed = tasks![0];

            // Assert
            Assert.Equal(5, processed.Importance);
            Assert.Equal(TimeSpan.FromMinutes(30), processed.EstimatedDuration);
            Assert.True(processed.IsPinned);
            Assert.Equal(3.5, processed.Complexity);
        }

        [Fact]
        public void Act_ShouldNotAlterPassThroughProperties()
        {
            var agent = new TaskAnalyzerAgent();
            // Scenario 1: Properties are null/zero
            var task1 = new TaskItem
            {
                Points = 0.0,
                BeforePadding = null,
                AfterPadding = null,
                ScheduledStartTime = null,
                ScheduledEndTime = null
            };
            var context1 = new MCPContext();
            context1.SharedState["Tasks"] = new List<TaskItem> { task1 };
            var result1 = agent.Act(context1);
            var processed1 = ((List<TaskItem>)result1.SharedState["Tasks"])[0];
            Assert.Equal(0.0, processed1.Points);
            Assert.Null(processed1.BeforePadding);
            Assert.Null(processed1.AfterPadding);
            Assert.Null(processed1.ScheduledStartTime);
            Assert.Null(processed1.ScheduledEndTime);

            // Scenario 2: Properties have values
            var now = DateTime.Now;
            var task2 = new TaskItem
            {
                Points = 50.0,
                BeforePadding = TimeSpan.FromMinutes(5),
                AfterPadding = TimeSpan.FromMinutes(10),
                ScheduledStartTime = now,
                ScheduledEndTime = now.AddHours(1)
            };
            var context2 = new MCPContext();
            context2.SharedState["Tasks"] = new List<TaskItem> { task2 };
            var result2 = agent.Act(context2);
            var processed2 = ((List<TaskItem>)result2.SharedState["Tasks"])[0];
            Assert.Equal(50.0, processed2.Points);
            Assert.Equal(TimeSpan.FromMinutes(5), processed2.BeforePadding);
            Assert.Equal(TimeSpan.FromMinutes(10), processed2.AfterPadding);
            Assert.Equal(now, processed2.ScheduledStartTime);
            Assert.Equal(now.AddHours(1), processed2.ScheduledEndTime);
        }
    }
}

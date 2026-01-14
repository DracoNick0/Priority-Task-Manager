using System;
using System.Collections.Generic;
using System.Linq;
using PriorityTaskManager.Models;
using PriorityTaskManager.MCP.Agents;
using PriorityTaskManager.MCP;
using Xunit;

namespace PriorityTaskManager.Tests
{
    public class ComplexityBalancerAgentTests
    {
        [Fact]
        public void Act_PinnedTasksAreAlwaysScheduled()
        {
            var agent = new ComplexityBalancerAgent();
            var pinnedTask = new TaskItem { Title = "Pinned", Complexity = 5.0, DueDate = DateTime.Today.AddDays(1), IsPinned = true };
            var unpinnedTask = new TaskItem { Title = "Unpinned", Complexity = 10.0, DueDate = DateTime.Today.AddDays(1), IsPinned = false };
            var context = new MCPContext();
            context.SharedState["Tasks"] = new List<TaskItem> { pinnedTask, unpinnedTask };

            var result = agent.Act(context);
            var resultTasks = result.SharedState["Tasks"] as List<TaskItem>;

            Assert.NotNull(resultTasks);
            Assert.Contains(resultTasks, t => t.Title == "Pinned");
        }

        [Fact]
        public void Act_NoTaskScheduledAfterDueDate()
        {
            var agent = new ComplexityBalancerAgent();
            var task = new TaskItem { Title = "Due Soon", Complexity = 2.0, DueDate = DateTime.Today.AddDays(1), EstimatedDuration = TimeSpan.FromHours(2) };
            var context = new MCPContext();
            context.SharedState["Tasks"] = new List<TaskItem> { task };

            var result = agent.Act(context);
            var resultTasks = result.SharedState["Tasks"] as List<TaskItem>;

            Assert.NotNull(resultTasks);
            foreach (var t in resultTasks)
            {
                foreach (var chunk in t.ScheduledParts)
                {
                    Assert.True(chunk.StartTime.Date <= t.DueDate.Value.Date);
                }
            }
        }

        [Fact]
        public void Act_ComplexityDensityIsBalancedAcrossDays()
        {
            var agent = new ComplexityBalancerAgent();
            var tasks = new List<TaskItem>
            {
                new TaskItem { Title = "A", Complexity = 4.0, EstimatedDuration = TimeSpan.FromHours(2), DueDate = DateTime.Today.AddDays(2) },
                new TaskItem { Title = "B", Complexity = 2.0, EstimatedDuration = TimeSpan.FromHours(1), DueDate = DateTime.Today.AddDays(2) },
                new TaskItem { Title = "C", Complexity = 2.0, EstimatedDuration = TimeSpan.FromHours(1), DueDate = DateTime.Today.AddDays(2) }
            };
            var context = new MCPContext();
            context.SharedState["Tasks"] = tasks;

            var result = agent.Act(context);
            var resultTasks = result.SharedState["Tasks"] as List<TaskItem>;

            // Calculate density per day
            var densityByDay = new Dictionary<DateTime, double>();
            foreach (var t in resultTasks)
            {
                foreach (var chunk in t.ScheduledParts)
                {
                    var day = chunk.StartTime.Date;
                    var density = t.Complexity / t.EstimatedDuration.TotalHours;
                    if (!densityByDay.ContainsKey(day)) densityByDay[day] = 0;
                    densityByDay[day] += density;
                }
            }
            // Assert densities are roughly equal (within 20%)
            var densities = densityByDay.Values.ToList();
            var avg = densities.Average();
            foreach (var d in densities)
            {
                Assert.InRange(d, avg * 0.8, avg * 1.2);
            }
        }

        [Fact]
        public void Act_DivisibleTasksAreSplitIfNeeded()
        {
            var agent = new ComplexityBalancerAgent();
            var task = new TaskItem { Title = "Big Divisible", Complexity = 8.0, EstimatedDuration = TimeSpan.FromHours(8), DueDate = DateTime.Today.AddDays(2), IsDivisible = true };
            var context = new MCPContext();
            context.SharedState["Tasks"] = new List<TaskItem> { task };

            var result = agent.Act(context);
            var resultTasks = result.SharedState["Tasks"] as List<TaskItem>;

            Assert.NotNull(resultTasks);
            var scheduledParts = resultTasks.First().ScheduledParts;
            Assert.True(scheduledParts.Count > 1);
        }
    }
}

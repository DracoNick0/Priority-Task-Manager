using System;
using System.Collections.Generic;
using System.Linq;
using PriorityTaskManager.Models;
using PriorityTaskManager.MCP.Agents;
using PriorityTaskManager.MCP;
using PriorityTaskManager.Services;
using Xunit;

namespace PriorityTaskManager.Tests
{
    public class ComplexityBalancerAgentTests
    {
        private readonly MockTimeService _mockTimeService;

        public ComplexityBalancerAgentTests()
        {
            _mockTimeService = new MockTimeService();
            _mockTimeService.SetCurrentTime(new DateTime(2024, 1, 1, 9, 0, 0));
        }

        private MCPContext CreateContextWithDependencies(List<TaskItem> tasks)
        {
            var context = new MCPContext();
            context.SharedState["Tasks"] = tasks;
            context.SharedState["TimeService"] = _mockTimeService;

            // Create a default schedule window covering the next 30 days
            // simulating 9-5 work days (8 hours)
            var today = _mockTimeService.GetCurrentTime().Date;
            var slots = new List<TimeSlot>();
            for (int i = 0; i < 30; i++)
            {
                var day = today.AddDays(i);
                slots.Add(new TimeSlot 
                { 
                    StartTime = day.AddHours(9), 
                    EndTime = day.AddHours(17) 
                });
            }
            context.SharedState["AvailableScheduleWindow"] = new ScheduleWindow { AvailableSlots = slots };

            return context;
        }

        [Fact]
        public void Act_PinnedTasksAreAlwaysScheduled()
        {
            var agent = new ComplexityBalancerAgent();
            var today = _mockTimeService.GetCurrentTime().Date;
            var pinnedTask = new TaskItem { Title = "Pinned", Complexity = 5.0, DueDate = today.AddDays(1), IsPinned = true, EstimatedDuration = TimeSpan.FromHours(1) };
            var unpinnedTask = new TaskItem { Title = "Unpinned", Complexity = 10.0, DueDate = today.AddDays(1), IsPinned = false, EstimatedDuration = TimeSpan.FromHours(1) };
            
            var context = CreateContextWithDependencies(new List<TaskItem> { pinnedTask, unpinnedTask });

            var result = agent.Act(context);
            var resultTasks = result.SharedState["Tasks"] as List<TaskItem>;

            Assert.NotNull(resultTasks);
            Assert.Contains(resultTasks, t => t.Title == "Pinned");
        }

        [Fact]
        public void Act_NoTaskScheduledAfterDueDate()
        {
            var agent = new ComplexityBalancerAgent();
            var today = _mockTimeService.GetCurrentTime().Date;
            var task = new TaskItem { Title = "Due Soon", Complexity = 2.0, DueDate = today.AddDays(1), EstimatedDuration = TimeSpan.FromHours(2) };
            
            var context = CreateContextWithDependencies(new List<TaskItem> { task });

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
            var today = _mockTimeService.GetCurrentTime().Date;
            
            // Setup: 2 Days capacity (8h each).
            // Tasks: 2 High Density, 2 Low Density.
            // REVISION: Complexity must be 1-10.
            
            // High Density: Complexity 10 / Duration 4h = 2.5 Density
            // Low Density:  Complexity 2  / Duration 4h = 0.5 Density
            
            var tasks = new List<TaskItem>
            {
                new TaskItem { Title = "H1", Complexity = 10.0, EstimatedDuration = TimeSpan.FromHours(4), DueDate = today.AddDays(5) },
                new TaskItem { Title = "H2", Complexity = 10.0, EstimatedDuration = TimeSpan.FromHours(4), DueDate = today.AddDays(5) },
                new TaskItem { Title = "L1", Complexity = 2.0, EstimatedDuration = TimeSpan.FromHours(4), DueDate = today.AddDays(5) },
                new TaskItem { Title = "L2", Complexity = 2.0, EstimatedDuration = TimeSpan.FromHours(4), DueDate = today.AddDays(5) }
            };
            
            var context = CreateContextWithDependencies(tasks);

            var result = agent.Act(context);
            var resultTasks = result.SharedState["Tasks"] as List<TaskItem>;

            // Calculate load per day
            var loadByDay = new Dictionary<DateTime, double>();
            foreach (var t in resultTasks)
            {
                foreach (var chunk in t.ScheduledParts)
                {
                    var day = chunk.StartTime.Date;
                    var chunkLoad = t.Complexity * chunk.Duration.TotalHours;
                    if (!loadByDay.ContainsKey(day)) loadByDay[day] = 0;
                    loadByDay[day] += chunkLoad;
                }
            }

            // We expect 2 days to be used.
            // Ideal: Each day has one high and one low task (Load 48).
            // Bad: One day has both high (80), other both low (16).
            Assert.True(loadByDay.Count >= 2, "Should use at least 2 days");
            var day1 = loadByDay.Values.ElementAt(0);
            var day2 = loadByDay.Values.ElementAt(1);
            var delta = Math.Abs(day1 - day2);
            Assert.True(delta < 20, $"Complexity should be balanced. Day1: {day1}, Day2: {day2}. Delta: {delta}");
        }

        [Fact]
        public void Act_DivisibleTasksAreSplitIfNeeded()
        {
            var agent = new ComplexityBalancerAgent();
            var today = _mockTimeService.GetCurrentTime().Date;
            // A task that requires 10 hours. Our slot window is 8 hours/day. It MUST split.
            var task = new TaskItem { Title = "Big Divisible", Complexity = 8.0, EstimatedDuration = TimeSpan.FromHours(10), DueDate = today.AddDays(2), IsDivisible = true };
            
            var context = CreateContextWithDependencies(new List<TaskItem> { task });

            var result = agent.Act(context);
            var resultTasks = result.SharedState["Tasks"] as List<TaskItem>;

            Assert.NotNull(resultTasks);
            var scheduledTask = resultTasks.First();
            Assert.True(scheduledTask.ScheduledParts.Count > 1, $"Task should be split. Duration: {scheduledTask.EstimatedDuration.TotalHours}");
        }
    }
}

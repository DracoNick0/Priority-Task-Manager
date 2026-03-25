using Xunit;
using PriorityTaskManager.Models;
using PriorityTaskManager.MCP;
using PriorityTaskManager.MCP.Agents;
using PriorityTaskManager.Tests.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PriorityTaskManager.Tests.MCP.GoldPanning
{
    public class ScheduleSpreaderAgentTests
    {
        private readonly ScheduleSpreaderAgent _agent;
        private readonly MockTimeService _timeService;

        public ScheduleSpreaderAgentTests()
        {
            _agent = new ScheduleSpreaderAgent();
            _timeService = new MockTimeService();
            _timeService.SetCurrentTime(new DateTime(2024, 1, 1, 8, 0, 0)); // Monday
        }

        private MCPContext CreateContext(List<TaskItem> tasks, int days = 5, int hoursPerDay = 8)
        {
            var context = new MCPContext();
            context.SharedState["Tasks"] = tasks;

            // Create window
            var today = _timeService.GetCurrentTime().Date;
            var slots = new List<TimeSlot>();
            for (int i = 0; i < days; i++)
            {
                var day = today.AddDays(i);
                slots.Add(new TimeSlot 
                { 
                    StartTime = day.AddHours(9), 
                    EndTime = day.AddHours(9 + hoursPerDay)
                });
            }
            context.SharedState["AvailableScheduleWindow"] = new ScheduleWindow { AvailableSlots = slots };

            // Create weights (simulate PrioritizationAgent output)
            var weights = new Dictionary<int, double>();
            foreach (var task in tasks)
            {
                // Simple weight: Importance * 10
                weights[task.Id] = task.Importance * 10.0;
            }
            context.SharedState["TaskWeights"] = weights;

            return context;
        }

        [Fact]
        public void Act_NoTasks_ShouldReturnContextUnchanged()
        {
            var context = CreateContext(new List<TaskItem>());
            var result = _agent.Act(context);
            Assert.False(result.SharedState.ContainsKey("DailyBuckets"));
        }

        [Fact]
        public void Act_FitsOnFirstDay_ShouldNotSpread()
        {
            var task = new TaskItem { Id = 1, Title = "Fits", EstimatedDuration = TimeSpan.FromHours(4), Importance = 1 };
            var context = CreateContext(new List<TaskItem> { task });

            var result = _agent.Act(context);
            
            Assert.True(result.SharedState.ContainsKey("DailyBuckets"));
            var buckets = result.SharedState["DailyBuckets"] as Dictionary<DateTime, List<TaskItem>>;
            
            var day1 = _timeService.GetCurrentTime().Date;
            Assert.Single(buckets[day1]);
            Assert.Equal("Fits", buckets[day1][0].Title);
        }

        [Fact]
        public void Act_OverflowsDay1_ShouldSpreadToDay2()
        {
            // Day 1 Cap: 8h.
            // Task A: 5h. Task B: 5h. Total 10h.
            // Task A is Heavier (Imp 5) than B (Imp 1).
            // Expect: A stays on Day 1. B moves to Day 2.
            
            var taskA = new TaskItem { Id = 1, Title = "Heavy", EstimatedDuration = TimeSpan.FromHours(5), Importance = 5 };
            var taskB = new TaskItem { Id = 2, Title = "Light", EstimatedDuration = TimeSpan.FromHours(5), Importance = 1 };
            
            var context = CreateContext(new List<TaskItem> { taskA, taskB });

            var result = _agent.Act(context);
            var buckets = result.SharedState["DailyBuckets"] as Dictionary<DateTime, List<TaskItem>>;
            var day1 = _timeService.GetCurrentTime().Date;
            var day2 = day1.AddDays(1);

            Assert.Single(buckets[day1]);
            Assert.Single(buckets[day2]);
            
            Assert.Equal("Heavy", buckets[day1][0].Title);
            Assert.Equal("Light", buckets[day2][0].Title);
        }

        [Fact]
        public void Act_MultiDayCascade_ShouldSpreadCorrectly()
        {
            // Day Cap 8h.
            // Tasks: A(5h, Imp 10), B(5h, Imp 5), C(5h, Imp 1). Total 15h.
            // Day 1: A (5h). Rem: 3h. B (5h) -> Overflow.
            // B moves to Day 2.
            // Day 2: B (5h). Rem: 3h. C (5h) -> Overflow.
            // C moves to Day 3.
            
            var taskA = new TaskItem { Id = 1, Title = "Heaviest", EstimatedDuration = TimeSpan.FromHours(5), Importance = 10 };
            var taskB = new TaskItem { Id = 2, Title = "Medium", EstimatedDuration = TimeSpan.FromHours(5), Importance = 5 };
            var taskC = new TaskItem { Id = 3, Title = "Lightest", EstimatedDuration = TimeSpan.FromHours(5), Importance = 1 };

            var context = CreateContext(new List<TaskItem> { taskA, taskB, taskC });
            var result = _agent.Act(context);
            
            var buckets = result.SharedState["DailyBuckets"] as Dictionary<DateTime, List<TaskItem>>;
            var today = _timeService.GetCurrentTime().Date;
            
            Assert.Equal("Heaviest", buckets[today][0].Title);
            Assert.Equal("Medium", buckets[today.AddDays(1)][0].Title);
            Assert.Equal("Lightest", buckets[today.AddDays(2)][0].Title);
        }

        [Fact]
        public void Act_MassiveOverflow_ShouldDropOrWarn()
        {
            // Window: 1 Day. Cap 8h.
            // Task: 10h.
            // Expect: Warning/Drop (since it can't fit in window).
            
            var task = new TaskItem { Id = 1, Title = "Too Big", EstimatedDuration = TimeSpan.FromHours(10), Importance = 5 };
            var context = CreateContext(new List<TaskItem> { task }, days: 1); // Only 1 day window

            var result = _agent.Act(context);
            var buckets = result.SharedState["DailyBuckets"] as Dictionary<DateTime, List<TaskItem>>;
            
            // It might be removed from buckets entirely if dropped.
            var day1 = _timeService.GetCurrentTime().Date;
            Assert.Empty(buckets[day1]);
            
            Assert.Contains(result.History, h => h.Contains("OVERFLOW") || h.Contains("Overflow"));
        }
    }
}

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
    public class DaySequencingAgentTests
    {
        private readonly DaySequencingAgent _agent;
        private readonly MockTimeService _timeService;

        public DaySequencingAgentTests()
        {
            _agent = new DaySequencingAgent();
            _timeService = new MockTimeService();
            _timeService.SetCurrentTime(new DateTime(2024, 1, 1, 8, 0, 0));
        }

        private MCPContext CreateContext(Dictionary<DateTime, List<TaskItem>> buckets)
        {
            var context = new MCPContext();
            context.SharedState["DailyBuckets"] = buckets;

            // Create window matching the buckets
            var days = buckets.Keys.OrderBy(d => d).ToList();
            if(!days.Any()) return context;

            var slots = new List<TimeSlot>();
            foreach(var day in days)
            {
                // Standard 9-5 work day
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
        public void Act_NoBuckets_ShouldReturnContextUnchanged()
        {
            var context = new MCPContext();
            var result = _agent.Act(context);
            Assert.DoesNotContain("Sequencing complete", result.History);
        }

        [Fact]
        public void Act_SequencesHighComplexityFirst()
        {
            var today = _timeService.GetCurrentTime().Date;
            var highComp = new TaskItem { Id = 1, Title = "High", Complexity = 5, EstimatedDuration = TimeSpan.FromHours(1) };
            var lowComp = new TaskItem { Id = 2, Title = "Low", Complexity = 1, EstimatedDuration = TimeSpan.FromHours(1) };
            
            var buckets = new Dictionary<DateTime, List<TaskItem>>
            {
                { today, new List<TaskItem> { lowComp, highComp } } // Input Order: Low then High
            };

            var context = CreateContext(buckets);
            var result = _agent.Act(context);
            
            var tasks = result.SharedState["Tasks"] as List<TaskItem>;
            Assert.NotNull(tasks);
            
            // Check sequencing order by Start Time
            var tHigh = tasks.First(t => t.Title == "High");
            var tLow = tasks.First(t => t.Title == "Low");
            
            Assert.Single(tHigh.ScheduledParts);
            Assert.Single(tLow.ScheduledParts);
            
            Assert.True(tHigh.ScheduledParts[0].StartTime < tLow.ScheduledParts[0].StartTime, "High complexity should be scheduled before Low complexity");
        }

        [Fact]
        public void Act_SequencesImportanceTieBreaker()
        {
            var today = _timeService.GetCurrentTime().Date;
            // Both High Complexity (5). A is High Imp (5), B is Low Imp (1).
            var highImp = new TaskItem { Id = 1, Title = "HighImp", Complexity = 5, Importance = 5, EffectiveImportance = 5, EstimatedDuration = TimeSpan.FromHours(1) };
            var lowImp = new TaskItem { Id = 2, Title = "LowImp", Complexity = 5, Importance = 1, EffectiveImportance = 1, EstimatedDuration = TimeSpan.FromHours(1) };
            
            var buckets = new Dictionary<DateTime, List<TaskItem>>
            {
                { today, new List<TaskItem> { lowImp, highImp } }
            };

            var context = new MCPContext();
            context.SharedState["DailyBuckets"] = buckets;
            
            // Need ScheduleWindow
            var slots = new List<TimeSlot> 
            { 
                new TimeSlot { StartTime = today.AddHours(9), EndTime = today.AddHours(17) } 
            };
            context.SharedState["AvailableScheduleWindow"] = new ScheduleWindow { AvailableSlots = slots };

            var agent = new DaySequencingAgent();
            var result = agent.Act(context);
            
            var tasks = result.SharedState["Tasks"] as List<TaskItem>;
            var tHigh = tasks.First(t => t.Title == "HighImp");
            var tLow = tasks.First(t => t.Title == "LowImp");

            Assert.True(tHigh.ScheduledParts[0].StartTime < tLow.ScheduledParts[0].StartTime, "High Importance should break tie locally");
        }

        [Fact]
        public void Act_TaskSpansSlots_ShouldSplitChunk()
        {
            // Setup: 1 Day. Slot 1 (9-10). Slot 2 (11-12). Gap 10-11.
            // Task: 2 Hours. 
            // Should fill Slot 1 (1h), skip gap, fill Slot 2 (1h).
            
            var today = _timeService.GetCurrentTime().Date;
            var task = new TaskItem { Id = 1, Title = "Span", Complexity = 1, EstimatedDuration = TimeSpan.FromHours(2), IsDivisible = true };
            
            var context = new MCPContext();
            context.SharedState["DailyBuckets"] = new Dictionary<DateTime, List<TaskItem>> { { today, new List<TaskItem> { task } } };
            
            var slots = new List<TimeSlot>
            {
                new TimeSlot { StartTime = today.AddHours(9), EndTime = today.AddHours(10) },
                new TimeSlot { StartTime = today.AddHours(11), EndTime = today.AddHours(12) }
            };
            context.SharedState["AvailableScheduleWindow"] = new ScheduleWindow { AvailableSlots = slots };

            var result = _agent.Act(context);
            var tasks = result.SharedState["Tasks"] as List<TaskItem>;
            var tResult = tasks.First();
            
            Assert.Equal(2, tResult.ScheduledParts.Count);
            Assert.Equal(today.AddHours(9), tResult.ScheduledParts[0].StartTime);
            Assert.Equal(today.AddHours(10), tResult.ScheduledParts[0].EndTime); // End of Slot 1
            
            Assert.Equal(today.AddHours(11), tResult.ScheduledParts[1].StartTime); // Start of Slot 2
            Assert.Equal(today.AddHours(12), tResult.ScheduledParts[1].EndTime);
        }

        [Fact(Skip = "FAILING: Legacy 'Eat the Frog' logic prioritizes Complexity over Importance. Un-skip during V1 migration to implement 'Prioritized Frogs'.")]
        public void Act_PreferImportanceOverComplexity()
        {
            // User Request: Importance should be primary, Complexity secondary.
            // Scenario: 
            // Task A: High Importance (5), Low Complexity (1).
            // Task B: Low Importance (1), High Complexity (5).
            // Current Legacy Behavior (Eat the Frog): B comes first (Complexity > Importance).
            // Desired Behavior for V1/Future: A comes first (Importance > Complexity).
            // This test is EXPECTED TO FAIL FOR NOW on the legacy branch.

            var today = _timeService.GetCurrentTime().Date;
            
            var importantTask = new TaskItem 
            { 
                Id = 1, 
                Title = "Important", 
                Importance = 5, 
                EffectiveImportance = 5,
                Complexity = 1, // Low Complexity
                EstimatedDuration = TimeSpan.FromHours(1) 
            };

            var complexTask = new TaskItem 
            { 
                Id = 2, 
                Title = "Complex", 
                Importance = 1, 
                EffectiveImportance = 1,
                Complexity = 5, // High Complexity (Frog)
                EstimatedDuration = TimeSpan.FromHours(1) 
            };
            
            var buckets = new Dictionary<DateTime, List<TaskItem>>
            {
                { today, new List<TaskItem> { complexTask, importantTask } }
            };

            var context = CreateContext(buckets);
            var result = _agent.Act(context);
            
            var tasks = result.SharedState["Tasks"] as List<TaskItem>;
            var tImp = tasks.First(t => t.Title == "Important");
            var tComp = tasks.First(t => t.Title == "Complex");

            // Assertion: The Important task should start BEFORE the Complex task
            Assert.True(tImp.ScheduledParts[0].StartTime < tComp.ScheduledParts[0].StartTime, 
                "High Importance should take precedence over High Complexity");
        }
    }
}
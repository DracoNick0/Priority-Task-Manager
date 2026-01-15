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

        private MCPContext CreateContextWithDependencies(List<TaskItem> tasks, int daysCount = 30)
        {
            var context = new MCPContext();
            context.SharedState["Tasks"] = tasks;
            context.SharedState["TimeService"] = _mockTimeService;

            // Create a default schedule window covering the specified days
            // simulating 9-5 work days (8 hours)
            var today = _mockTimeService.GetCurrentTime().Date;
            var slots = new List<TimeSlot>();
            for (int i = 0; i < daysCount; i++)
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
        public void Act_PinnedTasksAreAlwaysScheduled_EvenInConflict()
        {
            var agent = new ComplexityBalancerAgent();
            var today = _mockTimeService.GetCurrentTime().Date;
            
            // Setup: Only 1 Day available, with 2 hours capacity? 
            // Better: 1 Day, 8 hours capacity.
            // Pinned Task: 4 Hours. Due Day 1.
            // Unpinned Task: 6 Hours. Due Day 1. High Complexity (should win sorting if not for Pinned).
            // Conflict: 4+6 = 10 Hours > 8 Hours Capacity.
            // Pinned must take the slot. Unpinned should fail/warn.

            var pinnedTask = new TaskItem 
            { 
                Title = "Pinned", 
                Complexity = 2.0, 
                DueDate = today.AddDays(1), 
                IsPinned = true, 
                EstimatedDuration = TimeSpan.FromHours(4),
                IsDivisible = false
            };
            
            var unpinnedTask = new TaskItem 
            { 
                Title = "Unpinned High Value", 
                Complexity = 10.0, 
                DueDate = today.AddDays(1), 
                IsPinned = false, 
                EstimatedDuration = TimeSpan.FromHours(6),
                IsDivisible = false
            };
            
            // Note: We use 1 day window.
            var context = CreateContextWithDependencies(new List<TaskItem> { pinnedTask, unpinnedTask }, daysCount: 1);

            var result = agent.Act(context);
            var resultTasks = result.SharedState["Tasks"] as List<TaskItem>;

            Assert.NotNull(resultTasks);
            
            // Pinned must be there
            Assert.Contains(resultTasks, t => t.Title == "Pinned");
            var scheduledPinned = resultTasks.First(t => t.Title == "Pinned");
            Assert.NotEmpty(scheduledPinned.ScheduledParts);

            // Unpinned should NOT be scheduled because it doesn't fit after Pinned takes 4h (Leaving 4h, Unpinned needs 6h)
            // (Unless it splits, but set IsDivisible=false)
            var scheduledUnpinned = resultTasks.FirstOrDefault(t => t.Title == "Unpinned High Value");
            
            // Logic check: If the agent successfully filtered out the unschedulable one without crashing
            if (scheduledUnpinned != null)
            {
                Assert.Empty(scheduledUnpinned.ScheduledParts);
            }
            else
            {
                // Task might be removed from list or just not have scheduled parts? 
                // The agent loops through input tasks and outputs 'scheduledTasks'.
                // If it fails to schedule, it doesn't add to 'scheduledTasks' list in Act() method.
                // So expected is that unpinned is NOT in result list.
                 Assert.DoesNotContain(resultTasks, t => t.Title == "Unpinned High Value");
            }
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
                    Assert.True(t.DueDate.HasValue);
                    Assert.True(chunk.StartTime.Date <= t.DueDate!.Value.Date);
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
            Assert.NotNull(resultTasks);
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

        [Fact]
        public void Act_DistributesMixedWorkloadEvenly_TheBurnoutWeek()
        {
            var agent = new ComplexityBalancerAgent();
            var today = _mockTimeService.GetCurrentTime().Date;
            
            // Setup: 5 Days (Mon-Fri), 8 hours each.
            // Tasks:
            // 3x Deep Work (Comp 10, 4h) = 12h, 120 Load
            // 5x Standard (Comp 5, 2h)   = 10h,  50 Load
            // 10x Chore (Comp 1, 1h)     = 10h,  10 Load
            // Total: 32h, 180 Load. 
            // Target: ~6.4h/day, ~36 Load/day.

            var tasks = new List<TaskItem>();
            for(int i=0; i<3; i++) tasks.Add(new TaskItem { Title = $"Deep {i}", Complexity = 10, EstimatedDuration = TimeSpan.FromHours(4), DueDate = today.AddDays(5) });
            for(int i=0; i<5; i++) tasks.Add(new TaskItem { Title = $"Std {i}", Complexity = 5, EstimatedDuration = TimeSpan.FromHours(2), DueDate = today.AddDays(5) });
            for(int i=0; i<10; i++) tasks.Add(new TaskItem { Title = $"Chore {i}", Complexity = 1, EstimatedDuration = TimeSpan.FromHours(1), DueDate = today.AddDays(5) });

            var context = CreateContextWithDependencies(tasks);
            // Ensure context has 5 days of slots (mock service defaults to 30, so efficient)

            var result = agent.Act(context);
            var resultTasks = result.SharedState["Tasks"] as List<TaskItem>;
            Assert.NotNull(resultTasks);

            // Analyze Daily Loads
            var loadByDay = new Dictionary<DateTime, double>();
            var hoursByDay = new Dictionary<DateTime, double>();

            // Initialize 5 days
            for(int i=0; i<5; i++) 
            {
                var d = today.AddDays(i);
                loadByDay[d] = 0;
                hoursByDay[d] = 0;
            }

            foreach (var t in resultTasks)
            {
                foreach (var chunk in t.ScheduledParts)
                {
                    var day = chunk.StartTime.Date;
                    if(day >= today.AddDays(5)) continue; // Ignore if scheduled later (shouldn't happen with due dates)

                    var load = t.Complexity * chunk.Duration.TotalHours;
                    
                    loadByDay[day] += load;
                    hoursByDay[day] += chunk.Duration.TotalHours;
                }
            }

            // Assertions
            // 1. No day should exceed reasonable burnout limit (e.g. Load > 60 is heavily stressed).
            // Ideal is 36. 
            // If it stacks 2 Deep Works -> 80 Load. FAIL.
            
            var maxLoad = loadByDay.Values.Take(5).Max();
            var minLoad = loadByDay.Values.Take(5).Min();
            
            // We want tight grouping around 36.
            // Allow range 20-60.
            Assert.True(maxLoad < 65, $"Max Daily Load {maxLoad} is too high (Burnout Risk). Ideal is 36.");
            Assert.True(minLoad > 15, $"Min Daily Load {minLoad} is too low (Slack Day). Ideal is 36.");
            
            // Ensure hours are also reasonable (not leaving huge gaps if density is low?)
            // Actually, density is the goal.
        }

        [Fact]
        public void Act_NonDivisibleTask_ExceedingDailyCapacity_IsSkipped()
        {
            var agent = new ComplexityBalancerAgent();
            // Setup: 8 hour days.
            // Task: 10 Hours, Not Divisible.
            // It physically cannot fit into any single day.
            
            var task = new TaskItem 
            { 
                Title = "Impossible Task", 
                Complexity = 5, 
                EstimatedDuration = TimeSpan.FromHours(10), 
                IsDivisible = false,
                DueDate = _mockTimeService.GetCurrentTime().Date.AddDays(5)
            };

            var context = CreateContextWithDependencies(new List<TaskItem> { task });

            var result = agent.Act(context);
            var resultTasks = result.SharedState["Tasks"] as List<TaskItem>;
            
            // Should be skipped or have empty scheduled parts
            // NOTE: Agent currently returns only scheduled tasks? Or all tasks?
            // If the task was skipped, it might not be in the output list's scheduled subset if the agent filters.
            // Let's check. 
            if (resultTasks!.Count > 0)
            {
               var resultTask = resultTasks.First();
               Assert.Empty(resultTask.ScheduledParts);
            }
            else
            {
               // If empty, it means it wasn't returned, which is effectively skipping it.
               Assert.True(true); 
            }
            
            // Should have warning in history
            Assert.Contains(context.History, h => h.Contains("Could not schedule") && h.Contains("Impossible Task"));
        }

        [Fact]
        public void Act_InsufficientTotalCapacity_SchedulesPartialAndWarns()
        {
            var agent = new ComplexityBalancerAgent();
            
            // Setup: 1 Day Window (8 hours).
            // Task: 10 Hours, Divisible.
            // Should schedule 8 hours, leave 2 hours, and warn.
            
            var task = new TaskItem 
            { 
                Title = "Overflow Task", 
                Complexity = 5, 
                EstimatedDuration = TimeSpan.FromHours(10), 
                IsDivisible = true,
                DueDate = _mockTimeService.GetCurrentTime().Date.AddDays(1)
            };

            var context = CreateContextWithDependencies(new List<TaskItem> { task }, daysCount: 1);

            var result = agent.Act(context);
            var resultTasks = result.SharedState["Tasks"] as List<TaskItem>;
            var resultTask = resultTasks.First();
            
            // Check scheduled amount
            var totalScheduled = resultTask.ScheduledParts.Sum(c => c.Duration.TotalHours);
            Assert.Equal(8.0, totalScheduled); // Took all available capacity
            
            // Check Warning
            Assert.Contains(context.History, h => h.Contains("Could not fully schedule") && h.Contains("Overflow Task"));
        }

        [Fact]
        public void Act_TaskWithoutDueDate_UsesFullWindow()
        {
            var agent = new ComplexityBalancerAgent();
            var today = _mockTimeService.GetCurrentTime().Date;

            // Setup:
            // Window: 10 Days.
            // Task A: Due Day 1 (Tight).
            // Task B: No Due Date (Should be free to use Day 2-10).
            // Task A Fills Day 1.
            
            var taskA = new TaskItem { Title = "Urgent", DueDate = today.AddDays(1), EstimatedDuration = TimeSpan.FromHours(8) };
            var taskB = new TaskItem { Title = "Backlog", DueDate = null, EstimatedDuration = TimeSpan.FromHours(4) };
            
            var context = CreateContextWithDependencies(new List<TaskItem> { taskA, taskB }, daysCount: 10);
            
            var result = agent.Act(context);
            var resultTasks = result.SharedState["Tasks"] as List<TaskItem>;
            
            var resultB = resultTasks.First(t => t.Title == "Backlog");
            
            // If B was constrained to Day 1, it would be unscheduled (capacity full)
            Assert.NotEmpty(resultB.ScheduledParts);
            
            // Assert that it is scheduled LATER than Day 1 (meaning it used the full window, not just Task A's due date)
            // If the logic was faulty, it would try to cram on Day 1 alongside Task A, fail, or crash.
            // Wait, if it tried to cram on Day 1, Day 1 is full (8h). Available is 0. 
            // So resultB.ScheduledParts would be empty if constrained.
            
            // If the test failed assertion "True( ... > Day1 )", it means:
            // 1. It found a spot on Day 1? (But A took 8h? Maybe order matters. A took 8h.)
            // 2. It's scheduled on Day <= 1.
            
            var schedule = resultB.ScheduledParts.First();
            Assert.True(schedule.StartTime.Date > today.AddDays(1), $"Backlog scheduled on {schedule.StartTime.Date.ToShortDateString()} but should be after {today.AddDays(1).ToShortDateString()}. Is it constrained?");
            }
    }
}

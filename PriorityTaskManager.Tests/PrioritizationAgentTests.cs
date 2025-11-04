using System;
using System.Collections.Generic;
using Xunit;
using PriorityTaskManager.Models;
using PriorityTaskManager.MCP;
using PriorityTaskManager.Services.Agents;

using PriorityTaskManager.Services.Helpers;

namespace PriorityTaskManager.Tests
{
    public class PrioritizationAgentTests
    {
        [Fact]
        public void Act_ShouldRejectFullChainAndRePlan_WhenDependencyIsImpossible()
        {
            // Arrange
            var task1 = new TaskItem { Id = 1, IsPinned = true, EstimatedDuration = TimeSpan.FromHours(20), Title = "Impossible Pinned" };
            var task2 = new TaskItem { Id = 2, Dependencies = new List<int> { 1 }, Title = "Dependent on Impossible" };
            var task3 = new TaskItem { Id = 3, EstimatedDuration = TimeSpan.FromHours(2), Title = "Valid 1" };
            var task4 = new TaskItem { Id = 4, EstimatedDuration = TimeSpan.FromHours(3), Title = "Valid 2" };
            var allTasks = new List<TaskItem> { task1, task2, task3, task4 };

            var context = new MCPContext();
            context.SharedState["Tasks"] = allTasks;
            context.SharedState["TotalAvailableTime"] = TimeSpan.FromHours(8);

            var helper = new DependencyGraphHelper();
            var agent = new PrioritizationAgent(helper);

            // Act
            agent.Act(context);

            // Assert
            Assert.True(context.SharedState.ContainsKey("Tasks"));
            var finalSchedule = context.SharedState["Tasks"] as List<TaskItem>;
            Assert.NotNull(finalSchedule);
            Assert.Equal(2, finalSchedule.Count);
            Assert.Contains(finalSchedule, t => t.Id == 3);
            Assert.Contains(finalSchedule, t => t.Id == 4);
            Assert.DoesNotContain(finalSchedule, t => t.Id == 1);
            Assert.DoesNotContain(finalSchedule, t => t.Id == 2);

            Assert.True(context.SharedState.ContainsKey("UnschedulableTasks"));
            var unschedulable = context.SharedState["UnschedulableTasks"] as List<TaskItem>;
            Assert.NotNull(unschedulable);
            Assert.Equal(2, unschedulable.Count);
            Assert.Contains(unschedulable, t => t.Id == 1);
            Assert.Contains(unschedulable, t => t.Id == 2);
        }
        [Fact]
        public void Act_ShouldNeverRemovePinnedTask_EvenWithLowImportance()
        {
            // Arrange
            var helper = new WorkdayTimeHelper(DateTime.Today);
            var scheduleWindow = new ScheduleWindow
            {
                AvailableSlots = new List<TimeSlot>
                {
                    new TimeSlot
                    {
                        StartTime = helper.OffsetToDateTime(TimeSpan.Zero),
                        EndTime = helper.OffsetToDateTime(TimeSpan.FromHours(10))
                    }
                }
            };

            var taskA = new TaskItem
            {
                Title = "Task A",
                Importance = 1,
                EstimatedDuration = TimeSpan.FromHours(5),
                IsPinned = true
            };
            var taskB = new TaskItem
            {
                Title = "Task B",
                Importance = 5,
                EstimatedDuration = TimeSpan.FromHours(5),
                IsPinned = false
            };
            var taskC = new TaskItem
            {
                Title = "Task C",
                Importance = 10,
                EstimatedDuration = TimeSpan.FromHours(1),
                DueDate = helper.OffsetToDateTime(TimeSpan.FromHours(1)) // Today at 10 AM
            };
            var tasks = new List<TaskItem> { taskA, taskB, taskC };

            var context = new MCPContext();
            context.SharedState["Tasks"] = tasks;
            context.SharedState["TotalAvailableTime"] = TimeSpan.FromHours(10);
            context.SharedState["AvailableScheduleWindow"] = scheduleWindow;

            var agent = new PrioritizationAgent();

            // Act
            agent.Act(context);

            // Assert
            Assert.True(context.SharedState.ContainsKey("Tasks"));
            var result = context.SharedState["Tasks"] as List<TaskItem>;
            Assert.NotNull(result);
            Assert.Equal(2, result.Count); // Only two tasks should be scheduled
            Assert.Contains(result, t => t.Title == "Task A"); // Pinned task must remain
            Assert.Contains(result, t => t.Title == "Task C"); // New valid task must be scheduled
            Assert.DoesNotContain(result, t => t.Title == "Task B"); // Unpinned task should be removed
        }

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

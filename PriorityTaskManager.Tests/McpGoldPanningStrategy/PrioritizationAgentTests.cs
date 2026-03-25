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
    public class PrioritizationAgentTests
    {
        /*
         * === Test Coverage for PrioritizationAgent ===
         * [ ] Context Handling: Agent returns gracefully if 'Tasks' key is missing, invalid, or the list is empty.
         * [ ] Primary Sort (DueDate): Tasks are sorted correctly by their due date (ascending).
         * [ ] Secondary Sort (Complexity): Tasks with the same due date are sorted by complexity (descending).
         * [ ] Stable Sort: Tasks with no due date are sorted by complexity and appear after tasks with due dates.
         * [ ] Mixed Data: A comprehensive test with a mix of due dates and complexities is sorted correctly.
        */

        private readonly PrioritizationAgent _agent;

        public PrioritizationAgentTests()
        {
            _agent = new PrioritizationAgent();
        }

        [Fact]
        public void Act_WhenContextHasNoTasks_ShouldReturnContextUnchanged()
        {
            // Arrange
            var context = new MCPContext();

            // Act
            var resultContext = _agent.Act(context);

            // Assert
            Assert.False(resultContext.SharedState.ContainsKey("Tasks"));
            Assert.DoesNotContain("-> Task list sorted for scheduling agent.", resultContext.History);
        }

        [Fact]
        public void Act_WhenTasksListIsEmpty_ShouldReturnContextUnchanged()
        {
            // Arrange
            var context = new MCPContext();
            var tasks = new List<TaskItem>();
            context.SharedState["Tasks"] = tasks;

            // Act
            var resultContext = _agent.Act(context);
            var resultTasks = resultContext.SharedState["Tasks"] as List<TaskItem>;

            // Assert
            Assert.NotNull(resultTasks);
            Assert.Empty(resultTasks);
            Assert.DoesNotContain("-> Task list sorted for scheduling agent.", resultContext.History);
        }

        [Fact]
        public void Act_ShouldSortByDueDate_Primarily()
        {
            // Arrange
            var context = CreateContextWithTime();
            var task1 = new TaskItem { Id = 1, Title = "Due Later", DueDate = DateTime.Now.AddDays(2), Importance = 1 };
            var task2 = new TaskItem { Id = 2, Title = "Due Sooner", DueDate = DateTime.Now.AddDays(1), Importance = 1 };
            var tasks = new List<TaskItem> { task1, task2 };
            context.SharedState["Tasks"] = tasks;

            // Act
            var resultContext = _agent.Act(context);
            var resultTasks = resultContext.SharedState["Tasks"] as List<TaskItem>;

            // Assert
            Assert.NotNull(resultTasks);
            Assert.Equal("Due Sooner", resultTasks[0].Title);
            Assert.Equal("Due Later", resultTasks[1].Title);
        }

        [Fact]
        public void Act_ShouldSortByImportance_Secondarily()
        {
            // Arrange
            var context = CreateContextWithTime();
            var sameDueDate = DateTime.Now.AddDays(1);
            var task1 = new TaskItem { Id = 1, Title = "Less Important", DueDate = sameDueDate, Importance = 1 };
            var task2 = new TaskItem { Id = 2, Title = "More Important", DueDate = sameDueDate, Importance = 5 };
            var tasks = new List<TaskItem> { task1, task2 };
            context.SharedState["Tasks"] = tasks;

            // Act
            var resultContext = _agent.Act(context);
            var resultTasks = resultContext.SharedState["Tasks"] as List<TaskItem>;

            // Assert
            Assert.NotNull(resultTasks);
            Assert.Equal("More Important", resultTasks[0].Title);
            Assert.Equal("Less Important", resultTasks[1].Title);
        }

        [Fact]
        public void Act_ShouldPlaceTasksWithNoDueDate_Last_IfLowImportance()
        {
            // Arrange
            var context = CreateContextWithTime();
            var task1 = new TaskItem { Id = 1, Title = "No Due Date", Importance = 1 }; 
            var task2 = new TaskItem { Id = 2, Title = "Has Due Date", DueDate = DateTime.Now.AddDays(1), Importance = 1 };
            var tasks = new List<TaskItem> { task1, task2 };
            context.SharedState["Tasks"] = tasks;

            // Act
            var resultContext = _agent.Act(context);
            var resultTasks = resultContext.SharedState["Tasks"] as List<TaskItem>;

            // Assert
            Assert.NotNull(resultTasks);
            Assert.Equal("Has Due Date", resultTasks[0].Title);
            Assert.Equal("No Due Date", resultTasks[1].Title);
        }

        [Fact]
        public void Act_WithMixedTasks_ShouldSortCorrectly()
        {
            // Arrange
            var context = CreateContextWithTime();
            var today = DateTime.Now.Date;

            // Task Weights Logic: (Urgency + Importance*5) * Density
            // Urgency = 100 / (Days + 1)^2
            // Backlog Density = 0.5

            // A: Due Tomorrow (1 day), High Imp (5). U=100/4=25. I=25. Total=50.
            // B: Due Tomorrow (1 day), Low Imp (1).  U=100/4=25. I=5.  Total=30.
            // C: No Due, Low Imp (1).                U=0.        I=5.  Total=2.5.
            // D: No Due, High Imp (5).               U=0.        I=25. Total=12.5.
            // E: Due Today (0 days), Med Imp (3).    U=100/1=100. I=15. Total=115.

            // Expected Order: E (115), A (50), B (30), D (12.5), C (2.5)

            var tasks = new List<TaskItem>
            {
                new TaskItem { Id = 3, Title = "C: No Due Date, Low Importance", Importance = 1 },
                new TaskItem { Id = 2, Title = "A: Due Tomorrow, High Importance", DueDate = today.AddDays(1), Importance = 5 },
                new TaskItem { Id = 4, Title = "D: No Due Date, High Importance", Importance = 5 },
                new TaskItem { Id = 5, Title = "B: Due Tomorrow, Low Importance", DueDate = today.AddDays(1), Importance = 1 },
                new TaskItem { Id = 1, Title = "E: Due Today", DueDate = today, Importance = 3 }
            };
            context.SharedState["Tasks"] = tasks;

            // Act
            var resultContext = _agent.Act(context);
            var resultTasks = resultContext.SharedState["Tasks"] as List<TaskItem>;
            
            Assert.NotNull(resultTasks);
            var resultTitles = resultTasks.Select(t => t.Title ?? string.Empty).ToList();

            // Assert
            Assert.NotNull(resultTitles);
            var expectedOrder = new List<string>
            {
                "E: Due Today",
                "A: Due Tomorrow, High Importance",
                "B: Due Tomorrow, Low Importance",
                "D: No Due Date, High Importance",
                "C: No Due Date, Low Importance"
            };
            Assert.Equal(expectedOrder, resultTitles);
        }

        private MCPContext CreateContextWithTime()
        {
            var context = new MCPContext();
            context.SharedState["TimeService"] = new MockTimeService(); 
            return context;
        }
    }
}

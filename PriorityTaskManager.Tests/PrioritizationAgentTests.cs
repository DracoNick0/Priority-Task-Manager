using Xunit;
using PriorityTaskManager.Models;
using PriorityTaskManager.MCP;
using PriorityTaskManager.MCP.Agents;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PriorityTaskManager.Tests
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
            var context = new MCPContext();
            var task1 = new TaskItem { Title = "Due Later", DueDate = DateTime.Now.AddDays(2) };
            var task2 = new TaskItem { Title = "Due Sooner", DueDate = DateTime.Now.AddDays(1) };
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
        public void Act_ShouldSortByComplexity_Secondarily()
        {
            // Arrange
            var context = new MCPContext();
            var sameDueDate = DateTime.Now.AddDays(1);
            var task1 = new TaskItem { Title = "Less Complex", DueDate = sameDueDate, Complexity = 1.0 };
            var task2 = new TaskItem { Title = "More Complex", DueDate = sameDueDate, Complexity = 5.0 };
            var tasks = new List<TaskItem> { task1, task2 };
            context.SharedState["Tasks"] = tasks;

            // Act
            var resultContext = _agent.Act(context);
            var resultTasks = resultContext.SharedState["Tasks"] as List<TaskItem>;

            // Assert
            Assert.NotNull(resultTasks);
            Assert.Equal("More Complex", resultTasks[0].Title);
            Assert.Equal("Less Complex", resultTasks[1].Title);
        }

        [Fact]
        public void Act_ShouldPlaceTasksWithNoDueDate_Last()
        {
            // Arrange
            var context = new MCPContext();
            // TaskItem constructor sets DueDate to DateTime.MaxValue if not specified.
            var task1 = new TaskItem { Title = "No Due Date" }; 
            var task2 = new TaskItem { Title = "Has Due Date", DueDate = DateTime.Now.AddDays(1) };
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
            var context = new MCPContext();
            var today = DateTime.Now;

            var tasks = new List<TaskItem>
            {
                new TaskItem { Title = "C: No Due Date, Low Complexity", Complexity = 1.0 },
                new TaskItem { Title = "A: Due Tomorrow, High Complexity", DueDate = today.AddDays(1), Complexity = 5.0 },
                new TaskItem { Title = "D: No Due Date, High Complexity", Complexity = 5.0 },
                new TaskItem { Title = "B: Due Tomorrow, Low Complexity", DueDate = today.AddDays(1), Complexity = 1.0 },
                new TaskItem { Title = "E: Due Today", DueDate = today, Complexity = 3.0 }
            };
            context.SharedState["Tasks"] = tasks;

            // Act
            var resultContext = _agent.Act(context);
            var resultTasks = resultContext.SharedState["Tasks"] as List<TaskItem>;
            var resultTitles = resultTasks?.Select(t => t.Title).ToList();

            // Assert
            Assert.NotNull(resultTitles);
            var expectedOrder = new List<string>
            {
                "E: Due Today",
                "A: Due Tomorrow, High Complexity",
                "B: Due Tomorrow, Low Complexity",
                "D: No Due Date, High Complexity",
                "C: No Due Date, Low Complexity"
            };
            Assert.Equal(expectedOrder, resultTitles);
        }
    }
}

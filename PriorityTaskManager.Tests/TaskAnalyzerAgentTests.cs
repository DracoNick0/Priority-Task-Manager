using Xunit;
using PriorityTaskManager.Models;
using PriorityTaskManager.MCP;
using PriorityTaskManager.MCP.Agents;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PriorityTaskManager.Tests
{
    public class TaskAnalyzerAgentTests
    {
        /*
         * === Test Coverage for TaskAnalyzerAgent ===
         * [✓] Context Handling: Agent returns gracefully if 'Tasks' key is missing or not a valid task list.
         * [✓] No Defaults Needed: A fully defined task is not modified.
         * [✓] All Defaults Needed: A task with zero/default values is correctly updated.
         * [✓] Partial Defaults: A mix of tasks, some needing defaults and some not, are all handled correctly.
         * [✓] Edge Case (Complexity): A task with negative complexity is correctly defaulted.
         * [✓] Edge Case (Duration): A task with negative or zero duration is correctly defaulted.
         * [✓] Edge Case (Empty List): An empty list of tasks is handled without error.
         * [✓] Zero Importance: A task with importance explicitly set to 0 is correctly defaulted.
        */

        private readonly TaskAnalyzerAgent _agent;

        public TaskAnalyzerAgentTests()
        {
            _agent = new TaskAnalyzerAgent();
        }

        [Fact]
        public void Act_WhenContextHasNoTasks_ShouldReturnContextWithHistory()
        {
            // Arrange
            var context = new MCPContext();

            // Act
            var resultContext = _agent.Act(context);

            // Assert
            Assert.Contains("TaskAnalyzerAgent: No valid task list found in context.", resultContext.History);
            Assert.False(resultContext.SharedState.ContainsKey("Tasks"));
        }

        [Fact]
        public void Act_WhenTasksListIsEmpty_ShouldReturnContextWithHistory()
        {
            // Arrange
            var context = new MCPContext();
            context.SharedState["Tasks"] = new List<TaskItem>();

            // Act
            var resultContext = _agent.Act(context);
            var resultTasks = resultContext.SharedState["Tasks"] as List<TaskItem>;

            // Assert
            Assert.NotNull(resultTasks);
            Assert.Empty(resultTasks);
            Assert.Contains("TaskAnalyzerAgent: Tasks analyzed and defaults applied.", resultContext.History);
        }

        [Fact]
        public void Act_WhenTaskIsFullyDefined_ShouldNotChangeTask()
        {
            // Arrange
            var task = new TaskItem
            {
                Title = "Fully Defined Task",
                Importance = 5,
                EstimatedDuration = TimeSpan.FromMinutes(30),
                Complexity = 2.5
            };
            var context = new MCPContext();
            context.SharedState["Tasks"] = new List<TaskItem> { task };

            // Act
            var resultContext = _agent.Act(context);
            var resultTask = (resultContext.SharedState["Tasks"] as List<TaskItem>)?.FirstOrDefault();

            // Assert
            Assert.NotNull(resultTask);
            Assert.Equal(5, resultTask.Importance);
            Assert.Equal(TimeSpan.FromMinutes(30), resultTask.EstimatedDuration);
            Assert.Equal(2.5, resultTask.Complexity);
        }

        [Fact]
        public void Act_WhenTaskHasDefaultValues_ShouldApplyNewDefaults()
        {
            // Arrange
            var task = new TaskItem
            {
                Title = "Default Task",
                Importance = 0, // Should become 1
                EstimatedDuration = TimeSpan.Zero, // Should become 1 hour
                Complexity = 0.0 // Should become 1.0
            };
            var context = new MCPContext();
            context.SharedState["Tasks"] = new List<TaskItem> { task };

            // Act
            var resultContext = _agent.Act(context);
            var resultTask = (resultContext.SharedState["Tasks"] as List<TaskItem>)?.FirstOrDefault();

            // Assert
            Assert.NotNull(resultTask);
            Assert.Equal(1, resultTask.Importance);
            Assert.Equal(TimeSpan.FromHours(1), resultTask.EstimatedDuration);
            Assert.Equal(1.0, resultTask.Complexity);
        }

        [Fact]
        public void Act_WhenTaskHasZeroImportance_ShouldApplyDefault()
        {
            // Arrange
            var task = new TaskItem
            {
                Title = "Zero Importance Task",
                Importance = 0 // Should become 1
            };
            var context = new MCPContext();
            context.SharedState["Tasks"] = new List<TaskItem> { task };

            // Act
            var resultContext = _agent.Act(context);
            var resultTask = (resultContext.SharedState["Tasks"] as List<TaskItem>)?.FirstOrDefault();

            // Assert
            Assert.NotNull(resultTask);
            Assert.Equal(1, resultTask.Importance);
        }

        [Fact]
        public void Act_WhenTaskHasNegativeDuration_ShouldApplyDefault()
        {
            // Arrange
            var task = new TaskItem
            {
                Title = "Negative Duration Task",
                EstimatedDuration = TimeSpan.FromMinutes(-30) // Should become 1 hour
            };
            var context = new MCPContext();
            context.SharedState["Tasks"] = new List<TaskItem> { task };

            // Act
            var resultContext = _agent.Act(context);
            var resultTask = (resultContext.SharedState["Tasks"] as List<TaskItem>)?.FirstOrDefault();

            // Assert
            Assert.NotNull(resultTask);
            Assert.Equal(TimeSpan.FromHours(1), resultTask.EstimatedDuration);
        }

        [Fact]
        public void Act_WhenTaskHasNegativeComplexity_ShouldApplyDefault()
        {
            // Arrange
            var task = new TaskItem
            {
                Title = "Negative Complexity Task",
                Complexity = -10.0 // Should become 1.0
            };
            var context = new MCPContext();
            context.SharedState["Tasks"] = new List<TaskItem> { task };

            // Act
            var resultContext = _agent.Act(context);
            var resultTask = (resultContext.SharedState["Tasks"] as List<TaskItem>)?.FirstOrDefault();

            // Assert
            Assert.NotNull(resultTask);
            Assert.Equal(1.0, resultTask.Complexity);
        }

        [Fact]
        public void Act_WithMixedTasks_ShouldOnlyApplyDefaultsWhereNeeded()
        {
            // Arrange
            var defaultTask = new TaskItem { Title = "Default" }; // This will have constructor defaults: Importance=5, Duration=1hr, Complexity=1.0
            var zeroValueTask = new TaskItem { Title = "Zeroes", Importance = 0, EstimatedDuration = TimeSpan.Zero, Complexity = -1 };
            var definedTask = new TaskItem { Title = "Defined", Importance = 3, EstimatedDuration = TimeSpan.FromDays(1), Complexity = 5 };
            var context = new MCPContext();
            context.SharedState["Tasks"] = new List<TaskItem> { defaultTask, zeroValueTask, definedTask };

            // Act
            var resultContext = _agent.Act(context);
            var resultTasks = resultContext.SharedState["Tasks"] as List<TaskItem>;
            var resultDefaultTask = resultTasks?.FirstOrDefault(t => t.Title == "Default");
            var resultZeroTask = resultTasks?.FirstOrDefault(t => t.Title == "Zeroes");
            var resultDefinedTask = resultTasks?.FirstOrDefault(t => t.Title == "Defined");

            // Assert
            // The constructor-defaulted task should NOT be changed by the agent, as its values are not 0.
            Assert.NotNull(resultDefaultTask);
            Assert.Equal(5, resultDefaultTask.Importance);
            Assert.Equal(TimeSpan.FromHours(1), resultDefaultTask.EstimatedDuration);
            Assert.Equal(1.0, resultDefaultTask.Complexity);

            // The zero-value task SHOULD be changed by the agent.
            Assert.NotNull(resultZeroTask);
            Assert.Equal(1, resultZeroTask.Importance);
            Assert.Equal(TimeSpan.FromHours(1), resultZeroTask.EstimatedDuration);
            Assert.Equal(1.0, resultZeroTask.Complexity);

            // The fully-defined task should not be changed.
            Assert.NotNull(resultDefinedTask);
            Assert.Equal(3, resultDefinedTask.Importance);
            Assert.Equal(TimeSpan.FromDays(1), resultDefinedTask.EstimatedDuration);
            Assert.Equal(5, resultDefinedTask.Complexity);
        }

        [Fact]
        public void Act_WhenContextTasksIsNotAList_ShouldReturnContextWithHistory()
        {
            // Arrange
            var context = new MCPContext();
            context.SharedState["Tasks"] = "this is not a list";

            // Act
            var resultContext = _agent.Act(context);

            // Assert
            Assert.Contains("TaskAnalyzerAgent: No valid task list found in context.", resultContext.History);
            Assert.False(resultContext.SharedState.ContainsKey("Tasks"));
        }
    }
}

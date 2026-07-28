using PriorityTaskManager.Models;
using PriorityTaskManager.Services.Helpers;

namespace PriorityTaskManager.Tests.Services
{
    public class DependencyGraphHelperTests
    {
        private readonly DependencyGraphHelper _helper = new DependencyGraphHelper();

        [Fact]
        public void WouldCreateCycle_ReturnsFalse_WhenNoDependenciesExist()
        {
            var tasks = new List<TaskItem>
            {
                new TaskItem { Id = 1, Title = "A" },
                new TaskItem { Id = 2, Title = "B" }
            };

            var result = _helper.WouldCreateCycle(tasks, taskId: 1, newDependencies: new List<int> { 2 });

            Assert.False(result);
        }

        [Fact]
        public void WouldCreateCycle_ReturnsTrue_WhenDirectSelfDependency()
        {
            var tasks = new List<TaskItem>
            {
                new TaskItem { Id = 1, Title = "A" }
            };

            var result = _helper.WouldCreateCycle(tasks, taskId: 1, newDependencies: new List<int> { 1 });

            Assert.True(result);
        }

        [Fact]
        public void WouldCreateCycle_ReturnsTrue_WhenTransitiveCycleWouldFormAcrossChain()
        {
            // Task 2 already depends on Task 1. Making Task 1 depend on Task 2 creates a cycle: 1 -> 2 -> 1.
            var tasks = new List<TaskItem>
            {
                new TaskItem { Id = 1, Title = "A" },
                new TaskItem { Id = 2, Title = "B", Dependencies = new List<int> { 1 } }
            };

            var result = _helper.WouldCreateCycle(tasks, taskId: 1, newDependencies: new List<int> { 2 });

            Assert.True(result);
        }

        [Fact]
        public void WouldCreateCycle_ReturnsFalse_ForValidLinearChain()
        {
            // Task 2 already depends on Task 1. Adding an unrelated Task 3 -> Task 1 dependency is not a cycle.
            var tasks = new List<TaskItem>
            {
                new TaskItem { Id = 1, Title = "A" },
                new TaskItem { Id = 2, Title = "B", Dependencies = new List<int> { 1 } },
                new TaskItem { Id = 3, Title = "C" }
            };

            var result = _helper.WouldCreateCycle(tasks, taskId: 3, newDependencies: new List<int> { 1 });

            Assert.False(result);
        }
    }
}

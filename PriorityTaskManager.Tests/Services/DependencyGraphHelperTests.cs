using PriorityTaskManager.Models;
using PriorityTaskManager.Services.Helpers;

namespace PriorityTaskManager.Tests.Services
{
    public class DependencyGraphHelperTests
    {
        private readonly DependencyGraphHelper _helper = new DependencyGraphHelper();

        /// <summary>
        /// Deterministically derives a <see cref="Guid"/> from a small integer seed so tests can keep
        /// using short, readable literal IDs while satisfying the Guid-based identity model.
        /// </summary>
        private static Guid Id(int seed) => new Guid(seed, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);

        [Fact]
        public void WouldCreateCycle_ReturnsFalse_WhenNoDependenciesExist()
        {
            var tasks = new List<TaskItem>
            {
                new TaskItem { Id = Id(1), Title = "A" },
                new TaskItem { Id = Id(2), Title = "B" }
            };

            var result = _helper.WouldCreateCycle(tasks, taskId: Id(1), newDependencies: new List<Guid> { Id(2) });

            Assert.False(result);
        }

        [Fact]
        public void WouldCreateCycle_ReturnsTrue_WhenDirectSelfDependency()
        {
            var tasks = new List<TaskItem>
            {
                new TaskItem { Id = Id(1), Title = "A" }
            };

            var result = _helper.WouldCreateCycle(tasks, taskId: Id(1), newDependencies: new List<Guid> { Id(1) });

            Assert.True(result);
        }

        [Fact]
        public void WouldCreateCycle_ReturnsTrue_WhenTransitiveCycleWouldFormAcrossChain()
        {
            // Task 2 already depends on Task 1. Making Task 1 depend on Task 2 creates a cycle: 1 -> 2 -> 1.
            var tasks = new List<TaskItem>
            {
                new TaskItem { Id = Id(1), Title = "A" },
                new TaskItem { Id = Id(2), Title = "B", Dependencies = new List<Guid> { Id(1) } }
            };

            var result = _helper.WouldCreateCycle(tasks, taskId: Id(1), newDependencies: new List<Guid> { Id(2) });

            Assert.True(result);
        }

        [Fact]
        public void WouldCreateCycle_ReturnsFalse_ForValidLinearChain()
        {
            // Task 2 already depends on Task 1. Adding an unrelated Task 3 -> Task 1 dependency is not a cycle.
            var tasks = new List<TaskItem>
            {
                new TaskItem { Id = Id(1), Title = "A" },
                new TaskItem { Id = Id(2), Title = "B", Dependencies = new List<Guid> { Id(1) } },
                new TaskItem { Id = Id(3), Title = "C" }
            };

            var result = _helper.WouldCreateCycle(tasks, taskId: Id(3), newDependencies: new List<Guid> { Id(1) });

            Assert.False(result);
        }
    }
}

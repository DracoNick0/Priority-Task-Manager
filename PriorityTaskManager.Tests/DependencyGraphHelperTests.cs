using System.Collections.Generic;
using Xunit;
using PriorityTaskManager.Models;
using PriorityTaskManager.Services.Helpers;

namespace PriorityTaskManager.Tests
{
    public class DependencyGraphHelperTests
    {
        [Fact]
        public void GetFullChain_ShouldReturnAllConnectedTasks()
        {
            // Arrange
            var task1 = new TaskItem { Id = 1 };
            var task2 = new TaskItem { Id = 2, Dependencies = new List<int> { 1 } };
            var task3 = new TaskItem { Id = 3, Dependencies = new List<int> { 2 } };
            var task4 = new TaskItem { Id = 4 };
            var task5 = new TaskItem { Id = 5, Dependencies = new List<int> { 1 } };
            var allTasks = new List<TaskItem> { task1, task2, task3, task4, task5 };

            var helper = new DependencyGraphHelper();

            // Act
            var chain = helper.GetFullChain(allTasks, 2);

            // Assert
            Assert.NotNull(chain);
            Assert.Equal(4, chain.Count);
            Assert.Contains(chain, t => t.Id == 1);
            Assert.Contains(chain, t => t.Id == 2);
            Assert.Contains(chain, t => t.Id == 3);
            Assert.Contains(chain, t => t.Id == 5);
            Assert.DoesNotContain(chain, t => t.Id == 4);
        }
    }
}

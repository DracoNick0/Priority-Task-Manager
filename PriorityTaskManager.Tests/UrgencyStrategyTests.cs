using System;
using Xunit;
using PriorityTaskManager.Services;

namespace PriorityTaskManager.Tests
{
    public class UrgencyStrategyTests
    {
        [Fact]
        public void SingleAgentStrategy_Should_ImplementIUrgencyStrategy()
        {
            // Arrange
            var urgencyService = new SingleAgentStrategy();

            // Act & Assert
            Assert.IsAssignableFrom<IUrgencyStrategy>(urgencyService);
        }

        [Fact]
        public void MultiAgentUrgencyStrategy_Should_ImplementIUrgencyStrategy()
        {
            // Arrange
            var multiAgentStrategy = new MultiAgentUrgencyStrategy(new PriorityTaskManager.Models.UserProfile());

            // Act & Assert
            Assert.IsAssignableFrom<IUrgencyStrategy>(multiAgentStrategy);
        }
    }
}
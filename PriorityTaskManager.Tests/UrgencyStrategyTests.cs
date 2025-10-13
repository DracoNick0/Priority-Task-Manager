using System;
using Xunit;
using PriorityTaskManager.Services;

namespace PriorityTaskManager.Tests
{
    public class UrgencyStrategyTests
    {
        [Fact]
        public void UrgencyService_Should_ImplementIUrgencyStrategy()
        {
            // Arrange
            var urgencyService = new UrgencyService();

            // Act & Assert
            Assert.IsAssignableFrom<IUrgencyStrategy>(urgencyService);
        }
    }
}
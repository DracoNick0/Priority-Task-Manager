using System;
using System.Collections.Generic;
using PriorityTaskManager.Models;
using PriorityTaskManager.Services;
using Xunit;

namespace PriorityTaskManager.Tests
{
	public class MultiAgentUrgencyStrategyTests
	{
		[Fact]
		public void CalculateUrgency_ReturnsTasks_Unchanged()
		{
			// Arrange
			var userProfile = new UserProfile();
			var strategy = new MultiAgentUrgencyStrategy(userProfile);
			var tasks = new List<TaskItem> { new TaskItem { Title = "Test" } };

			// Act
			var result = strategy.CalculateUrgency(tasks);

			// Assert
			Assert.Equal(tasks, result);
		}
	}
}

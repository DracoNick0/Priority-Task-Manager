using System;
using System.Collections.Generic;
using PriorityTaskManager.Models;
using PriorityTaskManager.MCP;
using PriorityTaskManager.Services.Agents;
using Xunit;

namespace PriorityTaskManager.Tests
{
    public class SchedulePreProcessorAgentTests
    {
        [Fact]
        public void Act_ShouldCreateScheduleWindow_FromUserProfile()
        {
            // Arrange
            var userProfile = new UserProfile
            {
                WorkStartTime = new TimeOnly(9, 0),
                WorkEndTime = new TimeOnly(12, 0),
                WorkDays = new List<DayOfWeek> { DayOfWeek.Monday, DayOfWeek.Wednesday }
            };
            var context = new MCPContext();
            context.SharedState["UserProfile"] = userProfile;
            var agent = new SchedulePreProcessorAgent();

            // Act
            agent.Act(context);

            // Assert
            Assert.True(context.SharedState.ContainsKey("AvailableScheduleWindow"));
            var scheduleWindow = context.SharedState["AvailableScheduleWindow"] as ScheduleWindow;
            Assert.NotNull(scheduleWindow);
            Assert.NotNull(scheduleWindow.AvailableSlots);
            Assert.Equal(2, scheduleWindow.AvailableSlots.Count);

            // Check the first slot is for the next Monday
            var now = DateTime.Now;
            var nextMonday = now.Date.AddDays(((int)DayOfWeek.Monday - (int)now.DayOfWeek + 7) % 7);
            var expectedStart = nextMonday.Add(userProfile.WorkStartTime.ToTimeSpan());
            var expectedEnd = nextMonday.Add(userProfile.WorkEndTime.ToTimeSpan());
            Assert.Equal(expectedStart, scheduleWindow.AvailableSlots[0].StartTime);
            Assert.Equal(expectedEnd, scheduleWindow.AvailableSlots[0].EndTime);
        }
    }
}

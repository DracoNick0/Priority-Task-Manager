using System;
using System.IO;
using System.Text.Json;
using PriorityTaskManager.Models;
using PriorityTaskManager.Services;
using Xunit;

namespace PriorityTaskManager.Tests
{
    public class UserProfileTests : IDisposable
    {
        [Fact]
        public void ShouldCorrectlyPersistSchedulingPreferences()
        {
            // Arrange
            var mockPersistence = new MockPersistenceService();
            var service = new TaskManagerService(new SingleAgentStrategy(), mockPersistence);
            var updatedProfile = new UserProfile
            {
                WorkStartTime = new TimeOnly(8, 30),
                WorkEndTime = new TimeOnly(18, 0),
                WorkDays = new List<DayOfWeek> { DayOfWeek.Saturday, DayOfWeek.Sunday },
                DesiredBreatherDuration = TimeSpan.FromMinutes(30)
            };

            // Act
            service.UpdateUserProfile(updatedProfile);
            // Simulate reloading from persistence
            var reloadedService = new TaskManagerService(new SingleAgentStrategy(), mockPersistence);
            var loadedProfile = reloadedService.UserProfile;

            // Assert
            Assert.Equal(new TimeOnly(8, 30), loadedProfile.WorkStartTime);
            Assert.Equal(new TimeOnly(18, 0), loadedProfile.WorkEndTime);
            Assert.Equal(new List<DayOfWeek> { DayOfWeek.Saturday, DayOfWeek.Sunday }, loadedProfile.WorkDays);
            Assert.Equal(TimeSpan.FromMinutes(30), loadedProfile.DesiredBreatherDuration);
        }

        public UserProfileTests() { }
        public void Dispose() { }

        [Fact]
        public void TaskManagerService_ShouldCreateDefaultProfile_WhenNoProfileExists()
        {
            var mockPersistence = new MockPersistenceService();
            var service = new TaskManagerService(new SingleAgentStrategy(), mockPersistence);
            var profile = service.UserProfile;
            Assert.NotNull(profile);
            Assert.Equal(UrgencyMode.SingleAgent, profile.ActiveUrgencyMode);
        }

        [Fact]
        public void TaskManagerService_ShouldLoadExistingProfile_WhenProfileExists()
        {
            var initialData = new DataContainer { UserProfile = new UserProfile { ActiveUrgencyMode = UrgencyMode.MultiAgent } };
            var mockPersistence = new MockPersistenceService(initialData);
            var service = new TaskManagerService(new SingleAgentStrategy(), mockPersistence);
            var loadedProfile = service.UserProfile;
            Assert.Equal(UrgencyMode.MultiAgent, loadedProfile.ActiveUrgencyMode);
        }
    }
}

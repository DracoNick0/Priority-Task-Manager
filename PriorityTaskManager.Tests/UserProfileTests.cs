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

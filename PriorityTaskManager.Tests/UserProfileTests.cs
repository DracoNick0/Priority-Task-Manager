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
        private readonly string _profileFilePath = "user_profile.json";

        public UserProfileTests()
        {
            // Cleanup before each test
            if (File.Exists(_profileFilePath))
                File.Delete(_profileFilePath);
        }

        public void Dispose()
        {
            // Cleanup after each test
            if (File.Exists(_profileFilePath))
                File.Delete(_profileFilePath);
        }

        [Fact]
        public void TaskManagerService_ShouldCreateDefaultProfile_WhenFileDoesNotExist()
        {
            // Arrange
            Assert.False(File.Exists(_profileFilePath));

            // Act
            var service = new TaskManagerService(new SingleAgentStrategy());

            // Assert
            Assert.True(File.Exists(_profileFilePath));
            var json = File.ReadAllText(_profileFilePath);
            var profile = JsonSerializer.Deserialize<UserProfile>(json);
            Assert.NotNull(profile);
            Assert.Equal(UrgencyMode.SingleAgent, profile!.ActiveUrgencyMode);
        }

        [Fact]
        public void TaskManagerService_ShouldLoadExistingProfile_WhenFileExists()
        {
            // Arrange
            var profile = new UserProfile { ActiveUrgencyMode = UrgencyMode.MultiAgent };
            var json = JsonSerializer.Serialize(profile);
            File.WriteAllText(_profileFilePath, json);

            // Act
            var service = new TaskManagerService(new SingleAgentStrategy());

            // Assert
            var loadedProfile = service.UserProfile;
            Assert.Equal(UrgencyMode.MultiAgent, loadedProfile.ActiveUrgencyMode);
        }

    // No longer needed: public getter is now available.
    }
}

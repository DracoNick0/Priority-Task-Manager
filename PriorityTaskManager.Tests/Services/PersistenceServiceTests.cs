using PriorityTaskManager.Models;
using PriorityTaskManager.Services;

namespace PriorityTaskManager.Tests.Services
{
    public class PersistenceServiceTests
    {
        [Fact]
        public void LoadData_WhenFilesDoNotExist_ShouldReturnDefaultContainer()
        {
            var tempDir = CreateTempDirectory();
            try
            {
                var service = new PersistenceService(tempDir);

                var data = service.LoadData();

                Assert.NotNull(data);
                Assert.Empty(data.Tasks);
                Assert.Empty(data.Lists);
                Assert.Empty(data.Events);
                Assert.NotNull(data.UserProfile);
                Assert.Equal(1, data.NextTaskId);
                Assert.Equal(1, data.NextDisplayId);
                Assert.Equal(1, data.NextListId);
                Assert.Equal(1, data.NextEventId);
            }
            finally
            {
                DeleteDirectory(tempDir);
            }
        }

        [Fact]
        public void SaveData_ThenLoadData_ShouldRoundTripCoreState()
        {
            var tempDir = CreateTempDirectory();
            try
            {
                var service = new PersistenceService(tempDir);
                var container = new DataContainer
                {
                    NextTaskId = 42,
                    NextDisplayId = 77,
                    NextListId = 6,
                    NextEventId = 9,
                    ActiveListId = 3,
                    UserProfile = new UserProfile
                    {
                        WorkStartTime = new TimeOnly(8, 0),
                        WorkEndTime = new TimeOnly(16, 30)
                    },
                    Lists = new List<TaskList>
                    {
                        new TaskList { Id = 3, Name = "Work" }
                    },
                    Tasks = new List<TaskItem>
                    {
                        new TaskItem
                        {
                            Id = 10,
                            DisplayId = 1,
                            Title = "Write tests",
                            ListId = 3,
                            EstimatedDuration = TimeSpan.FromHours(2),
                            Importance = 4,
                            Complexity = 2.5,
                            DueDate = new DateTime(2026, 7, 20, 12, 0, 0)
                        }
                    },
                    Events = new List<Event>
                    {
                        new Event
                        {
                            Id = 2,
                            Name = "Doctor",
                            StartTime = new DateTime(2026, 7, 10, 10, 0, 0),
                            EndTime = new DateTime(2026, 7, 10, 11, 0, 0)
                        }
                    }
                };

                service.SaveData(container);
                var loaded = service.LoadData();

                Assert.Single(loaded.Lists);
                Assert.Single(loaded.Tasks);
                Assert.Single(loaded.Events);
                Assert.Equal(42, loaded.NextTaskId);
                Assert.Equal(77, loaded.NextDisplayId);
                Assert.Equal(6, loaded.NextListId);
                Assert.Equal(9, loaded.NextEventId);
                Assert.Equal("Work", loaded.Lists[0].Name);
                Assert.Equal("Write tests", loaded.Tasks[0].Title);
                Assert.Equal("Doctor", loaded.Events[0].Name);
                Assert.Equal(new TimeOnly(8, 0), loaded.UserProfile.WorkStartTime);
                Assert.Equal(new TimeOnly(16, 30), loaded.UserProfile.WorkEndTime);
            }
            finally
            {
                DeleteDirectory(tempDir);
            }
        }

        [Fact]
        public void LoadData_WhenFilesContainMalformedJson_ShouldFailSoftToDefaults()
        {
            var tempDir = CreateTempDirectory();
            try
            {
                File.WriteAllText(Path.Combine(tempDir, "tasks.json"), "{ malformed");
                File.WriteAllText(Path.Combine(tempDir, "lists.json"), "{ malformed");
                File.WriteAllText(Path.Combine(tempDir, "events.json"), "{ malformed");
                File.WriteAllText(Path.Combine(tempDir, "user_profile.json"), "{ malformed");

                var service = new PersistenceService(tempDir);

                var data = service.LoadData();

                Assert.NotNull(data);
                Assert.Empty(data.Tasks);
                Assert.Empty(data.Lists);
                Assert.Empty(data.Events);
                Assert.NotNull(data.UserProfile);
            }
            finally
            {
                DeleteDirectory(tempDir);
            }
        }

        private static string CreateTempDirectory()
        {
            var path = Path.Combine(Path.GetTempPath(), "ptm-tests-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(path);
            return path;
        }

        private static void DeleteDirectory(string path)
        {
            if (Directory.Exists(path))
            {
                Directory.Delete(path, recursive: true);
            }
        }
    }
}

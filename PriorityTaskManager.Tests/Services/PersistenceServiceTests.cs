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
                Assert.Equal(1, data.NextDisplayId);
                Assert.Empty(data.LoadWarnings);
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
                var listId = Guid.NewGuid();
                var taskId = Guid.NewGuid();
                var eventId = Guid.NewGuid();
                var container = new DataContainer
                {
                    NextDisplayId = 77,
                    ActiveListId = listId,
                    UserProfile = new UserProfile
                    {
                        WorkStartTime = new TimeOnly(8, 0),
                        WorkEndTime = new TimeOnly(16, 30)
                    },
                    Lists = new List<TaskList>
                    {
                        new TaskList { Id = listId, Name = "Work" }
                    },
                    Tasks = new List<TaskItem>
                    {
                        new TaskItem
                        {
                            Id = taskId,
                            DisplayId = 1,
                            Title = "Write tests",
                            ListId = listId,
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
                            Id = eventId,
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
                Assert.Equal(77, loaded.NextDisplayId);
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
                Assert.Equal(4, data.LoadWarnings.Count);
                Assert.Contains(data.LoadWarnings, w => w.Contains("tasks.json"));
                Assert.Contains(data.LoadWarnings, w => w.Contains("lists.json"));
                Assert.Contains(data.LoadWarnings, w => w.Contains("events.json"));
                Assert.Contains(data.LoadWarnings, w => w.Contains("user_profile.json"));
            }
            finally
            {
                DeleteDirectory(tempDir);
            }
        }

        [Fact]
        public void ArchiveTasks_AppendsArchivedTasksToArchiveFile()
        {
            var tempDir = CreateTempDirectory();
            var archivePath = Path.Combine(Directory.GetCurrentDirectory(), "archive.json");
            var archiveExistedBefore = File.Exists(archivePath);
            var backupPath = archivePath + ".bak";
            if (archiveExistedBefore)
            {
                File.Move(archivePath, backupPath, overwrite: true);
            }
            try
            {
                var service = new PersistenceService(tempDir);
                var task = new TaskItem { Id = Guid.NewGuid(), DisplayId = 1, Title = "Archived Task", ListId = Guid.NewGuid() };

                service.ArchiveTasks(new List<TaskItem> { task });

                Assert.True(File.Exists(archivePath));
                var archived = System.Text.Json.JsonSerializer.Deserialize<List<TaskItem>>(File.ReadAllText(archivePath));
                Assert.Single(archived!);
                Assert.Equal("Archived Task", archived![0].Title);
            }
            finally
            {
                if (File.Exists(archivePath))
                {
                    File.Delete(archivePath);
                }
                if (archiveExistedBefore)
                {
                    File.Move(backupPath, archivePath, overwrite: true);
                }
                DeleteDirectory(tempDir);
            }
        }

        [Fact]
        public void LoadData_WhenOnlyOneFileIsCorrupt_ShouldResetOnlyThatFileAndKeepOthersValid()
        {
            var tempDir = CreateTempDirectory();
            try
            {
                var service = new PersistenceService(tempDir);
                var listId = Guid.NewGuid();
                var eventId = Guid.NewGuid();
                var container = new DataContainer
                {
                    NextDisplayId = 5,
                    Lists = new List<TaskList>
                    {
                        new TaskList { Id = listId, Name = "Home" }
                    },
                    Events = new List<Event>
                    {
                        new Event
                        {
                            Id = eventId,
                            Name = "Meeting",
                            StartTime = new DateTime(2026, 7, 10, 9, 0, 0),
                            EndTime = new DateTime(2026, 7, 10, 10, 0, 0)
                        }
                    },
                    UserProfile = new UserProfile { WorkStartTime = new TimeOnly(9, 0) }
                };
                service.SaveData(container);

                // Corrupt only the tasks file; lists/events/profile remain valid on disk.
                File.WriteAllText(Path.Combine(tempDir, "tasks.json"), "{ not valid json");

                var data = service.LoadData();

                Assert.Empty(data.Tasks);
                Assert.Single(data.LoadWarnings);
                Assert.Contains("tasks.json", data.LoadWarnings[0]);
                Assert.Single(data.Lists);
                Assert.Equal("Home", data.Lists[0].Name);
                Assert.Single(data.Events);
                Assert.Equal("Meeting", data.Events[0].Name);
                Assert.Equal(new TimeOnly(9, 0), data.UserProfile.WorkStartTime);
            }
            finally
            {
                DeleteDirectory(tempDir);
            }
        }

        [Fact]
        public void SaveData_WritesThroughTemporaryFile_SoInterruptedSaveLeavesPreviousDataIntact()
        {
            var tempDir = CreateTempDirectory();
            try
            {
                var service = new PersistenceService(tempDir);
                var original = new DataContainer
                {
                    NextDisplayId = 2,
                    Tasks = new List<TaskItem>
                    {
                        new TaskItem { Id = Guid.NewGuid(), DisplayId = 1, Title = "Original task", ListId = Guid.NewGuid() }
                    }
                };
                service.SaveData(original);

                // Simulate a save that was interrupted after the temp file was written but
                // before it was swapped into place: leave a stale/incomplete .tmp file behind.
                var tasksTempPath = Path.Combine(tempDir, "tasks.json.tmp");
                File.WriteAllText(tasksTempPath, "{ incomplete");

                var data = service.LoadData();

                Assert.Single(data.Tasks);
                Assert.Equal("Original task", data.Tasks[0].Title);
                Assert.Empty(data.LoadWarnings);
                Assert.True(File.Exists(tasksTempPath), "Stray temp file should be left untouched by LoadData.");
            }
            finally
            {
                DeleteDirectory(tempDir);
            }
        }

        [Fact]
        public void LoadData_WhenListsFileHasLegacyIntIds_ShouldMigrateToGuidAndWarn()
        {
            var tempDir = CreateTempDirectory();
            try
            {
                File.WriteAllText(Path.Combine(tempDir, "lists.json"),
                    "{\"Lists\":[{\"Id\":1,\"Name\":\"Home\",\"SortOption\":0}],\"NextListId\":2}");

                var service = new PersistenceService(tempDir);
                var data = service.LoadData();

                Assert.Single(data.Lists);
                Assert.NotEqual(Guid.Empty, data.Lists[0].Id);
                Assert.Equal("Home", data.Lists[0].Name);
                Assert.Contains(data.LoadWarnings, w => w.Contains("lists.json") && w.Contains("legacy"));
            }
            finally
            {
                DeleteDirectory(tempDir);
            }
        }

        [Fact]
        public void LoadData_WhenTasksFileHasLegacyIntIds_ShouldMigrateListIdAndDependenciesConsistently()
        {
            var tempDir = CreateTempDirectory();
            try
            {
                // Legacy list with int Id 1, referenced by both tasks below via ListId.
                File.WriteAllText(Path.Combine(tempDir, "lists.json"),
                    "{\"Lists\":[{\"Id\":1,\"Name\":\"Home\",\"SortOption\":0}],\"NextListId\":2}");

                // Legacy tasks: Task 2 depends on Task 1, and both belong to legacy list Id 1.
                File.WriteAllText(Path.Combine(tempDir, "tasks.json"),
                    "{\"Tasks\":[" +
                    "{\"Id\":1,\"DisplayId\":1,\"Title\":\"Prerequisite\",\"ListId\":1,\"Dependencies\":[]}," +
                    "{\"Id\":2,\"DisplayId\":2,\"Title\":\"Dependent\",\"ListId\":1,\"Dependencies\":[1]}" +
                    "],\"NextTaskId\":3,\"NextDisplayId\":3}");

                var service = new PersistenceService(tempDir);
                var data = service.LoadData();

                Assert.Equal(2, data.Tasks.Count);
                var prerequisite = data.Tasks.Single(t => t.Title == "Prerequisite");
                var dependent = data.Tasks.Single(t => t.Title == "Dependent");

                // Ids were migrated to non-empty, distinct Guids.
                Assert.NotEqual(Guid.Empty, prerequisite.Id);
                Assert.NotEqual(Guid.Empty, dependent.Id);
                Assert.NotEqual(prerequisite.Id, dependent.Id);

                // ListId was remapped to the migrated list's new Guid, consistently for both tasks.
                var migratedList = data.Lists.Single();
                Assert.Equal(migratedList.Id, prerequisite.ListId);
                Assert.Equal(migratedList.Id, dependent.ListId);

                // The dependency reference was rewritten from the legacy int Id to the new Guid Id.
                Assert.Single(dependent.Dependencies);
                Assert.Equal(prerequisite.Id, dependent.Dependencies[0]);

                Assert.Contains(data.LoadWarnings, w => w.Contains("tasks.json") && w.Contains("legacy"));
            }
            finally
            {
                DeleteDirectory(tempDir);
            }
        }

        [Fact]
        public void LoadData_WhenEventsFileHasLegacyIntIds_ShouldMigrateToGuid()
        {
            var tempDir = CreateTempDirectory();
            try
            {
                File.WriteAllText(Path.Combine(tempDir, "events.json"),
                    "{\"Events\":[{\"Id\":1,\"Name\":\"Standup\",\"StartTime\":\"2026-01-01T09:00:00\",\"EndTime\":\"2026-01-01T09:15:00\"}],\"NextEventId\":2}");

                var service = new PersistenceService(tempDir);
                var data = service.LoadData();

                Assert.Single(data.Events);
                Assert.NotEqual(Guid.Empty, data.Events[0].Id);
                Assert.Equal("Standup", data.Events[0].Name);
                Assert.Contains(data.LoadWarnings, w => w.Contains("events.json") && w.Contains("legacy"));
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

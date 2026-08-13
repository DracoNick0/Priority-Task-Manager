using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using PriorityTaskManager.Models;

namespace PriorityTaskManager.Services
{
    /// <summary>
    /// Handles reading and writing persistent application data to disk.
    /// </summary>
    public class PersistenceService : IPersistenceService
    {
        private readonly string _tasksFilePath;
        private readonly string _listsFilePath;
        private readonly string _userProfileFilePath;
        private readonly string _eventsFilePath;
        private readonly string _archiveFilePath;

        public PersistenceService(string dataDirectory)
        {
            _tasksFilePath = Path.Combine(dataDirectory, "tasks.json");
            _listsFilePath = Path.Combine(dataDirectory, "lists.json");
            _userProfileFilePath = Path.Combine(dataDirectory, "user_profile.json");
            _eventsFilePath = Path.Combine(dataDirectory, "events.json");
            _archiveFilePath = Path.Combine(dataDirectory, "archive.json");
        }

        public DataContainer LoadData()
        {
            var data = new DataContainer();
            var listIdMap = new Dictionary<int, Guid>();
            var taskIdMap = new Dictionary<int, Guid>();

            // Load lists first: tasks reference list IDs, so the list ID map must exist before
            // tasks are loaded/migrated.
            if (File.Exists(_listsFilePath))
            {
                try
                {
                    var json = File.ReadAllText(_listsFilePath);
                    var dict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);
                    if (dict != null && dict.ContainsKey("Lists"))
                    {
                        var listsElement = dict["Lists"];
                        if (IsLegacyIntIdShape(listsElement))
                        {
                            data.Lists = MigrateLegacyLists(listsElement, listIdMap);
                            data.LoadWarnings.Add($"Migrated '{_listsFilePath}' from legacy integer list IDs to new unique identifiers.");
                        }
                        else
                        {
                            data.Lists = JsonSerializer.Deserialize<List<TaskList>>(listsElement.GetRawText()) ?? new List<TaskList>();
                        }
                    }
                }
                catch (Exception ex)
                {
                    data.LoadWarnings.Add($"Could not load '{_listsFilePath}' ({ex.GetType().Name}: {ex.Message}). Lists were reset to an empty default.");
                }
            }

            // Load tasks
            if (File.Exists(_tasksFilePath))
            {
                try
                {
                    var json = File.ReadAllText(_tasksFilePath);
                    var dict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);
                    if (dict != null && dict.ContainsKey("Tasks"))
                    {
                        var tasksElement = dict["Tasks"];
                        if (IsLegacyIntIdShape(tasksElement))
                        {
                            data.Tasks = MigrateLegacyTasks(tasksElement, listIdMap, taskIdMap);
                            data.LoadWarnings.Add($"Migrated '{_tasksFilePath}' from legacy integer task IDs to new unique identifiers.");
                        }
                        else
                        {
                            data.Tasks = JsonSerializer.Deserialize<List<TaskItem>>(tasksElement.GetRawText()) ?? new List<TaskItem>();
                        }
                        if (dict.ContainsKey("NextDisplayId"))
                            data.NextDisplayId = dict["NextDisplayId"].GetInt32();
                    }
                }
                catch (Exception ex)
                {
                    data.LoadWarnings.Add($"Could not load '{_tasksFilePath}' ({ex.GetType().Name}: {ex.Message}). Tasks were reset to an empty default.");
                }
            }

            // Load events
            if (File.Exists(_eventsFilePath))
            {
                try
                {
                    var json = File.ReadAllText(_eventsFilePath);
                    var dict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);
                    if (dict != null && dict.ContainsKey("Events"))
                    {
                        var eventsElement = dict["Events"];
                        if (IsLegacyIntIdShape(eventsElement))
                        {
                            data.Events = MigrateLegacyEvents(eventsElement);
                            data.LoadWarnings.Add($"Migrated '{_eventsFilePath}' from legacy integer event IDs to new unique identifiers.");
                        }
                        else
                        {
                            data.Events = JsonSerializer.Deserialize<List<Event>>(eventsElement.GetRawText()) ?? new List<Event>();
                        }
                    }
                }
                catch (Exception ex)
                {
                    data.LoadWarnings.Add($"Could not load '{_eventsFilePath}' ({ex.GetType().Name}: {ex.Message}). Events were reset to an empty default.");
                }
            }

            // Load user profile
            if (File.Exists(_userProfileFilePath))
            {
                try
                {
                    var json = File.ReadAllText(_userProfileFilePath);
                    data.UserProfile = JsonSerializer.Deserialize<UserProfile>(json) ?? new UserProfile();
                }
                catch (Exception ex)
                {
                    data.UserProfile = new UserProfile();
                    data.LoadWarnings.Add($"Could not load '{_userProfileFilePath}' ({ex.GetType().Name}: {ex.Message}). User profile was reset to defaults.");
                }
            }
            else
            {
                data.UserProfile = new UserProfile();
            }

            return data;
        }

        /// <summary>
        /// Determines whether a persisted JSON array of records uses the legacy shape where
        /// <c>Id</c> was a monotonic <see cref="int"/> instead of the current <see cref="Guid"/> shape.
        /// An empty array is treated as already-current shape since there is nothing to migrate.
        /// </summary>
        private static bool IsLegacyIntIdShape(JsonElement arrayElement)
        {
            if (arrayElement.ValueKind != JsonValueKind.Array)
                return false;

            foreach (var item in arrayElement.EnumerateArray())
            {
                if (item.TryGetProperty("Id", out var idProperty))
                {
                    return idProperty.ValueKind == JsonValueKind.Number;
                }
                return false;
            }

            return false;
        }

        /// <summary>
        /// Migrates a legacy int-keyed <c>Lists</c> array to the current Guid-keyed shape,
        /// recording the old-to-new ID mapping so referencing tasks can be remapped consistently.
        /// </summary>
        private static List<TaskList> MigrateLegacyLists(JsonElement listsElement, Dictionary<int, Guid> listIdMap)
        {
            var arrayNode = JsonNode.Parse(listsElement.GetRawText())!.AsArray();
            foreach (var item in arrayNode)
            {
                var obj = item!.AsObject();
                var oldId = obj["Id"]!.GetValue<int>();
                var newId = Guid.NewGuid();
                listIdMap[oldId] = newId;
                obj["Id"] = newId.ToString();
            }

            return JsonSerializer.Deserialize<List<TaskList>>(arrayNode.ToJsonString()) ?? new List<TaskList>();
        }

        /// <summary>
        /// Migrates a legacy int-keyed <c>Tasks</c> array to the current Guid-keyed shape, remapping
        /// <c>ListId</c> using <paramref name="listIdMap"/> and <c>Dependencies</c> against sibling tasks
        /// within the same file so cross-references stay consistent after migration.
        /// </summary>
        private static List<TaskItem> MigrateLegacyTasks(JsonElement tasksElement, Dictionary<int, Guid> listIdMap, Dictionary<int, Guid> taskIdMap)
        {
            var arrayNode = JsonNode.Parse(tasksElement.GetRawText())!.AsArray();

            // First pass: assign new IDs for every task so dependency remapping below can resolve
            // forward references regardless of array order.
            foreach (var item in arrayNode)
            {
                var obj = item!.AsObject();
                var oldId = obj["Id"]!.GetValue<int>();
                if (!taskIdMap.ContainsKey(oldId))
                    taskIdMap[oldId] = Guid.NewGuid();
            }

            foreach (var item in arrayNode)
            {
                var obj = item!.AsObject();
                var oldId = obj["Id"]!.GetValue<int>();
                obj["Id"] = taskIdMap[oldId].ToString();

                if (obj.TryGetPropertyValue("ListId", out var listIdNode) && listIdNode != null)
                {
                    var oldListId = listIdNode.GetValue<int>();
                    obj["ListId"] = (listIdMap.TryGetValue(oldListId, out var newListId) ? newListId : Guid.Empty).ToString();
                }

                if (obj.TryGetPropertyValue("Dependencies", out var depsNode) && depsNode is JsonArray depsArray)
                {
                    var newDeps = new JsonArray();
                    foreach (var dep in depsArray)
                    {
                        if (dep == null)
                            continue;
                        var oldDepId = dep.GetValue<int>();
                        if (taskIdMap.TryGetValue(oldDepId, out var newDepId))
                        {
                            newDeps.Add(JsonValue.Create(newDepId.ToString()));
                        }
                    }
                    obj["Dependencies"] = newDeps;
                }
            }

            return JsonSerializer.Deserialize<List<TaskItem>>(arrayNode.ToJsonString()) ?? new List<TaskItem>();
        }

        /// <summary>
        /// Migrates a legacy int-keyed <c>Events</c> array to the current Guid-keyed shape.
        /// Events have no cross-file references, so no ID map is required.
        /// </summary>
        private static List<Event> MigrateLegacyEvents(JsonElement eventsElement)
        {
            var arrayNode = JsonNode.Parse(eventsElement.GetRawText())!.AsArray();
            foreach (var item in arrayNode)
            {
                var obj = item!.AsObject();
                obj["Id"] = Guid.NewGuid().ToString();
            }

            return JsonSerializer.Deserialize<List<Event>>(arrayNode.ToJsonString()) ?? new List<Event>();
        }

        public void SaveData(DataContainer data)
        {
            // Ensure data directory exists for all files
            Directory.CreateDirectory(Path.GetDirectoryName(_tasksFilePath) ?? ".");
            Directory.CreateDirectory(Path.GetDirectoryName(_listsFilePath) ?? ".");
            Directory.CreateDirectory(Path.GetDirectoryName(_eventsFilePath) ?? ".");
            Directory.CreateDirectory(Path.GetDirectoryName(_userProfileFilePath) ?? ".");

            // Save tasks
            var tasksData = new
            {
                Tasks = data.Tasks,
                NextDisplayId = data.NextDisplayId
            };
            WriteAtomic(_tasksFilePath, JsonSerializer.Serialize(tasksData));

            // Save lists
            var listsData = new
            {
                Lists = data.Lists
            };
            WriteAtomic(_listsFilePath, JsonSerializer.Serialize(listsData));

            // Save events
            var eventsData = new
            {
                Events = data.Events
            };
            WriteAtomic(_eventsFilePath, JsonSerializer.Serialize(eventsData));

            // Save user profile
            WriteAtomic(_userProfileFilePath, JsonSerializer.Serialize(data.UserProfile));
        }


        /// <summary>
        /// Writes <paramref name="content"/> to <paramref name="filePath"/> atomically by writing
        /// to a temporary file in the same directory first, then swapping it into place, so a
        /// crash or interruption mid-write cannot leave <paramref name="filePath"/> partially written.
        /// </summary>
        /// <param name="filePath">The destination file path.</param>
        /// <param name="content">The file content to write.</param>
        private static void WriteAtomic(string filePath, string content)
        {
            var tempFilePath = filePath + ".tmp";
            File.WriteAllText(tempFilePath, content);

            // File.Replace/Move can transiently fail with IOException if another process (e.g. an
            // antivirus scanner) has the destination briefly open, so retry a few times before giving up.
            const int maxAttempts = 5;
            for (var attempt = 1; attempt <= maxAttempts; attempt++)
            {
                try
                {
                    if (File.Exists(filePath))
                    {
                        File.Replace(tempFilePath, filePath, null);
                    }
                    else
                    {
                        File.Move(tempFilePath, filePath);
                    }
                    return;
                }
                catch (IOException) when (attempt < maxAttempts)
                {
                    Thread.Sleep(50 * attempt);
                }
            }
        }

        /// <summary>
        /// Appends the given tasks to the persisted archive record.
        /// </summary>
        /// <param name="tasksToArchive">The tasks to archive.</param>
        public void ArchiveTasks(IEnumerable<TaskItem> tasksToArchive)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_archiveFilePath) ?? ".");

            List<TaskItem> archivedTasks = new List<TaskItem>();
            if (File.Exists(_archiveFilePath))
            {
                var existingData = File.ReadAllText(_archiveFilePath);
                archivedTasks = JsonSerializer.Deserialize<List<TaskItem>>(existingData) ?? new List<TaskItem>();
            }
            archivedTasks.AddRange(tasksToArchive);
            var updatedData = JsonSerializer.Serialize(archivedTasks);
            WriteAtomic(_archiveFilePath, updatedData);
        }
    }
}

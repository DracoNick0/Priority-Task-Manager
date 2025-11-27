using System;
using System.IO;
using System.Text.Json;
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

        public PersistenceService(string tasksFilePath, string listsFilePath, string userProfileFilePath, string eventsFilePath)
        {
            _tasksFilePath = Path.GetFullPath(tasksFilePath);
            _listsFilePath = Path.GetFullPath(listsFilePath);
            _userProfileFilePath = Path.GetFullPath(userProfileFilePath);
            _eventsFilePath = Path.GetFullPath(eventsFilePath);
        }

        public DataContainer LoadData()
        {
            var data = new DataContainer();

            // Load tasks
            if (File.Exists(_tasksFilePath))
            {
                try
                {
                    var json = File.ReadAllText(_tasksFilePath);
                    var dict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);
                    if (dict != null && dict.ContainsKey("Tasks"))
                    {
                        data.Tasks = JsonSerializer.Deserialize<List<TaskItem>>(dict["Tasks"].GetRawText()) ?? new List<TaskItem>();
                        if (dict.ContainsKey("NextId"))
                            data.NextTaskId = dict["NextId"].GetInt32();
                        if (dict.ContainsKey("NextDisplayId"))
                            data.NextDisplayId = dict["NextDisplayId"].GetInt32();
                    }
                }
                catch { }
            }

            // Load lists
            if (File.Exists(_listsFilePath))
            {
                try
                {
                    var json = File.ReadAllText(_listsFilePath);
                    var dict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);
                    if (dict != null && dict.ContainsKey("Lists"))
                    {
                        data.Lists = JsonSerializer.Deserialize<List<TaskList>>(dict["Lists"].GetRawText()) ?? new List<TaskList>();
                        if (dict.ContainsKey("NextListId"))
                            data.NextListId = dict["NextListId"].GetInt32();
                    }
                }
                catch { }
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
                        data.Events = JsonSerializer.Deserialize<List<Event>>(dict["Events"].GetRawText()) ?? new List<Event>();
                        if (dict.ContainsKey("NextEventId"))
                            data.NextEventId = dict["NextEventId"].GetInt32();
                    }
                }
                catch { }
            }

            // Load user profile
            if (File.Exists(_userProfileFilePath))
            {
                try
                {
                    var json = File.ReadAllText(_userProfileFilePath);
                    data.UserProfile = JsonSerializer.Deserialize<UserProfile>(json) ?? new UserProfile();
                }
                catch { data.UserProfile = new UserProfile(); }
            }
            else
            {
                data.UserProfile = new UserProfile();
            }

            return data;
        }

        public void SaveData(DataContainer data)
        {
            // Save tasks
            var tasksData = new
            {
                Tasks = data.Tasks,
                NextId = data.NextTaskId,
                NextDisplayId = data.NextDisplayId
            };
            File.WriteAllText(_tasksFilePath, JsonSerializer.Serialize(tasksData));

            // Save lists
            var listsData = new
            {
                Lists = data.Lists,
                NextListId = data.NextListId
            };
            File.WriteAllText(_listsFilePath, JsonSerializer.Serialize(listsData));

            // Save events
            var eventsData = new
            {
                Events = data.Events,
                NextEventId = data.NextEventId
            };
            File.WriteAllText(_eventsFilePath, JsonSerializer.Serialize(eventsData));

            // Save user profile
            File.WriteAllText(_userProfileFilePath, JsonSerializer.Serialize(data.UserProfile));
        }
    }
}

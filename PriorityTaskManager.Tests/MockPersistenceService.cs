using PriorityTaskManager.Models;
using PriorityTaskManager.Services;

namespace PriorityTaskManager.Tests
{
    /// <summary>
    /// In-memory mock implementation of IPersistenceService for unit testing.
    /// Allows preloading and inspection of the DataContainer.
    /// </summary>
    public class MockPersistenceService : IPersistenceService
    {
        public DataContainer Data { get; private set; }

        public MockPersistenceService()
        {
            Data = new DataContainer();
        }

        public MockPersistenceService(DataContainer initialData)
        {
            Data = initialData;
        }

        private Event CloneEvent(Event e) => new Event
        {
            Id = e.Id,
            Name = e.Name,
            StartTime = e.StartTime,
            EndTime = e.EndTime
        };

        public DataContainer LoadData()
        {
            // Return a deep copy to prevent test cross-contamination
            return new DataContainer
            {
                Tasks = new List<TaskItem>(Data.Tasks.Select(t => t.Clone())),
                Lists = new List<TaskList>(Data.Lists.Select(l => CloneList(l))),
                Events = new List<Event>(Data.Events.Select(e => CloneEvent(e))),
                NextTaskId = Data.NextTaskId,
                NextDisplayId = Data.NextDisplayId,
                NextListId = Data.NextListId,
                NextEventId = Data.NextEventId,
                UserProfile = CloneUserProfile(Data.UserProfile),
                ActiveListId = Data.ActiveListId
            };
        }

        public void SaveData(DataContainer data)
        {
            // Store a deep copy to prevent test cross-contamination
            Data = new DataContainer
            {
                Tasks = new List<TaskItem>(data.Tasks.Select(t => t.Clone())),
                Lists = new List<TaskList>(data.Lists.Select(l => CloneList(l))),
                Events = new List<Event>(data.Events.Select(e => CloneEvent(e))),
                NextTaskId = data.NextTaskId,
                NextDisplayId = data.NextDisplayId,
                NextListId = data.NextListId,
                NextEventId = data.NextEventId,
                UserProfile = CloneUserProfile(data.UserProfile),
                ActiveListId = data.ActiveListId
            };
        }

        private TaskList CloneList(TaskList l) => new TaskList
        {
            Id = l.Id,
            Name = l.Name,
            SortOption = l.SortOption
        };

        private UserProfile CloneUserProfile(UserProfile u) => new UserProfile
        {
            WorkStartTime = u.WorkStartTime,
            WorkEndTime = u.WorkEndTime,
            WorkDays = new List<DayOfWeek>(u.WorkDays),
            DesiredBreatherDuration = u.DesiredBreatherDuration
        };
    }
}

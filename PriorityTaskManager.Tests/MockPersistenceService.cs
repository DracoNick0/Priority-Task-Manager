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
        private DataContainer _data;

        public MockPersistenceService()
        {
            _data = new DataContainer();
        }

        public MockPersistenceService(DataContainer initialData)
        {
            _data = initialData;
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
                Tasks = new List<TaskItem>(_data.Tasks.Select(t => t.Clone())),
                Lists = new List<TaskList>(_data.Lists.Select(l => CloneList(l))),
                Events = new List<Event>(_data.Events.Select(e => CloneEvent(e))),
                NextTaskId = _data.NextTaskId,
                NextDisplayId = _data.NextDisplayId,
                NextListId = _data.NextListId,
                NextEventId = _data.NextEventId,
                UserProfile = CloneUserProfile(_data.UserProfile),
                ActiveListId = _data.ActiveListId
            };
        }

        public void SaveData(DataContainer data)
        {
            // Store a deep copy to prevent test cross-contamination
            _data = new DataContainer
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
            ActiveUrgencyMode = u.ActiveUrgencyMode,
            WorkStartTime = u.WorkStartTime,
            WorkEndTime = u.WorkEndTime,
            WorkDays = new List<DayOfWeek>(u.WorkDays),
            DesiredBreatherDuration = u.DesiredBreatherDuration
        };
    }
}

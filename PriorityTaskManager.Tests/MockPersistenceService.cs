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


        public DataContainer LoadData()
        {
            // Return a deep copy to prevent test cross-contamination
            return new DataContainer
            {
                Tasks = new List<TaskItem>(_data.Tasks.Select(t => t.Clone())),
                Lists = new List<TaskList>(_data.Lists.Select(l => CloneList(l))),
                NextTaskId = _data.NextTaskId,
                NextDisplayId = _data.NextDisplayId,
                NextListId = _data.NextListId,
                UserProfile = CloneUserProfile(_data.UserProfile)
            };
        }

        public void SaveData(DataContainer data)
        {
            // Store a deep copy to prevent test cross-contamination
            _data = new DataContainer
            {
                Tasks = new List<TaskItem>(data.Tasks.Select(t => t.Clone())),
                Lists = new List<TaskList>(data.Lists.Select(l => CloneList(l))),
                NextTaskId = data.NextTaskId,
                NextDisplayId = data.NextDisplayId,
                NextListId = data.NextListId,
                UserProfile = CloneUserProfile(data.UserProfile)
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
            ActiveUrgencyMode = u.ActiveUrgencyMode
        };
    }
}

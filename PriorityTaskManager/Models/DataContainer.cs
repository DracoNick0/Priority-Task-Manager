using System.Collections.Generic;
using PriorityTaskManager.Models;

namespace PriorityTaskManager.Models
{
    /// <summary>
    /// Container for all persistent application data.
    /// </summary>
    public class DataContainer
    {
        public List<TaskItem> Tasks { get; set; } = new List<TaskItem>();
        public List<TaskList> Lists { get; set; } = new List<TaskList>();
        public int NextTaskId { get; set; } = 1;
        public int NextDisplayId { get; set; } = 1;
        public int NextListId { get; set; } = 1;
        public UserProfile UserProfile { get; set; } = new UserProfile();
        /// <summary>
        /// The ID of the currently active task list.
        /// </summary>
        public int ActiveListId { get; set; } = 0;
    }
}

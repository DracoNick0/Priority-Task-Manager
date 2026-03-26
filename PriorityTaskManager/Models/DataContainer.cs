using System.Collections.Generic;
using PriorityTaskManager.Models;

namespace PriorityTaskManager.Models
{
    /// <summary>
    /// Acts as a master container for all persistent application data.
    /// This single object is serialized to and from JSON, encapsulating the entire application state.
    /// </summary>
    public class DataContainer
    {
        /// <summary>
        /// Gets or sets the master list of all tasks across all task lists.
        /// </summary>
        public List<TaskItem> Tasks { get; set; } = new List<TaskItem>();

        /// <summary>
        /// Gets or sets the collection of all user-defined task lists.
        /// </summary>
        public List<TaskList> Lists { get; set; } = new List<TaskList>();

        /// <summary>
        /// Gets or sets the auto-incrementing counter for generating unique `TaskItem` IDs.
        /// </summary>
        public int NextTaskId { get; set; } = 1;

        /// <summary>
        /// Gets or sets the auto-incrementing counter for generating user-facing display IDs for tasks.
        /// </summary>
        public int NextDisplayId { get; set; } = 1;

        /// <summary>
        /// Gets or sets the auto-incrementing counter for generating unique `TaskList` IDs.
        /// </summary>
        public int NextListId { get; set; } = 1;

        /// <summary>
        /// Gets or sets the list of all user-defined events that block out time on the schedule.
        /// </summary>
        public List<Event> Events { get; set; } = new List<Event>();

        /// <summary>
        /// Gets or sets the auto-incrementing counter for generating unique `Event` IDs.
        /// </summary>
        public int NextEventId { get; set; } = 1;

        /// <summary>
        /// Gets or sets the user's profile, containing preferences and settings.
        /// </summary>
        public UserProfile UserProfile { get; set; } = new UserProfile();

        /// <summary>
        /// Gets or sets the ID of the currently active task list.
        /// </summary>
        public int ActiveListId { get; set; } = 0;
    }
}

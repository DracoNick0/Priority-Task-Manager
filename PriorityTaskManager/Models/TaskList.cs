using PriorityTaskManager.Models;

namespace PriorityTaskManager.Models
{
    /// <summary>
    /// Represents a task list with a name and sorting option.
    /// </summary>
    public class TaskList
    {
        /// <summary>
        /// Gets or sets the name of the task list.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the sorting option for the task list.
        /// </summary>
        public SortOption SortOption { get; set; }

        /// <summary>
        /// Initializes a new instance of the TaskList class.
        /// </summary>
        public TaskList()
        {
            Name = string.Empty;
            SortOption = SortOption.Default;
        }
    }
}

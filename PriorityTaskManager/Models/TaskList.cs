using PriorityTaskManager.Models;

namespace PriorityTaskManager.Models
{
    /// <summary>
    /// Represents a named container for a collection of tasks.
    /// </summary>
    public class TaskList
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TaskList"/> class with default values.
        /// </summary>
        public TaskList()
        {
            Name = string.Empty;
            SortOption = SortOption.Default;
        }

        /// <summary>
        /// Gets or sets the unique numeric identifier for the task list.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the user-defined name of the task list.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the preferred sorting option for displaying tasks within this list.
        /// </summary>
        public SortOption SortOption { get; set; }

    }
}

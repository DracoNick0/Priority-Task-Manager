using PriorityTaskManager.Models;

namespace PriorityTaskManager.Models
{
    /// <summary>
    /// Represents a task list with a name and sorting option.
    /// </summary>
    public class TaskList
    {
        /// <summary>
        /// Initializes a new instance of the TaskList class.
        /// </summary>
        public TaskList()
        {
            Name = string.Empty;
            SortOption = SortOption.Default;
        }

        /// <summary>
        /// Gets or sets the name of the task list.
        /// </summary>
    /// <summary>
    /// Gets or sets the unique numeric ID of the task list.
    /// </summary>
    public int Id { get; set; }
    public string Name { get; set; }

        /// <summary>
        /// Gets or sets the sorting option for the task list.
        /// </summary>
        public SortOption SortOption { get; set; }

    }
}

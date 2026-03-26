namespace PriorityTaskManager.Models
{
    /// <summary>
    /// Defines the available sorting options for displaying tasks in a list.
    /// </summary>
    public enum SortOption
    {
        /// <summary>
        /// The default sort order, typically determined by the scheduling engine's output.
        /// </summary>
        Default,

        /// <summary>
        /// Sorts tasks alphabetically by title.
        /// </summary>
        Alphabetical,

        /// <summary>
        /// Sorts tasks by their due date, from earliest to latest.
        /// </summary>
        DueDate,

        /// <summary>
        /// Sorts tasks by their unique identifier.
        /// </summary>
        Id
    }
}

namespace PriorityTaskManager.Models
{
    /// <summary>
    /// Describes the outcome of attempting to restore an archived task back to the active task list.
    /// </summary>
    public enum RestoreArchivedTaskResult
    {
        /// <summary>
        /// The task was restored to the active task list.
        /// </summary>
        Restored,

        /// <summary>
        /// No archived task with the given ID was found.
        /// </summary>
        NotFound,

        /// <summary>
        /// The task's original list no longer exists and no target list was specified.
        /// </summary>
        ListRequired,

        /// <summary>
        /// The specified target list does not exist.
        /// </summary>
        TargetListNotFound
    }
}

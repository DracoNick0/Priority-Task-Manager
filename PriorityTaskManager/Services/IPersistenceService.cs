using PriorityTaskManager.Models;

namespace PriorityTaskManager.Services
{
    /// <summary>
    /// Defines methods for loading and saving persistent application data.
    /// </summary>
    public interface IPersistenceService
    {
        DataContainer LoadData();
        void SaveData(DataContainer data);

        /// <summary>
        /// Appends the given tasks to the persisted archive record.
        /// </summary>
        /// <param name="tasksToArchive">The tasks to archive.</param>
        void ArchiveTasks(IEnumerable<TaskItem> tasksToArchive);

        /// <summary>
        /// Retrieves all tasks currently held in the persisted archive record.
        /// </summary>
        List<TaskItem> GetArchivedTasks();

        /// <summary>
        /// Removes a task from the persisted archive record.
        /// </summary>
        /// <param name="taskId">The ID of the archived task to remove.</param>
        /// <returns>True if a matching archived task was found and removed; otherwise false.</returns>
        bool RemoveArchivedTask(Guid taskId);
    }
}

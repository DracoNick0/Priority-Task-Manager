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
    }
}

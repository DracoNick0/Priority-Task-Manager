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

        /// <summary>
        /// Removes multiple tasks from the persisted archive in one operation.
        /// </summary>
        /// <param name="taskIds">The IDs of the archived tasks to remove.</param>
        /// <returns>The number of matching archived tasks removed.</returns>
        int RemoveArchivedTasks(IEnumerable<Guid> taskIds);

        /// <summary>
        /// Appends the given events to the persisted archive record.
        /// </summary>
        /// <param name="eventsToArchive">The events to archive.</param>
        void ArchiveEvents(IEnumerable<Event> eventsToArchive);

        /// <summary>
        /// Retrieves all events currently held in the persisted archive record.
        /// </summary>
        List<Event> GetArchivedEvents();

        /// <summary>
        /// Removes an event from the persisted archive record.
        /// </summary>
        /// <param name="eventId">The ID of the archived event to remove.</param>
        /// <returns>True if a matching archived event was found and removed; otherwise false.</returns>
        bool RemoveArchivedEvent(Guid eventId);

        /// <summary>
        /// Removes multiple events from the persisted archive in one operation.
        /// </summary>
        /// <param name="eventIds">The IDs of the archived events to remove.</param>
        /// <returns>The number of matching archived events removed.</returns>
        int RemoveArchivedEvents(IEnumerable<Guid> eventIds);

        /// <summary>
        /// Permanently removes every archived task and event.
        /// </summary>
        void ClearArchive();
    }
}

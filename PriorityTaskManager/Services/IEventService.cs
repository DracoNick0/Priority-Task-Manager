using PriorityTaskManager.Models;

namespace PriorityTaskManager.Services
{
    /// <summary>
    /// Defines CRUD operations for calendar events that block out time on the schedule.
    /// </summary>
    public interface IEventService
    {
        /// <summary>
        /// Adds a new event and persists the change.
        /// </summary>
        /// <param name="newEvent">The event to add.</param>
        void AddEvent(Event newEvent);

        /// <summary>
        /// Retrieves all events.
        /// </summary>
        /// <returns>An enumerable collection of events.</returns>
        IEnumerable<Event> GetAllEvents();

        /// <summary>
        /// Retrieves an event by its unique ID.
        /// </summary>
        /// <param name="id">The unique ID of the event.</param>
        /// <returns>The event if found; otherwise, null.</returns>
        Event? GetEvent(int id);

        /// <summary>
        /// Updates an existing event with new details.
        /// </summary>
        /// <param name="updatedEvent">The updated event object.</param>
        /// <returns>True if the event was updated successfully; otherwise, false.</returns>
        bool UpdateEvent(Event updatedEvent);

        /// <summary>
        /// Deletes an event by its unique ID.
        /// </summary>
        /// <param name="id">The unique ID of the event to delete.</param>
        /// <returns>True if the event was deleted successfully; otherwise, false.</returns>
        bool DeleteEvent(int id);

        /// <summary>
        /// Removes all events.
        /// </summary>
        void ClearEvents();
    }
}

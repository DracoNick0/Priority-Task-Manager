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

        /// <summary>Retrieves concrete event occurrences within an inclusive date window.</summary>
        IEnumerable<EventOccurrence> GetEventOccurrences(DateTime rangeStart, DateTime rangeEnd);

        /// <summary>
        /// Retrieves an event by its unique ID.
        /// </summary>
        /// <param name="id">The unique ID of the event.</param>
        /// <returns>The event if found; otherwise, null.</returns>
        Event? GetEvent(Guid id);

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
        bool DeleteEvent(Guid id);

        /// <summary>
        /// Removes all events.
        /// </summary>
        void ClearEvents();

        /// <summary>
        /// Edits a single occurrence, this-and-following occurrences, or the whole series of the recurring
        /// event whose base row has <see cref="Event.Id"/> equal to <paramref name="seriesId"/>.
        /// </summary>
        /// <param name="seriesId">The recurring event series' base event ID (equals <see cref="Event.SeriesId"/>).</param>
        /// <param name="occurrenceDate">The original (unmodified) date of the targeted occurrence.</param>
        /// <param name="name">The new name to apply.</param>
        /// <param name="startTime">The new start time to apply.</param>
        /// <param name="endTime">The new end time to apply.</param>
        /// <param name="target">Which occurrences of the series the edit applies to.</param>
        /// <param name="recurrenceRule">
        /// An updated recurrence pattern to apply, or null to leave the existing pattern unchanged.
        /// Ignored for <see cref="RecurrenceEditTarget.ThisOccurrence"/>, which only ever overrides a
        /// single occurrence and has no series pattern to change.
        /// </param>
        /// <returns>True if the series was found and edited; otherwise, false.</returns>
        bool EditOccurrence(Guid seriesId, DateTime occurrenceDate, string name, DateTime startTime, DateTime endTime, RecurrenceEditTarget target, RecurrenceRule? recurrenceRule = null);

        /// <summary>
        /// Deletes a single occurrence, this-and-following occurrences, or the whole series of the recurring
        /// event whose base row has <see cref="Event.Id"/> equal to <paramref name="seriesId"/>.
        /// </summary>
        /// <param name="seriesId">The recurring event series' base event ID (equals <see cref="Event.SeriesId"/>).</param>
        /// <param name="occurrenceDate">The original (unmodified) date of the targeted occurrence.</param>
        /// <param name="target">Which occurrences of the series the delete applies to.</param>
        /// <returns>True if the series was found and the delete applied; otherwise, false.</returns>
        bool DeleteOccurrence(Guid seriesId, DateTime occurrenceDate, RecurrenceEditTarget target);
    }
}

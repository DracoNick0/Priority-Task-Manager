using PriorityTaskManager.Models;

namespace PriorityTaskManager.API.Events
{
	/// <summary>Request body for creating/updating an event.</summary>
	public record EventRequest(string Name, DateTime StartTime, DateTime EndTime, RecurrenceRule? RecurrenceRule = null, string Description = "", string? Link = null);

	/// <summary>Request body for editing a single occurrence, this-and-following occurrences, or the whole series.</summary>
	public record EventOccurrenceEditRequest(string Name, DateTime StartTime, DateTime EndTime, RecurrenceRule? RecurrenceRule = null, string Description = "", string? Link = null);

	/// <summary>Response body representing a persisted event.</summary>
	public record EventResponse(Guid Id, string Name, DateTime StartTime, DateTime EndTime, RecurrenceRule? RecurrenceRule, Guid? SeriesId, string Description, string? Link);

	/// <summary>A concrete occurrence with its original recurrence date for edits and deletes.</summary>
	public record EventOccurrenceResponse(Guid Id, string Name, DateTime StartTime, DateTime EndTime, Guid? SeriesId, DateTime? OriginalOccurrenceDate, string Description, string? Link);

	public static class EventDtoExtensions
	{
		public static EventResponse ToResponse(this Event evt) => new(evt.Id, evt.Name, evt.StartTime, evt.EndTime, evt.RecurrenceRule, evt.SeriesId, evt.Description, evt.Link);

		/// <summary>Maps an expanded occurrence to its display response.</summary>
		public static EventOccurrenceResponse ToResponse(this EventOccurrence occurrence) => new(
			occurrence.Event.Id, occurrence.Event.Name, occurrence.Event.StartTime,
			occurrence.Event.EndTime, occurrence.Event.SeriesId, occurrence.OriginalOccurrenceDate,
			occurrence.Event.Description, occurrence.Event.Link);

		/// <summary>Maps a request onto a new <see cref="Event"/>; <c>Id</c>/<c>SeriesId</c> are assigned by core on add.</summary>
		public static Event ToNewEvent(this EventRequest request) => new()
		{
			Name = request.Name,
			Description = request.Description,
			Link = request.Link,
			StartTime = request.StartTime,
			EndTime = request.EndTime,
			RecurrenceRule = request.RecurrenceRule
		};

		/// <summary>Applies a request's fields onto <paramref name="id"/> for <c>TaskManagerService.UpdateEvent</c>.</summary>
		public static Event ToUpdatedEvent(this EventRequest request, Guid id)
		{
			var evt = request.ToNewEvent();
			evt.Id = id;
			return evt;
		}
	}
}

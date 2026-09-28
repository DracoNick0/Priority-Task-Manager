using PriorityTaskManager.Models;

namespace PriorityTaskManager.API.Events
{
	/// <summary>Request body for creating/updating an event.</summary>
	public record EventRequest(string Name, DateTime StartTime, DateTime EndTime, RecurrenceRule? RecurrenceRule = null);

	/// <summary>Request body for editing a single occurrence, this-and-following occurrences, or the whole series.</summary>
	public record EventOccurrenceEditRequest(string Name, DateTime StartTime, DateTime EndTime);

	/// <summary>Response body representing a persisted event.</summary>
	public record EventResponse(Guid Id, string Name, DateTime StartTime, DateTime EndTime, RecurrenceRule? RecurrenceRule, Guid? SeriesId);

	public static class EventDtoExtensions
	{
		public static EventResponse ToResponse(this Event evt) => new(evt.Id, evt.Name, evt.StartTime, evt.EndTime, evt.RecurrenceRule, evt.SeriesId);

		/// <summary>Maps a request onto a new <see cref="Event"/>; <c>Id</c>/<c>SeriesId</c> are assigned by core on add.</summary>
		public static Event ToNewEvent(this EventRequest request) => new()
		{
			Name = request.Name,
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

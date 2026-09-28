using PriorityTaskManager.Models;
using PriorityTaskManager.Services;

namespace PriorityTaskManager.API.Events
{
	/// <summary>
	/// Maps the REST endpoints for event CRUD, wrapping <see cref="TaskManagerService"/> per the
	/// integrations boundary (no persistence/scheduling logic lives here; see docs/ARCHITECTURE_INTEGRATIONS.md).
	/// </summary>
	public static class EventEndpoints
	{
		public static void MapEventEndpoints(this WebApplication app)
		{
			var group = app.MapGroup("/api/events").RequireAuthorization();

			group.MapGet("/", (TaskManagerService taskManagerService) =>
				Results.Ok(taskManagerService.GetAllEvents().Select(e => e.ToResponse())));

			group.MapGet("/{id:guid}", (Guid id, TaskManagerService taskManagerService) =>
			{
				var evt = taskManagerService.GetEvent(id);
				return evt is null ? Results.NotFound() : Results.Ok(evt.ToResponse());
			});

			group.MapPost("/", (EventRequest request, TaskManagerService taskManagerService) =>
			{
				if (string.IsNullOrWhiteSpace(request.Name))
				{
					return Results.BadRequest(new { error = "Event name cannot be empty." });
				}
				if (request.EndTime <= request.StartTime)
				{
					return Results.BadRequest(new { error = "Event end time must be after its start time." });
				}

				var evt = request.ToNewEvent();
				taskManagerService.AddEvent(evt);
				return Results.Created($"/api/events/{evt.Id}", evt.ToResponse());
			});

			group.MapPut("/{id:guid}", (Guid id, EventRequest request, TaskManagerService taskManagerService) =>
			{
				if (request.EndTime <= request.StartTime)
				{
					return Results.BadRequest(new { error = "Event end time must be after its start time." });
				}
				var updated = taskManagerService.UpdateEvent(request.ToUpdatedEvent(id));
				return updated ? Results.Ok(taskManagerService.GetEvent(id)!.ToResponse()) : Results.NotFound();
			});

			group.MapDelete("/{id:guid}", (Guid id, TaskManagerService taskManagerService) =>
				taskManagerService.DeleteEvent(id) ? Results.NoContent() : Results.NotFound());

			group.MapPut("/{seriesId:guid}/occurrences/{date}", (Guid seriesId, DateTime date, string target, EventOccurrenceEditRequest request, TaskManagerService taskManagerService) =>
			{
				if (!Enum.TryParse<RecurrenceEditTarget>(target, ignoreCase: true, out var editTarget))
				{
					return Results.BadRequest(new { error = $"Invalid target '{target}'. Expected ThisOccurrence, ThisAndFollowing, or AllOccurrences." });
				}
				if (string.IsNullOrWhiteSpace(request.Name))
				{
					return Results.BadRequest(new { error = "Event name cannot be empty." });
				}
				if (request.EndTime <= request.StartTime)
				{
					return Results.BadRequest(new { error = "Event end time must be after its start time." });
				}

				var edited = taskManagerService.EditEventOccurrence(seriesId, date, request.Name, request.StartTime, request.EndTime, editTarget);
				return edited ? Results.Ok(taskManagerService.GetEvent(seriesId)!.ToResponse()) : Results.NotFound();
			});

			group.MapDelete("/{seriesId:guid}/occurrences/{date}", (Guid seriesId, DateTime date, string target, TaskManagerService taskManagerService) =>
			{
				if (!Enum.TryParse<RecurrenceEditTarget>(target, ignoreCase: true, out var editTarget))
				{
					return Results.BadRequest(new { error = $"Invalid target '{target}'. Expected ThisOccurrence, ThisAndFollowing, or AllOccurrences." });
				}

				var deleted = taskManagerService.DeleteEventOccurrence(seriesId, date, editTarget);
				return deleted ? Results.NoContent() : Results.NotFound();
			});
		}
	}
}

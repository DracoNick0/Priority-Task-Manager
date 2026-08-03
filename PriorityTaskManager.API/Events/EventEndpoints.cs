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
		}
	}
}

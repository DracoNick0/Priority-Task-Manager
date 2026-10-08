using PriorityTaskManager.API.Tasks;
using PriorityTaskManager.API.Events;
using PriorityTaskManager.Models;
using PriorityTaskManager.Services;

namespace PriorityTaskManager.API.Archive
{
	/// <summary>
	/// Maps the REST endpoints for reading, grouping, restoring, and permanently deleting archive items, wrapping
	/// <see cref="TaskManagerService"/> per the integrations boundary (no persistence logic lives here;
	/// see docs/ARCHITECTURE_INTEGRATIONS.md).
	/// </summary>
	public static class ArchiveEndpoints
	{
		public static void MapArchiveEndpoints(this WebApplication app)
		{
			var group = app.MapGroup("/api/archive").RequireAuthorization();

			group.MapGet("/groups", (TaskManagerService taskManagerService) =>
				Results.Ok(taskManagerService.GetArchiveGroups().Select(archiveGroup =>
					new ArchiveGroupResponse(
						archiveGroup.GroupId,
						archiveGroup.Kind,
						archiveGroup.Tasks.Select(task => task.ToResponse()).ToList(),
						archiveGroup.Events.Select(evt => evt.ToResponse()).ToList()))));

			group.MapGet("/", (TaskManagerService taskManagerService) =>
				Results.Ok(taskManagerService.GetArchivedTasks().Select(t => t.ToResponse())));

			group.MapGet("/events", (TaskManagerService taskManagerService) =>
				Results.Ok(taskManagerService.GetArchivedEvents().Select(e => e.ToResponse())));

			group.MapPost("/{id:guid}/restore", (Guid id, RestoreArchivedTaskRequest? request, TaskManagerService taskManagerService) =>
			{
				var result = taskManagerService.RestoreArchivedTask(id, request?.TargetListId);
				return result switch
				{
					RestoreArchivedTaskResult.Restored => Results.Ok(taskManagerService.GetTaskById(id)!.ToResponse()),
					RestoreArchivedTaskResult.NotFound => Results.NotFound(),
					RestoreArchivedTaskResult.TargetListNotFound => Results.BadRequest(new { error = "Target list does not exist." }),
					RestoreArchivedTaskResult.ListRequired => Results.Conflict(new { error = "The task's original list no longer exists; specify a targetListId." }),
					_ => Results.Problem("Unexpected restore result.")
				};
			});

			group.MapPost("/task-groups/{groupId:guid}/restore", (Guid groupId, RestoreArchivedTaskRequest? request, TaskManagerService taskManagerService) =>
			{
				var result = taskManagerService.RestoreArchivedTaskGroup(groupId, request?.TargetListId);
				return result switch
				{
					RestoreArchivedTaskResult.Restored => Results.NoContent(),
					RestoreArchivedTaskResult.NotFound => Results.NotFound(),
					RestoreArchivedTaskResult.TargetListNotFound => Results.BadRequest(new { error = "Target list does not exist." }),
					RestoreArchivedTaskResult.ListRequired => Results.Conflict(new { error = "A task's original list no longer exists; specify a targetListId." }),
					_ => Results.Problem("Unexpected restore result.")
				};
			});

			group.MapPost("/event-groups/{groupId:guid}/restore", (Guid groupId, TaskManagerService taskManagerService) =>
				taskManagerService.RestoreArchivedEventGroup(groupId)
					? Results.NoContent()
					: Results.NotFound());

			group.MapDelete("/{id:guid}", (Guid id, TaskManagerService taskManagerService) =>
				taskManagerService.DeleteArchivedTask(id) ? Results.NoContent() : Results.NotFound());

			group.MapPost("/events/{id:guid}/restore", (Guid id, TaskManagerService taskManagerService) =>
				taskManagerService.RestoreArchivedEvent(id) && taskManagerService.GetEvent(id) is { } restored
					? Results.Ok(restored.ToResponse())
					: Results.NotFound());

			group.MapDelete("/events/{id:guid}", (Guid id, TaskManagerService taskManagerService) =>
				taskManagerService.GetArchivedEvents().Any(e => e.Id == id) && taskManagerService
					.DeleteArchivedEvent(id)
					? Results.NoContent()
					: Results.NotFound());

			group.MapDelete("/", (TaskManagerService taskManagerService) =>
			{
				taskManagerService.ClearArchive();
				return Results.NoContent();
			});
		}
	}
}

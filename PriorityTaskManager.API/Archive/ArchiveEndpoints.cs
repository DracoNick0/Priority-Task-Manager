using PriorityTaskManager.API.Tasks;
using PriorityTaskManager.Models;
using PriorityTaskManager.Services;

namespace PriorityTaskManager.API.Archive
{
	/// <summary>
	/// Maps the REST endpoints for reading and restoring archived tasks, wrapping
	/// <see cref="TaskManagerService"/> per the integrations boundary (no persistence logic lives here;
	/// see docs/ARCHITECTURE_INTEGRATIONS.md).
	/// </summary>
	public static class ArchiveEndpoints
	{
		public static void MapArchiveEndpoints(this WebApplication app)
		{
			var group = app.MapGroup("/api/archive").RequireAuthorization();

			group.MapGet("/", (TaskManagerService taskManagerService) =>
				Results.Ok(taskManagerService.GetArchivedTasks().Select(t => t.ToResponse())));

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

			group.MapDelete("/{id:guid}", (Guid id, TaskManagerService taskManagerService) =>
				taskManagerService.DeleteArchivedTask(id) ? Results.NoContent() : Results.NotFound());
		}
	}
}

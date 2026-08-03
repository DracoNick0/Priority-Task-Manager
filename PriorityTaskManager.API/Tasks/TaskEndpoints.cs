using PriorityTaskManager.Services;

namespace PriorityTaskManager.API.Tasks
{
	/// <summary>
	/// Maps the REST endpoints for task CRUD, wrapping <see cref="TaskManagerService"/> per the
	/// integrations boundary (no persistence/scheduling logic lives here; see docs/ARCHITECTURE_INTEGRATIONS.md).
	/// </summary>
	public static class TaskEndpoints
	{
		public static void MapTaskEndpoints(this WebApplication app)
		{
			var group = app.MapGroup("/api/tasks").RequireAuthorization();

			group.MapGet("/", (TaskManagerService taskManagerService) =>
				Results.Ok(taskManagerService.GetAllTasks().Select(t => t.ToResponse())));

			group.MapGet("/{id:guid}", (Guid id, TaskManagerService taskManagerService) =>
			{
				var task = taskManagerService.GetTaskById(id);
				return task is null ? Results.NotFound() : Results.Ok(task.ToResponse());
			});

			group.MapPost("/", (TaskRequest request, TaskManagerService taskManagerService) =>
			{
				if (string.IsNullOrWhiteSpace(request.Title))
				{
					return Results.BadRequest(new { error = "Task title cannot be empty." });
				}

				var task = request.ToNewTaskItem();
				try
				{
					taskManagerService.AddTask(task);
				}
				catch (ArgumentException ex)
				{
					return Results.BadRequest(new { error = ex.Message });
				}
				return Results.Created($"/api/tasks/{task.Id}", task.ToResponse());
			});

			group.MapPut("/{id:guid}", (Guid id, TaskRequest request, TaskManagerService taskManagerService) =>
			{
				try
				{
					var updated = taskManagerService.UpdateTask(request.ToUpdatedTaskItem(id));
					if (!updated)
					{
						return Results.NotFound();
					}
				}
				catch (ArgumentException ex)
				{
					return Results.BadRequest(new { error = ex.Message });
				}
				catch (InvalidOperationException ex)
				{
					return Results.Conflict(new { error = ex.Message });
				}
				var task = taskManagerService.GetTaskById(id);
				return Results.Ok(task!.ToResponse());
			});

			group.MapDelete("/{id:guid}", (Guid id, TaskManagerService taskManagerService) =>
				taskManagerService.DeleteTask(id) ? Results.NoContent() : Results.NotFound());

			group.MapPost("/{id:guid}/complete", (Guid id, TaskManagerService taskManagerService) =>
				taskManagerService.MarkTaskAsComplete(id) ? Results.Ok(taskManagerService.GetTaskById(id)!.ToResponse()) : Results.NotFound());

			group.MapPost("/{id:guid}/uncomplete", (Guid id, TaskManagerService taskManagerService) =>
				taskManagerService.MarkTaskAsIncomplete(id) ? Results.Ok(taskManagerService.GetTaskById(id)!.ToResponse()) : Results.NotFound());
		}
	}
}

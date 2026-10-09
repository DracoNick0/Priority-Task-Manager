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

			group.MapGet("/occurrences", (Guid listId, DateTime? from, DateTime? to, TaskManagerService taskManagerService, ITimeService timeService) =>
			{
				taskManagerService.ApplyListTimePreference(listId, timeService);
				var now = timeService.GetCurrentTime().Date;
				var rangeStart = from?.Date ?? now.AddDays(-14);
				var rangeEnd = to?.Date ?? now.AddDays(14);
				if (rangeEnd < rangeStart || (rangeEnd - rangeStart).TotalDays > 31)
					return Results.BadRequest(new { error = "Occurrence window must span 0 to 31 days." });
				return Results.Ok(taskManagerService.GetTaskOccurrences(listId, rangeStart, rangeEnd, timeService).Select(o => o.ToResponse()));
			});

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
				taskManagerService.ArchiveTask(id) ? Results.NoContent() : Results.NotFound());

			group.MapPost("/archive/batch", (ArchiveTasksRequest request, TaskManagerService taskManagerService) =>
			{
				if (request.TaskIds is not { Count: > 0 })
					return Results.BadRequest(new { error = "At least one task ID is required." });
				return taskManagerService.ArchiveTaskGroup(request.TaskIds)
					? Results.NoContent()
					: Results.NotFound();
			});

			group.MapPost("/{id:guid}/complete", (Guid id, TaskManagerService taskManagerService) =>
			{
				var task = taskManagerService.GetTaskById(id);
				if (task?.RecurrenceRule != null)
					return Results.BadRequest(new { error = "Complete a recurring task through one of its occurrences." });
				return taskManagerService.MarkTaskAsComplete(id)
					? Results.Ok(taskManagerService.GetTaskById(id)!.ToResponse())
					: Results.NotFound();
			});

			group.MapPost("/{id:guid}/uncomplete", (Guid id, TaskManagerService taskManagerService) =>
			{
				var task = taskManagerService.GetTaskById(id);
				if (task?.RecurrenceRule != null)
					return Results.BadRequest(new { error = "Undo completion through the completed occurrence." });
				return taskManagerService.MarkTaskAsIncomplete(id)
					? Results.Ok(taskManagerService.GetTaskById(id)!.ToResponse())
					: Results.NotFound();
			});

			group.MapPost("/{seriesId:guid}/occurrences/{date}/complete", (Guid seriesId, DateTime date, TaskManagerService taskManagerService, ITimeService timeService) =>
			{
				var task = taskManagerService.GetTaskById(seriesId);
				if (task == null)
					return Results.NotFound();
				taskManagerService.ApplyListTimePreference(task.ListId, timeService);
				try
				{
					return taskManagerService.CompleteTaskOccurrence(seriesId, date, timeService)
						? Results.NoContent()
						: Results.NotFound();
				}
				catch (InvalidOperationException error)
				{
					return Results.Conflict(new { error = error.Message });
				}
			});

			group.MapPost("/{seriesId:guid}/occurrences/{date}/undo", (Guid seriesId, DateTime date, TaskManagerService taskManagerService, ITimeService timeService) =>
			{
				var task = taskManagerService.GetTaskById(seriesId);
				if (task == null)
					return Results.NotFound();
				taskManagerService.ApplyListTimePreference(task.ListId, timeService);
				return taskManagerService.UndoTaskOccurrenceCompletion(seriesId, date, timeService)
					? Results.NoContent()
					: Results.NotFound();
			});

			group.MapPost("/{seriesId:guid}/occurrences/{date}/skip", (Guid seriesId, DateTime date, TaskManagerService taskManagerService, ITimeService timeService) =>
			{
				var task = taskManagerService.GetTaskById(seriesId);
				if (task == null)
					return Results.NotFound();
				taskManagerService.ApplyListTimePreference(task.ListId, timeService);
				try
				{
					return taskManagerService.SkipTaskOccurrence(seriesId, date, timeService)
						? Results.NoContent()
						: Results.NotFound();
				}
				catch (InvalidOperationException error)
				{
					return Results.Conflict(new { error = error.Message });
				}
			});

			group.MapPost("/{id:guid}/archive", (Guid id, TaskManagerService taskManagerService) =>
				taskManagerService.ArchiveTask(id) ? Results.NoContent() : Results.NotFound());
		}
	}
}

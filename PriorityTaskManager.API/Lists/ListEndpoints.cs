using PriorityTaskManager.Services;

namespace PriorityTaskManager.API.Lists
{
	/// <summary>
	/// Maps the REST endpoints for task list CRUD, wrapping <see cref="TaskManagerService"/> per the
	/// integrations boundary (no persistence/scheduling logic lives here; see docs/ARCHITECTURE_INTEGRATIONS.md).
	/// </summary>
	public static class ListEndpoints
	{
		public static void MapListEndpoints(this WebApplication app)
		{
			var group = app.MapGroup("/api/lists").RequireAuthorization();

			group.MapGet("/", (TaskManagerService taskManagerService) =>
				Results.Ok(taskManagerService.GetAllLists().Select(l => l.ToResponse())));

			group.MapGet("/{id:guid}", (Guid id, TaskManagerService taskManagerService) =>
			{
				var list = taskManagerService.GetListById(id);
				return list is null ? Results.NotFound() : Results.Ok(list.ToResponse());
			});

			group.MapPost("/", (ListRequest request, TaskManagerService taskManagerService) =>
			{
				if (string.IsNullOrWhiteSpace(request.Name))
				{
					return Results.BadRequest(new { error = "List name cannot be empty." });
				}

				var list = request.ToNewTaskList();
				try
				{
					taskManagerService.AddList(list);
				}
				catch (InvalidOperationException ex)
				{
					return Results.Conflict(new { error = ex.Message });
				}
				return Results.Created($"/api/lists/{list.Id}", list.ToResponse());
			});

			group.MapPut("/{id:guid}", (Guid id, ListRequest request, TaskManagerService taskManagerService) =>
			{
				if (taskManagerService.GetListById(id) is null)
				{
					return Results.NotFound();
				}
				try
				{
					taskManagerService.UpdateList(request.ToUpdatedTaskList(id));
				}
				catch (InvalidOperationException ex)
				{
					return Results.Conflict(new { error = ex.Message });
				}
				return Results.Ok(taskManagerService.GetListById(id)!.ToResponse());
			});

			group.MapDelete("/{id:guid}", (Guid id, TaskManagerService taskManagerService) =>
			{
				var list = taskManagerService.GetListById(id);
				if (list is null)
				{
					return Results.NotFound();
				}
				taskManagerService.DeleteList(list.Name);
				return Results.NoContent();
			});
		}
	}
}

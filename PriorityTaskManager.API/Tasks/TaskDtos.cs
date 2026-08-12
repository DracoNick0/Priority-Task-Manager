using PriorityTaskManager.Models;

namespace PriorityTaskManager.API.Tasks
{
	/// <summary>
	/// Request body for creating/updating a task. Mirrors <see cref="TaskItem"/>'s user-editable fields
	/// (not scheduler-computed ones like <c>UrgencyScore</c> or <c>ScheduledParts</c>) so it can also serve
	/// as the base shape for future LLM-intake candidate tasks (see docs/LLM_ASSISTED_INTAKE.md).
	/// </summary>
	public record TaskRequest(
		string Title,
		string? Description,
		Guid ListId,
		int Importance,
		DateTime? DueDate,
		DateTime? NotBefore,
		TimeSpan EstimatedDuration,
		List<Guid>? Dependencies,
		bool IsPinned,
		int Complexity,
		double Points,
		TimeSpan? BeforePadding,
		TimeSpan? AfterPadding,
		bool IsDivisible);

	/// <summary>Response body representing a persisted task, including scheduler-computed read-only fields.</summary>
	public record TaskResponse(
		Guid Id,
		int DisplayId,
		string? Title,
		string? Description,
		Guid ListId,
		string ListName,
		bool IsCompleted,
		double Progress,
		int Importance,
		double EffectiveImportance,
		DateTime? DueDate,
		DateTime? NotBefore,
		TimeSpan EstimatedDuration,
		DateTime? LatestPossibleStartDate,
		List<Guid> Dependencies,
		double UrgencyScore,
		bool IsPinned,
		int Complexity,
		double Points,
		TimeSpan? BeforePadding,
		TimeSpan? AfterPadding,
		bool IsDivisible);

	public static class TaskDtoExtensions
	{
		public static TaskResponse ToResponse(this TaskItem task) => new(
			task.Id,
			task.DisplayId,
			task.Title,
			task.Description,
			task.ListId,
			task.ListName,
			task.IsCompleted,
			task.Progress,
			task.Importance,
			task.EffectiveImportance,
			task.DueDate,
			task.NotBefore,
			task.EstimatedDuration,
			task.LatestPossibleStartDate,
			task.Dependencies,
			task.UrgencyScore,
			task.IsPinned,
			task.Complexity,
			task.Points,
			task.BeforePadding,
			task.AfterPadding,
			task.IsDivisible);

		/// <summary>Maps a request onto a new <see cref="TaskItem"/>; identity/scheduler-computed fields are left for core to assign.</summary>
		public static TaskItem ToNewTaskItem(this TaskRequest request) => new()
		{
			Title = request.Title,
			Description = request.Description ?? string.Empty,
			ListId = request.ListId,
			Importance = request.Importance,
			DueDate = request.DueDate,
			NotBefore = request.NotBefore,
			EstimatedDuration = request.EstimatedDuration,
			Dependencies = request.Dependencies is null ? new List<Guid>() : new List<Guid>(request.Dependencies),
			IsPinned = request.IsPinned,
			Complexity = request.Complexity,
			Points = request.Points,
			BeforePadding = request.BeforePadding,
			AfterPadding = request.AfterPadding,
			IsDivisible = request.IsDivisible
		};

		/// <summary>Applies a request's editable fields onto <paramref name="id"/> for <c>TaskManagerService.UpdateTask</c>.</summary>
		public static TaskItem ToUpdatedTaskItem(this TaskRequest request, Guid id)
		{
			var task = request.ToNewTaskItem();
			task.Id = id;
			return task;
		}
	}
}

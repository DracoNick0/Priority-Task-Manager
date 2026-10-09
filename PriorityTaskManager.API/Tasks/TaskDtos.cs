using PriorityTaskManager.Models;
using System.Security.Cryptography;
using System.Text;

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
		bool IsDivisible = false,
		string? Link = null,
		RecurrenceRule? RecurrenceRule = null,
		TaskProgressionMode ProgressionMode = TaskProgressionMode.RollForwardKeepBacklog,
		int RequiredCompletions = 1,
		bool ShowMissedIndicator = true,
		bool TrackStreak = false);

	/// <summary>Response body representing a concrete recurring task occurrence.</summary>
	public record TaskOccurrenceResponse(
		Guid Id,
		Guid SeriesId,
		DateTime OccurrenceDate,
		string? Title,
		string? Description,
		string? Link,
		Guid ListId,
		string ListName,
		bool IsCompleted,
		double Progress,
		int CompletionCount,
		int RequiredCompletions,
		TaskOccurrenceStatus Status,
		bool IsMissed,
		bool ShowMissedIndicator,
		bool HasMissedOccurrence,
		bool TrackStreak,
		int CurrentStreak,
		int BestStreak,
		int Importance,
		double EffectiveImportance,
		DateTime? DueDate,
		DateTime? NotBefore,
		TimeSpan EstimatedDuration,
		List<Guid> Dependencies,
		double UrgencyScore,
		bool IsPinned,
		int Complexity,
		double Points,
		TimeSpan? BeforePadding,
		TimeSpan? AfterPadding,
		bool IsDivisible,
		TaskProgressionMode ProgressionMode);

	/// <summary>Request body for archiving a set of tasks as one group.</summary>
	public record ArchiveTasksRequest(List<Guid> TaskIds);

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
		bool IsDivisible,
		string? Link,
		RecurrenceRule? RecurrenceRule,
		Guid? SeriesId,
		TaskProgressionMode ProgressionMode,
		int RequiredCompletions,
		bool ShowMissedIndicator,
		bool TrackStreak,
		bool HasMissedOccurrence,
		int CurrentStreak,
		int BestStreak);

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
			task.IsDivisible,
			task.Link,
			task.RecurrenceRule,
			task.SeriesId,
			task.ProgressionMode,
			task.RequiredCompletions,
			task.ShowMissedIndicator,
			task.TrackStreak,
			task.ShowMissedIndicator && task.HasUnresolvedMissedIndicator,
			task.TrackStreak ? task.CurrentStreak : 0,
			task.TrackStreak ? task.BestStreak : 0);

		public static TaskOccurrenceResponse ToResponse(this TaskOccurrence occurrence)
		{
			var task = occurrence.Series;
			var seriesId = task.SeriesId ?? task.Id;
			var occurrenceId = CreateOccurrenceId(seriesId, occurrence.ScheduledDate);
			return new TaskOccurrenceResponse(
				occurrenceId,
				seriesId,
				occurrence.ScheduledDate,
				task.Title,
				task.Description,
				task.Link,
				task.ListId,
				task.ListName,
				occurrence.Status == TaskOccurrenceStatus.Completed,
				Math.Min(1.0, (double)occurrence.CompletionCount / task.RequiredCompletions),
				occurrence.CompletionCount,
				task.RequiredCompletions,
				occurrence.Status,
				occurrence.IsMissed,
				task.ShowMissedIndicator,
				task.ShowMissedIndicator && task.HasUnresolvedMissedIndicator,
				task.TrackStreak,
				occurrence.CurrentStreak,
				occurrence.BestStreak,
				task.Importance,
				task.EffectiveImportance,
				occurrence.ScheduledDate,
				task.NotBefore.HasValue && task.NotBefore.Value.Date > occurrence.ScheduledDate.Date
					? task.NotBefore
					: occurrence.ScheduledDate,
				TimeSpan.FromTicks((long)(task.EstimatedDuration.Ticks *
					(double)Math.Max(0, task.RequiredCompletions - occurrence.CompletionCount) / task.RequiredCompletions)),
				task.Dependencies,
				task.UrgencyScore,
				task.IsPinned,
				task.Complexity,
				task.Points,
				task.BeforePadding,
				task.AfterPadding,
				task.IsDivisible,
				task.ProgressionMode);
		}

		private static Guid CreateOccurrenceId(Guid seriesId, DateTime date)
		{
			var hash = SHA256.HashData(Encoding.UTF8.GetBytes($"{seriesId:N}:{date:yyyy-MM-dd}"));
			return new Guid(hash.AsSpan(0, 16));
		}

		/// <summary>Maps a request onto a new <see cref="TaskItem"/>; identity/scheduler-computed fields are left for core to assign.</summary>
		public static TaskItem ToNewTaskItem(this TaskRequest request) => new()
		{
			Title = request.Title,
			Description = request.Description ?? string.Empty,
			Link = request.Link,
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
			IsDivisible = request.IsDivisible,
			RecurrenceRule = request.RecurrenceRule?.Clone(),
			ProgressionMode = request.ProgressionMode,
			RequiredCompletions = request.RequiredCompletions,
			ShowMissedIndicator = request.ShowMissedIndicator,
			TrackStreak = request.TrackStreak
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

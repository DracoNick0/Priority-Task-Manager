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
		TaskProgressionMode ProgressionMode,
		RecurrenceRule? RecurrenceRule,
		TimeSpan FullEstimatedDuration);

	/// <summary>Editable task and series fields for recurring occurrence mutations.</summary>
	public record TaskOccurrenceEditRequest(
		string Title,
		string? Description,
		string? Link,
		DateTime? DueDate,
		DateTime? NotBefore,
		TimeSpan EstimatedDuration,
		int Importance,
		int Complexity,
		bool IsPinned,
		bool IsDivisible,
		RecurrenceRule? RecurrenceRule,
		TaskProgressionMode ProgressionMode,
		int RequiredCompletions,
		bool ShowMissedIndicator,
		bool TrackStreak)
	{
		public TaskItem ToTaskItem(Guid listId) => new()
		{
			Title = Title,
			Description = Description ?? string.Empty,
			Link = Link,
			ListId = listId,
			DueDate = DueDate,
			NotBefore = NotBefore,
			EstimatedDuration = EstimatedDuration,
			Importance = Importance,
			Complexity = Complexity,
			IsPinned = IsPinned,
			IsDivisible = IsDivisible,
			RecurrenceRule = RecurrenceRule?.Clone(),
			ProgressionMode = ProgressionMode,
			RequiredCompletions = RequiredCompletions,
			ShowMissedIndicator = ShowMissedIndicator,
			TrackStreak = TrackStreak
		};
	}

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
		int BestStreak,
		List<TaskOccurrenceStateResponse> OccurrenceStates,
		List<RecurrenceException> RecurrenceExceptions,
		List<TaskOccurrenceOverride> OccurrenceOverrides);

	/// <summary>Persisted progress for one recurrence-rule date, supplied to the scheduler.</summary>
	public sealed record TaskOccurrenceStateResponse(
		DateTime ScheduledDate,
		TaskOccurrenceStatus Status,
		int CompletionCount,
		DateTime? CompletedAt);

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
			task.TrackStreak ? task.BestStreak : 0,
			(task.OccurrenceStates ?? new List<TaskOccurrenceState>())
				.Select(state => new TaskOccurrenceStateResponse(
					state.ScheduledDate,
					state.Status,
					state.CompletionCount,
					state.CompletedAt))
				.ToList(),
			(task.RecurrenceExceptions ?? new List<RecurrenceException>())
				.Select(exception => exception.Clone())
				.ToList(),
			(task.OccurrenceOverrides ?? new List<TaskOccurrenceOverride>())
				.Select(occurrenceOverride => occurrenceOverride.Clone())
				.ToList());

		public static TaskOccurrenceResponse ToResponse(this TaskOccurrence occurrence)
		{
			var task = occurrence.Series;
			var seriesId = task.SeriesId ?? task.Id;
			var occurrenceId = CreateOccurrenceId(seriesId, occurrence.ScheduledDate);
			var occurrenceOverride = task.OccurrenceOverrides
				?.FirstOrDefault(item => item.OriginalOccurrenceDate.Date == occurrence.ScheduledDate.Date);
			var duration = occurrenceOverride?.EstimatedDuration ?? task.EstimatedDuration;
			var importance = occurrenceOverride?.Importance ?? task.Importance;
			return new TaskOccurrenceResponse(
				occurrenceId,
				seriesId,
				occurrence.ScheduledDate,
				occurrenceOverride?.Title ?? task.Title,
				occurrenceOverride?.Description ?? task.Description,
				occurrenceOverride?.Link ?? task.Link,
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
				importance,
				occurrenceOverride == null ? task.EffectiveImportance : importance,
				occurrenceOverride == null
					? occurrence.ScheduledDate.Date.Add(task.DueDate?.TimeOfDay ?? TimeSpan.Zero)
					: occurrenceOverride.DueDate,
				(occurrenceOverride?.NotBefore ?? task.NotBefore) is { } notBefore &&
					notBefore.Date > occurrence.ScheduledDate.Date
						? notBefore
						: occurrence.ScheduledDate,
				TimeSpan.FromTicks((long)(duration.Ticks *
					(double)Math.Max(0, task.RequiredCompletions - occurrence.CompletionCount) / task.RequiredCompletions)),
				task.Dependencies,
				task.UrgencyScore,
				occurrenceOverride?.IsPinned ?? task.IsPinned,
				occurrenceOverride?.Complexity ?? task.Complexity,
				task.Points,
				task.BeforePadding,
				task.AfterPadding,
				occurrenceOverride?.IsDivisible ?? task.IsDivisible,
				task.ProgressionMode,
				task.RecurrenceRule?.Clone(),
				duration);
		}

		private static Guid CreateOccurrenceId(Guid seriesId, DateTime date)
		{
			return TaskOccurrence.CreateId(seriesId, date);
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

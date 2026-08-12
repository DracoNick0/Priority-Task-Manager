using PriorityTaskManager.Models;

namespace PriorityTaskManager.API.Local
{
	/// <summary>Task shape sent by a local/offline client (e.g. the Flutter app's Hive-backed store) for schedule computation.</summary>
	public record LocalTaskRequest(
		Guid Id,
		string? Title,
		bool IsCompleted,
		double Progress,
		int Importance,
		int Complexity,
		double Points,
		DateTime? DueDate,
		DateTime? NotBefore,
		TimeSpan EstimatedDuration,
		List<Guid>? Dependencies,
		bool IsPinned,
		TimeSpan? BeforePadding,
		TimeSpan? AfterPadding,
		bool IsDivisible)
	{
		public TaskItem ToTaskItem() => new()
		{
			Id = Id,
			Title = Title,
			IsCompleted = IsCompleted,
			Progress = Progress,
			Importance = Importance,
			Complexity = Complexity,
			Points = Points,
			DueDate = DueDate,
			NotBefore = NotBefore,
			EstimatedDuration = EstimatedDuration,
			Dependencies = Dependencies is null ? new List<Guid>() : new List<Guid>(Dependencies),
			IsPinned = IsPinned,
			BeforePadding = BeforePadding,
			AfterPadding = AfterPadding,
			IsDivisible = IsDivisible
		};
	}

	/// <summary>Fixed time block (e.g. a calendar event) sent by the local client, unavailable for scheduling.</summary>
	public record LocalEventRequest(Guid Id, string Name, DateTime StartTime, DateTime EndTime)
	{
		public Event ToEvent() => new() { Id = Id, Name = Name, StartTime = StartTime, EndTime = EndTime };
	}

	/// <summary>The subset of <see cref="UserProfile"/> that affects scheduling, sent by the local client.</summary>
	public record LocalUserProfileRequest(
		TimeOnly WorkStartTime,
		TimeOnly WorkEndTime,
		List<DayOfWeek> WorkDays,
		SchedulingMode SchedulingMode,
		TimeSpan DesiredBreatherDuration,
		double SlackThresholdDire,
		double SlackThresholdPressing,
		double SlackThresholdFocus,
		double SlackThresholdSafe)
	{
		public UserProfile ToUserProfile() => new()
		{
			WorkStartTime = WorkStartTime,
			WorkEndTime = WorkEndTime,
			WorkDays = WorkDays is null || WorkDays.Count == 0 ? new List<DayOfWeek> { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday } : new List<DayOfWeek>(WorkDays),
			SchedulingMode = SchedulingMode,
			DesiredBreatherDuration = DesiredBreatherDuration,
			SlackThresholdDire = SlackThresholdDire,
			SlackThresholdPressing = SlackThresholdPressing,
			SlackThresholdFocus = SlackThresholdFocus,
			SlackThresholdSafe = SlackThresholdSafe
		};
	}

	/// <summary>
	/// Request body for the stateless local schedule-compute endpoint: the client's current tasks/events/profile,
	/// run through the same scheduling strategies the CLI uses, with nothing persisted server-side.
	/// </summary>
	/// <param name="Now">Optional simulated "current time" override (mirrors the CLI's `time custom`); defaults to server real time.</param>
	public record LocalScheduleRequest(
		List<LocalTaskRequest> Tasks,
		List<LocalEventRequest> Events,
		LocalUserProfileRequest Profile,
		DateTime? Now);

	public record LocalScheduledChunkResponse(DateTime StartTime, DateTime EndTime);

	public record LocalScheduledTaskResponse(
		Guid Id,
		string? Title,
		DateTime? DueDate,
		TimeSpan EstimatedDuration,
		bool IsPinned,
		List<LocalScheduledChunkResponse> ScheduledParts,
		double RealisticSlackMinutes,
		double ActualSlackMinutes);

	/// <summary>
	/// Response for the local schedule-compute endpoint: the computed placement for each schedulable task,
	/// any tasks the strategy could not fit, and the day's least-slack summary (mirrors the CLI dashboard meter).
	/// </summary>
	public record LocalScheduleResponse(
		List<LocalScheduledTaskResponse> ScheduledTasks,
		List<Guid> UnscheduledTaskIds,
		Guid? LeastSlackTaskId,
		string? LeastSlackTaskTitle,
		double? LeastSlackRealisticMinutes,
		double? LeastSlackActualMinutes);
}

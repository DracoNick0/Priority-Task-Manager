using PriorityTaskManager.Models;
using PriorityTaskManager.Scheduling.GoldPanning;
using PriorityTaskManager.Scheduling.Optimization;
using PriorityTaskManager.Services;

namespace PriorityTaskManager.API.Local
{
	/// <summary>
	/// Runs a stateless schedule computation from a posted <see cref="LocalScheduleRequest"/>, used by the
	/// authenticated, Subscription-gated <c>/api/schedule</c> route
	/// (<see cref="PriorityTaskManager.API.Schedule.ScheduleEndpoints"/>). Nothing here reads or writes
	/// server-side persistence: the caller sends its current tasks/events/profile and gets back a computed
	/// schedule.
	/// </summary>
	public static class ScheduleComputation
	{
		public static IResult Compute(LocalScheduleRequest request)
		{
			if (request.Tasks is null)
			{
				return Results.BadRequest(new { error = "A task list is required to compute a schedule." });
			}

			if (request.Profile is null)
			{
				return Results.BadRequest(new { error = "A user profile is required to compute a schedule." });
			}

			if (request.Tasks.Any(task => task.RecurrenceRule != null && !IsValidRecurringTask(task)))
			{
				return Results.BadRequest(new
				{
					error = "Recurring tasks require a rule, valid occurrence state, at least one required completion, and no dependencies."
				});
			}

			var profile = request.Profile.ToUserProfile();
			var events = (request.Events ?? new List<LocalEventRequest>()).Select(e => e.ToEvent()).ToList();
			var tasks = (request.Tasks ?? new List<LocalTaskRequest>()).Select(t => t.ToTaskItem()).ToList();

			var timeService = new TimeService();
			if (request.Now.HasValue)
			{
				timeService.SetSimulatedTime(request.Now.Value);
			}

			IUrgencyStrategy strategy = profile.SchedulingMode == SchedulingMode.ConstraintOptimization
				? new ConstraintOptimizationStrategy(profile, events, timeService)
				: new GoldPanningStrategy(profile, events, timeService, new RecurrenceExpansionService());

			var result = strategy.CalculateUrgency(tasks);
			var metrics = new TaskMetricsService();
			var recurrenceRulesBySeriesId = tasks
				.Where(task => task.RecurrenceRule != null)
				.ToDictionary(task => task.SeriesId ?? task.Id, task => task.RecurrenceRule!);

			var scheduledTasks = result.Tasks
				.Where(t => t.ScheduledParts.Any())
				.Select(t => ToResponse(t, metrics, profile, recurrenceRulesBySeriesId))
				.ToList();
			var unscheduledTasks = result.UnscheduledTasks
				.GroupBy(task => task.Id)
				.Select(group => ToResponse(group.First(), metrics, profile, recurrenceRulesBySeriesId))
				.ToList();

			// Mirrors the CLI dashboard's "closest task to due date" pick (see ConsoleHelper.FindClosestTaskToDueDate).
			var leastSlackTask = result.Tasks
				.Where(t => t.DueDate.HasValue && t.ScheduledParts.Any() && !t.IsCompleted)
				.OrderBy(t => (t.DueDate!.Value - t.ScheduledParts.Min(p => p.StartTime)).Duration())
				.FirstOrDefault();

			var response = new LocalScheduleResponse(
				scheduledTasks,
				unscheduledTasks.Select(task => task.Id).Distinct().ToList(),
				leastSlackTask?.Id,
				leastSlackTask?.Title,
				leastSlackTask is null ? null : metrics.CalculateRealisticSlack(leastSlackTask, profile).TotalMinutes,
				leastSlackTask is null ? null : metrics.CalculateActualSlack(leastSlackTask, profile).TotalMinutes,
				unscheduledTasks);

			return Results.Ok(response);
		}

		private static LocalScheduledTaskResponse ToResponse(
			TaskItem task,
			TaskMetricsService metrics,
			UserProfile profile,
			IReadOnlyDictionary<Guid, RecurrenceRule> recurrenceRulesBySeriesId)
		{
			var seriesId = task.SeriesId ?? task.Id;
			recurrenceRulesBySeriesId.TryGetValue(seriesId, out var recurrenceRule);
			return new(
				task.Id,
				task.Title,
				task.DueDate,
				task.EstimatedDuration,
				task.IsPinned,
				task.ScheduledParts.Select(part =>
					new LocalScheduledChunkResponse(part.StartTime, part.EndTime)).ToList(),
				metrics.CalculateRealisticSlack(task, profile).TotalMinutes,
				metrics.CalculateActualSlack(task, profile).TotalMinutes,
				task.ListId,
				task.Description,
				task.Link,
				task.Importance,
				task.Complexity,
				task.EffectiveImportance,
				task.SeriesId,
				task.OccurrenceDate,
				task.Progress,
				(int)Math.Round(task.Progress * task.RequiredCompletions),
				task.RequiredCompletions,
				task.OccurrenceStatus,
				task.ShowMissedIndicator,
				task.ShowMissedIndicator &&
					(task.HasUnresolvedMissedIndicator || task.OccurrenceStatus == TaskOccurrenceStatus.Missed),
				task.OccurrenceStatus == TaskOccurrenceStatus.Missed,
				task.TrackStreak,
				task.TrackStreak ? task.CurrentStreak : 0,
				task.TrackStreak ? task.BestStreak : 0,
				task.ProgressionMode,
				recurrenceRule?.Clone());
		}

		private static bool IsValidRecurringTask(LocalTaskRequest task)
		{
			if (task.RecurrenceRule?.EndCondition == null ||
				task.RequiredCompletions < 1 ||
				!Enum.IsDefined(task.ProgressionMode) ||
				task.Dependencies is { Count: > 0 } ||
				task.OccurrenceStates is null)
			{
				return false;
			}

			var states = task.OccurrenceStates;
			if (states.Any(state => state is null ||
				!Enum.IsDefined(state.Status) ||
				state.CompletionCount < 0 ||
				state.CompletionCount > task.RequiredCompletions ||
				((state.Status == TaskOccurrenceStatus.Completed) !=
					(state.CompletionCount == task.RequiredCompletions)) ||
				(state.Status is TaskOccurrenceStatus.Skipped or TaskOccurrenceStatus.Disregarded &&
					state.CompletionCount != 0)))
			{
				return false;
			}

			return states
				.GroupBy(state => state.ScheduledDate.Date)
				.All(group => group.Count() == 1);
		}
	}
}

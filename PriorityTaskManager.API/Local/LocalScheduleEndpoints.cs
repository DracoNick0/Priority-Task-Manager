using PriorityTaskManager.Models;
using PriorityTaskManager.Scheduling.GoldPanning;
using PriorityTaskManager.Scheduling.Optimization;
using PriorityTaskManager.Services;

namespace PriorityTaskManager.API.Local
{
	/// <summary>
	/// Maps the unauthenticated, loopback-only local schedule-compute endpoint used by fully offline clients
	/// (e.g. the Flutter app's Hive-backed store) to run the real scheduling algorithms without an account/API
	/// login, per docs/VISION.md's MVP scope. Nothing here reads or writes server-side persistence: the client
	/// sends its current tasks/events/profile and gets back a computed schedule, keeping the client's own store
	/// (Hive) as the single source of truth instead of duplicating it server-side.
	/// </summary>
	public static class LocalScheduleEndpoints
	{
		public static void MapLocalScheduleEndpoints(this WebApplication app)
		{
			var group = app.MapGroup("/api/local");

			group.MapPost("/schedule", (LocalScheduleRequest request) =>
			{
				if (request.Profile is null)
				{
					return Results.BadRequest(new { error = "A user profile is required to compute a schedule." });
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
					: new GoldPanningStrategy(profile, events, timeService);

				var result = strategy.CalculateUrgency(tasks);
				var metrics = new TaskMetricsService();

				var scheduledTasks = result.Tasks
					.Where(t => t.ScheduledParts.Any())
					.Select(t => new LocalScheduledTaskResponse(
						t.Id,
						t.Title,
						t.DueDate,
						t.EstimatedDuration,
						t.IsPinned,
						t.ScheduledParts.Select(p => new LocalScheduledChunkResponse(p.StartTime, p.EndTime)).ToList(),
						metrics.CalculateRealisticSlack(t, profile).TotalMinutes,
						metrics.CalculateActualSlack(t, profile).TotalMinutes))
					.ToList();

				// Mirrors the CLI dashboard's "closest task to due date" pick (see ConsoleHelper.FindClosestTaskToDueDate).
				var leastSlackTask = result.Tasks
					.Where(t => t.DueDate.HasValue && t.ScheduledParts.Any() && !t.IsCompleted)
					.OrderBy(t => (t.DueDate!.Value - t.ScheduledParts.Min(p => p.StartTime)).Duration())
					.FirstOrDefault();

				var response = new LocalScheduleResponse(
					scheduledTasks,
					result.UnscheduledTasks.Select(t => t.Id).ToList(),
					leastSlackTask?.Id,
					leastSlackTask?.Title,
					leastSlackTask is null ? null : metrics.CalculateRealisticSlack(leastSlackTask, profile).TotalMinutes,
					leastSlackTask is null ? null : metrics.CalculateActualSlack(leastSlackTask, profile).TotalMinutes);

				return Results.Ok(response);
			});
		}
	}
}

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

			group.MapPost("/schedule", (LocalScheduleRequest request) => ScheduleComputation.Compute(request));
		}
	}
}


using PriorityTaskManager.API.Auth;
using PriorityTaskManager.API.Local;

namespace PriorityTaskManager.API.Schedule
{
	/// <summary>
	/// Maps the authenticated, Subscription-gated schedule-compute endpoint (see
	/// docs/ARCHITECTURE_INTEGRATIONS.md's "Planned Direction" section). Requires a valid JWT whose
	/// <see cref="SubscriptionPolicy.ClaimType"/> claim is <c>Subscription</c>; a Free-tier caller gets
	/// <c>403 Forbidden</c>. Reuses the same stateless computation as the unauthenticated
	/// <c>/api/local/schedule</c> route (<see cref="ScheduleComputation"/>): the caller posts its current
	/// tasks/events/profile and nothing is persisted server-side.
	/// </summary>
	public static class ScheduleEndpoints
	{
		public static void MapScheduleEndpoints(this WebApplication app)
		{
			app.MapPost("/api/schedule", (LocalScheduleRequest request) => ScheduleComputation.Compute(request))
				.RequireAuthorization(SubscriptionPolicy.PolicyName);
		}
	}
}

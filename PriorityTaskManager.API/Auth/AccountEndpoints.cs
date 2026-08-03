using System.Security.Claims;
using PriorityTaskManager.Services;

namespace PriorityTaskManager.API.Auth
{
	/// <summary>
	/// A minimal authenticated endpoint that proves the account-scoping wiring end to end (JWT claim
	/// resolution -> per-account <see cref="PriorityTaskManager.API.Persistence.PostgresPersistenceService"/>
	/// -> <see cref="TaskManagerService"/>) ahead of the full task/list/event REST surface, which is
	/// tracked by a future issue.
	/// </summary>
	public static class AccountEndpoints
	{
		public static void MapAccountEndpoints(this WebApplication app)
		{
			app.MapGet("/api/account/me", (ClaimsPrincipal user, TaskManagerService taskManagerService) =>
			{
				var email = user.FindFirstValue(ClaimTypes.Email);
				var accountId = user.FindFirstValue(ClaimTypes.NameIdentifier);
				return Results.Ok(new { accountId, email, taskCount = taskManagerService.GetAllTasks().Count });
			}).RequireAuthorization();
		}
	}
}

using PriorityTaskManager.Services;

namespace PriorityTaskManager.API.Profile
{
	/// <summary>
	/// Maps the REST endpoints for the account's global user profile, wrapping <see cref="TaskManagerService"/>
	/// per the integrations boundary (no persistence/scheduling logic lives here; see docs/ARCHITECTURE_INTEGRATIONS.md).
	/// </summary>
	public static class ProfileEndpoints
	{
		public static void MapProfileEndpoints(this WebApplication app)
		{
			var group = app.MapGroup("/api/profile").RequireAuthorization();

			group.MapGet("/", (TaskManagerService taskManagerService) =>
				Results.Ok(taskManagerService.GetUserProfile().ToResponse()));

			group.MapPut("/", (ProfileRequest request, TaskManagerService taskManagerService) =>
			{
				taskManagerService.UpdateUserProfile(request.ToUserProfile());
				return Results.Ok(taskManagerService.GetUserProfile().ToResponse());
			});
		}
	}
}

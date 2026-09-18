using PriorityTaskManager.Models;

namespace PriorityTaskManager.API.Profile
{
	/// <summary>Request body for updating the account's global user profile.</summary>
	public record ProfileRequest(
		SortOption DefaultListSortOption,
		TimeSpan DesiredBreatherDuration,
		TimeOnly WorkStartTime,
		TimeOnly WorkEndTime,
		List<DayOfWeek> WorkDays,
		SchedulingMode SchedulingMode,
		double SlackThresholdDire,
		double SlackThresholdPressing,
		double SlackThresholdFocus,
		double SlackThresholdSafe);

	/// <summary>Response body representing the account's global user profile.</summary>
	public record ProfileResponse(
		SortOption DefaultListSortOption,
		TimeSpan DesiredBreatherDuration,
		TimeOnly WorkStartTime,
		TimeOnly WorkEndTime,
		List<DayOfWeek> WorkDays,
		SchedulingMode SchedulingMode,
		double SlackThresholdDire,
		double SlackThresholdPressing,
		double SlackThresholdFocus,
		double SlackThresholdSafe);

	public static class ProfileDtoExtensions
	{
		public static ProfileResponse ToResponse(this UserProfile profile) => new(
			profile.DefaultListSortOption,
			profile.DesiredBreatherDuration,
			profile.WorkStartTime,
			profile.WorkEndTime,
			profile.WorkDays,
			profile.SchedulingMode,
			profile.SlackThresholdDire,
			profile.SlackThresholdPressing,
			profile.SlackThresholdFocus,
			profile.SlackThresholdSafe);

		/// <summary>Applies a request's fields onto a <see cref="UserProfile"/> for <c>TaskManagerService.UpdateUserProfile</c>.</summary>
		public static UserProfile ToUserProfile(this ProfileRequest request) => new()
		{
			DefaultListSortOption = request.DefaultListSortOption,
			DesiredBreatherDuration = request.DesiredBreatherDuration,
			WorkStartTime = request.WorkStartTime,
			WorkEndTime = request.WorkEndTime,
			WorkDays = new List<DayOfWeek>(request.WorkDays),
			SchedulingMode = request.SchedulingMode,
			SlackThresholdDire = request.SlackThresholdDire,
			SlackThresholdPressing = request.SlackThresholdPressing,
			SlackThresholdFocus = request.SlackThresholdFocus,
			SlackThresholdSafe = request.SlackThresholdSafe
		};
	}
}

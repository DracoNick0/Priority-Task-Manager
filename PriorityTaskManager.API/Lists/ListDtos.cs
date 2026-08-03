using PriorityTaskManager.Models;

namespace PriorityTaskManager.API.Lists
{
	/// <summary>Request body for creating/updating a task list.</summary>
	public record ListRequest(
		string Name,
		string? Description,
		SortOption? SortOption,
		SchedulingMode? SchedulingMode,
		TimeOnly? WorkStartTime,
		TimeOnly? WorkEndTime,
		List<DayOfWeek>? WorkDays,
		double? SlackThresholdDire,
		double? SlackThresholdPressing,
		double? SlackThresholdFocus,
		double? SlackThresholdSafe,
		DateTime? SimulatedTime);

	/// <summary>Response body representing a persisted task list.</summary>
	public record ListResponse(
		Guid Id,
		string Name,
		string? Description,
		SortOption? SortOption,
		SchedulingMode? SchedulingMode,
		TimeOnly? WorkStartTime,
		TimeOnly? WorkEndTime,
		List<DayOfWeek>? WorkDays,
		double? SlackThresholdDire,
		double? SlackThresholdPressing,
		double? SlackThresholdFocus,
		double? SlackThresholdSafe,
		DateTime? SimulatedTime);

	public static class ListDtoExtensions
	{
		public static ListResponse ToResponse(this TaskList list) => new(
			list.Id,
			list.Name,
			list.Description,
			list.SortOption,
			list.SchedulingMode,
			list.WorkStartTime,
			list.WorkEndTime,
			list.WorkDays,
			list.SlackThresholdDire,
			list.SlackThresholdPressing,
			list.SlackThresholdFocus,
			list.SlackThresholdSafe,
			list.SimulatedTime);

		/// <summary>Maps a request onto a new <see cref="TaskList"/>; <c>Id</c> is assigned by core on add.</summary>
		public static TaskList ToNewTaskList(this ListRequest request) => new()
		{
			Name = request.Name,
			Description = request.Description,
			SortOption = request.SortOption,
			SchedulingMode = request.SchedulingMode,
			WorkStartTime = request.WorkStartTime,
			WorkEndTime = request.WorkEndTime,
			WorkDays = request.WorkDays,
			SlackThresholdDire = request.SlackThresholdDire,
			SlackThresholdPressing = request.SlackThresholdPressing,
			SlackThresholdFocus = request.SlackThresholdFocus,
			SlackThresholdSafe = request.SlackThresholdSafe,
			SimulatedTime = request.SimulatedTime
		};

		/// <summary>Applies a request's editable fields onto <paramref name="id"/> for <c>TaskManagerService.UpdateList</c>.</summary>
		public static TaskList ToUpdatedTaskList(this ListRequest request, Guid id)
		{
			var list = request.ToNewTaskList();
			list.Id = id;
			return list;
		}
	}
}

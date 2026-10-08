using PriorityTaskManager.API.Events;
using PriorityTaskManager.API.Tasks;

namespace PriorityTaskManager.API.Archive
{
	/// <summary>Request body for restoring an archived task; <c>TargetListId</c> is only required
	/// when the task's original list no longer exists.</summary>
	public record RestoreArchivedTaskRequest(Guid? TargetListId);

	/// <summary>A restorable set of archived tasks or events.</summary>
	public record ArchiveGroupResponse(
		Guid GroupId,
		string Kind,
		List<TaskResponse> Tasks,
		List<EventResponse> Events);
}

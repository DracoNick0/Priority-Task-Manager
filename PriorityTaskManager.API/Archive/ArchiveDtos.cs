namespace PriorityTaskManager.API.Archive
{
	/// <summary>Request body for restoring an archived task; <c>TargetListId</c> is only required
	/// when the task's original list no longer exists.</summary>
	public record RestoreArchivedTaskRequest(Guid? TargetListId);
}

namespace PriorityTaskManager.Models
{
    /// <summary>
    /// A set of archived tasks or events that can be restored as one operation.
    /// </summary>
    /// <param name="GroupId">The stable identity shared by grouped archive items.</param>
    /// <param name="Kind">The archive item type: <c>tasks</c> or <c>events</c>.</param>
    /// <param name="Tasks">The group's tasks, or an empty list for event groups.</param>
    /// <param name="Events">The group's events, or an empty list for task groups.</param>
    public sealed record ArchiveGroup(
        Guid GroupId,
        string Kind,
        IReadOnlyList<TaskItem> Tasks,
        IReadOnlyList<Event> Events);
}

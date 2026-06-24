using PriorityTaskManager.Models;

namespace PriorityTaskManager.CLI.Utils
{
    /// <summary>
    /// Cached CLI-friendly schedule data that can be rendered without rerunning the scheduler.
    /// </summary>
    public class ScheduleSnapshot
    {
        public int ActiveListId { get; init; }

        public string ActiveListName { get; init; } = string.Empty;

        public SortOption ActiveListSortOption { get; init; } = SortOption.Default;

        public PrioritizationResult Result { get; init; } = new PrioritizationResult();

        public List<TaskItem> IncompleteTasks { get; init; } = new List<TaskItem>();

        public UserProfile UserProfile { get; init; } = new UserProfile();

        public List<Event> EventsForDay { get; init; } = new List<Event>();

        public DateTime Now { get; init; }

        public DateTime TargetDay { get; init; }

        public DateTime WorkStart { get; init; }

        public DateTime WorkEnd { get; init; }
    }
}

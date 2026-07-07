using PriorityTaskManager.Services;

namespace PriorityTaskManager.CLI.Utils
{
    /// <summary>
    /// Builds and caches the latest schedule snapshot for the active list.
    /// </summary>
    public class ScheduleSnapshotProvider
    {
        private readonly TaskManagerService _service;
        private readonly ITaskMetricsService _taskMetricsService;
        private readonly ITimeService _timeService;
        private readonly object _syncRoot = new object();

        private ScheduleSnapshot? _latestSnapshot;

        public ScheduleSnapshotProvider(
            TaskManagerService service,
            ITaskMetricsService taskMetricsService,
            ITimeService timeService)
        {
            _service = service;
            _taskMetricsService = taskMetricsService;
            _timeService = timeService;
        }

        public bool TryGetLatestSnapshot(out ScheduleSnapshot? snapshot)
        {
            lock (_syncRoot)
            {
                snapshot = _latestSnapshot;
                return snapshot != null;
            }
        }

        public bool RefreshActiveListSnapshot(out string? error)
        {
            error = null;

            var activeListId = _service.GetActiveListId();
            var activeList = _service.GetAllLists().FirstOrDefault(l => l.Id == activeListId);
            if (activeList == null)
            {
                error = $"Error: Active list with ID '{activeListId}' could not be found.";
                return false;
            }

            var effectiveProfile = _service.BuildEffectiveUserProfile(activeList);
            var result = _service.GetPrioritizedTasks(activeList.Id, _timeService);
            var incompleteTasks = result.Tasks.Where(t => !t.IsCompleted).ToList();
            var now = _timeService.GetCurrentTime();
            var targetDay = _taskMetricsService.FindTargetDayForSlackMeter(now, effectiveProfile);
            var workStart = targetDay.Date.Add(effectiveProfile.WorkStartTime.ToTimeSpan());
            var workEnd = targetDay.Date.Add(effectiveProfile.WorkEndTime.ToTimeSpan());

            var eventsForDay = _service.GetAllEvents()
                .Where(e => e.StartTime.Date == targetDay.Date)
                .OrderBy(e => e.StartTime)
                .ToList();

            var snapshot = new ScheduleSnapshot
            {
                ActiveListId = activeList.Id,
                ActiveListName = activeList.Name,
                ActiveListSortOption = activeList.SortOption ?? _service.UserProfile.DefaultListSortOption,
                Result = result,
                IncompleteTasks = incompleteTasks,
                UserProfile = effectiveProfile,
                EventsForDay = eventsForDay,
                Now = now,
                TargetDay = targetDay,
                WorkStart = workStart,
                WorkEnd = workEnd
            };

            lock (_syncRoot)
            {
                _latestSnapshot = snapshot;
            }

            return true;
        }
    }
}

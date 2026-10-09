using PriorityTaskManager.Models;
using PriorityTaskManager.Services;

namespace PriorityTaskManager.Scheduling.GoldPanning.Stages
{
    /// <summary>Expands recurring task series into ephemeral occurrences after the schedule horizon is known.</summary>
    public sealed class TaskOccurrenceExpansionStage : ISchedulingStage
    {
        private readonly ITimeService _timeService;
        private readonly IRecurrenceExpansionService _recurrenceExpansionService;

        /// <summary>Initializes the stage with deterministic time and recurrence expansion services.</summary>
        public TaskOccurrenceExpansionStage(
            ITimeService timeService,
            IRecurrenceExpansionService recurrenceExpansionService)
        {
            _timeService = timeService;
            _recurrenceExpansionService = recurrenceExpansionService;
        }

        /// <inheritdoc />
        public SchedulingContext Act(SchedulingContext context)
        {
            if (!context.SharedState.TryGetValue("Tasks", out var tasksValue) ||
                tasksValue is not List<TaskItem> tasks)
            {
                throw new InvalidOperationException("TaskOccurrenceExpansionStage requires a task list.");
            }

            if (!context.SharedState.TryGetValue("AvailableScheduleWindow", out var windowValue) ||
                windowValue is not ScheduleWindow window ||
                !window.HorizonEndDate.HasValue)
            {
                throw new InvalidOperationException("TaskOccurrenceExpansionStage requires a computed schedule horizon.");
            }

            var today = _timeService.GetCurrentTime().Date;
            var horizonEnd = window.HorizonEndDate.Value.Date;
            var expandedTasks = tasks.Where(task => task.RecurrenceRule == null).ToList();

            foreach (var series in tasks.Where(task => task.RecurrenceRule != null))
            {
                ValidateSeries(series);
                if (series.EstimatedDuration <= TimeSpan.Zero)
                    continue;

                var rule = series.RecurrenceRule!;
                var seriesId = series.SeriesId ?? series.Id;
                var dates = _recurrenceExpansionService.GetOccurrences(
                    rule,
                    Array.Empty<RecurrenceException>(),
                    rule.SeriesStartDate.Date,
                    horizonEnd);
                var lookaheadDate = GetLookaheadDate(series, dates, horizonEnd);
                var states = series.OccurrenceStates.ToDictionary(state => state.ScheduledDate.Date);
                var eligibleDates = GetEligibleDates(
                    series,
                    dates,
                    lookaheadDate,
                    states,
                    today,
                    horizonEnd);

                foreach (var date in eligibleDates)
                {
                    states.TryGetValue(date, out var state);
                    var notBefore = date == lookaheadDate
                        ? dates[^1].Date
                        : date;
                    var occurrence = CreateOccurrence(series, seriesId, date, notBefore, state);
                    if (occurrence.EstimatedDuration > TimeSpan.Zero)
                        expandedTasks.Add(occurrence);
                }
            }

            context.SharedState["Tasks"] = expandedTasks;
            context.History.Add("TaskOccurrenceExpansionStage: Expanded recurring task dates within the computed horizon.");
            return context;
        }

        private static void ValidateSeries(TaskItem series)
        {
            if (series.Id == Guid.Empty)
                throw new InvalidOperationException("A recurring task series must have an ID.");
            if (series.RecurrenceRule?.EndCondition == null)
                throw new InvalidOperationException($"Recurring task '{series.Id}' has no recurrence end condition.");
            if (series.RequiredCompletions < 1)
                throw new InvalidOperationException($"Recurring task '{series.Id}' has an invalid RequiredCompletions value.");
            if (series.Dependencies.Count != 0)
                throw new InvalidOperationException($"Recurring task '{series.Id}' cannot have dependencies.");
            if (!Enum.IsDefined(series.ProgressionMode))
                throw new InvalidOperationException($"Recurring task '{series.Id}' has an invalid progression mode.");
            if (series.OccurrenceStates == null)
                throw new InvalidOperationException($"Recurring task '{series.Id}' has no occurrence-state metadata.");
            if (series.OccurrenceStates.Any(state =>
                !Enum.IsDefined(state.Status) ||
                state.CompletionCount < 0 ||
                state.CompletionCount > series.RequiredCompletions ||
                ((state.Status == TaskOccurrenceStatus.Completed) !=
                    (state.CompletionCount == series.RequiredCompletions)) ||
                (state.Status is TaskOccurrenceStatus.Skipped or TaskOccurrenceStatus.Disregarded &&
                    state.CompletionCount != 0)))
            {
                throw new InvalidOperationException($"Recurring task '{series.Id}' has invalid occurrence progress.");
            }
            if (series.OccurrenceStates.GroupBy(state => state.ScheduledDate.Date).Any(group => group.Count() > 1))
                throw new InvalidOperationException($"Recurring task '{series.Id}' has duplicate occurrence-state dates.");
        }

        private static List<DateTime> GetEligibleDates(
            TaskItem series,
            IReadOnlyList<DateTime> generatedDates,
            DateTime? lookaheadDate,
            IReadOnlyDictionary<DateTime, TaskOccurrenceState> states,
            DateTime today,
            DateTime horizonEnd)
        {
            bool IsUnresolved(DateTime date) =>
                !states.TryGetValue(date.Date, out var state) ||
                state.Status is TaskOccurrenceStatus.Pending or TaskOccurrenceStatus.Missed;

            if (series.ProgressionMode == TaskProgressionMode.SequentialCatchUp)
            {
                var oldestUnresolved = generatedDates.FirstOrDefault(date => IsUnresolved(date.Date));
                return oldestUnresolved == default ? new List<DateTime>() : new List<DateTime> { oldestUnresolved.Date };
            }

            var result = generatedDates
                .Where(date => date.Date >= today && date.Date <= horizonEnd && IsUnresolved(date.Date))
                .Select(date => date.Date)
                .ToHashSet();

            if (lookaheadDate.HasValue && IsUnresolved(lookaheadDate.Value))
                result.Add(lookaheadDate.Value.Date);

            if (series.ProgressionMode == TaskProgressionMode.RollForwardKeepBacklog)
            {
                var generatedDateSet = generatedDates.Select(date => date.Date).ToHashSet();
                result.UnionWith(states.Values
                    .Where(state => state.Status == TaskOccurrenceStatus.Missed &&
                        generatedDateSet.Contains(state.ScheduledDate.Date))
                    .Select(state => state.ScheduledDate.Date));
            }

            return result.OrderBy(date => date).ToList();
        }

        private DateTime? GetLookaheadDate(
            TaskItem series,
            IReadOnlyList<DateTime> generatedDates,
            DateTime horizonEnd)
        {
            if (series.ProgressionMode == TaskProgressionMode.SequentialCatchUp ||
                generatedDates.Count == 0 ||
                horizonEnd.Date >= DateTime.MaxValue.Date.AddDays(-366))
            {
                return null;
            }

            var nextDate = _recurrenceExpansionService.GetOccurrences(
                    series.RecurrenceRule!,
                    Array.Empty<RecurrenceException>(),
                    horizonEnd.Date.AddDays(1),
                    horizonEnd.Date.AddDays(366))
                .FirstOrDefault();
            if (nextDate == default)
            {
                if (series.RecurrenceRule is not ExplicitDatesRecurrenceRule)
                    return null;

                nextDate = _recurrenceExpansionService.GetOccurrences(
                        series.RecurrenceRule,
                        Array.Empty<RecurrenceException>(),
                        horizonEnd.Date.AddDays(1),
                        DateTime.MaxValue.Date)
                    .FirstOrDefault();
            }

            return nextDate == default ? null : nextDate.Date;
        }

        private static TaskItem CreateOccurrence(
            TaskItem series,
            Guid seriesId,
            DateTime date,
            DateTime notBefore,
            TaskOccurrenceState? state)
        {
            var completionCount = state?.CompletionCount ?? 0;
            var occurrence = series.Clone();
            occurrence.Id = TaskOccurrence.CreateId(seriesId, date);
            occurrence.SeriesId = seriesId;
            occurrence.OccurrenceDate = date.Date;
            occurrence.RecurrenceRule = null;
            occurrence.IsCompleted = false;
            occurrence.Progress = (double)completionCount / series.RequiredCompletions;
            occurrence.DueDate = date.Date;
            occurrence.NotBefore = series.NotBefore.HasValue && series.NotBefore.Value > notBefore
                ? series.NotBefore
                : notBefore.Date;
            occurrence.OccurrenceStatus = state?.Status ?? TaskOccurrenceStatus.Pending;
            occurrence.ScheduledParts.Clear();
            occurrence.EstimatedDuration = series.EstimatedDuration;
            TaskNormalizationStage.NormalizeOccurrence(occurrence);
            return occurrence;
        }
    }
}

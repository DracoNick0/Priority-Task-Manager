using PriorityTaskManager.Models;
using PriorityTaskManager.Services.Helpers;

namespace PriorityTaskManager.Services
{
    /// <summary>
    /// Coordinates CRUD operations for calendar events, persisting changes through <see cref="IPersistenceService"/>.
    /// </summary>
    public class EventService : IEventService
    {
        private readonly IPersistenceService _persistenceService;
        private readonly DataContainer _data;

        /// <summary>
        /// Initializes a new instance of the EventService class over the shared application data container.
        /// </summary>
        /// <param name="persistenceService">The persistence service used to save changes.</param>
        /// <param name="data">The shared in-memory data container.</param>
        public EventService(IPersistenceService persistenceService, DataContainer data)
        {
            _persistenceService = persistenceService;
            _data = data;
        }

        /// <inheritdoc />
        public void AddEvent(Event newEvent)
        {
            ValidateTimeRange(newEvent.StartTime, newEvent.EndTime);
            newEvent.Id = Guid.NewGuid();
            // A recurrence rule marks this event as the base occurrence defining a new series.
            if (newEvent.RecurrenceRule != null)
            {
                newEvent.SeriesId = newEvent.Id;
            }
            _data.Events.Add(newEvent);
            _persistenceService.SaveData(_data);
        }

        /// <inheritdoc />
        public IEnumerable<Event> GetAllEvents()
        {
            return _data.Events;
        }

        /// <inheritdoc />
        public Event? GetEvent(Guid id)
        {
            return _data.Events.Find(e => e.Id == id);
        }

        /// <summary>Expands persisted events for a display window without persisting generated occurrences.</summary>
        public IEnumerable<EventOccurrence> GetEventOccurrences(DateTime rangeStart, DateTime rangeEnd)
            => ExpandEventOccurrences(rangeStart, rangeEnd).OrderBy(o => o.Event.StartTime);

        private IEnumerable<EventOccurrence> ExpandEventOccurrences(DateTime rangeStart, DateTime rangeEnd)
        {
            var expansion = new RecurrenceExpansionService();
            foreach (var series in _data.Events)
            {
                if (series.RecurrenceRule == null)
                {
                    if (series.EndTime > rangeStart.Date && series.StartTime < rangeEnd.Date.AddDays(1))
                        yield return new EventOccurrence(series, null);
                    continue;
                }

                var overnightDays = Math.Max(0, (series.EndTime.Date - series.StartTime.Date).Days);
                var dates = expansion.GetOccurrences(series.RecurrenceRule, series.Exceptions,
                    rangeStart.Date.AddDays(-overnightDays), rangeEnd).ToList();
                foreach (var moved in series.OccurrenceOverrides.Where(o => o.StartTime.Date <= rangeEnd.Date && o.EndTime > rangeStart.Date))
                {
                    if (!dates.Contains(moved.OriginalOccurrenceDate.Date) &&
                        expansion.GetOccurrences(series.RecurrenceRule, series.Exceptions,
                            moved.OriginalOccurrenceDate, moved.OriginalOccurrenceDate).Count != 0)
                        dates.Add(moved.OriginalOccurrenceDate.Date);
                }

                foreach (var date in dates)
                {
                    var edited = series.OccurrenceOverrides.FirstOrDefault(o => o.OriginalOccurrenceDate.Date == date);
                    var start = edited?.StartTime ?? date.Date.Add(series.StartTime.TimeOfDay);
                    var end = edited?.EndTime ?? start.Add(series.EndTime - series.StartTime);
                    if (end <= rangeStart.Date || start >= rangeEnd.Date.AddDays(1))
                        continue;
                    yield return new EventOccurrence(new Event
                    {
                        Id = series.Id,
                        SeriesId = series.SeriesId,
                        Name = edited?.Name ?? series.Name,
                        Description = edited?.Description ?? series.Description,
                        Link = edited?.Link ?? series.Link,
                        StartTime = start,
                        EndTime = end,
                        RecurrenceRule = series.RecurrenceRule
                    }, date);
                }
            }
        }

        /// <inheritdoc />
        public bool UpdateEvent(Event updatedEvent)
        {
            var existingEvent = _data.Events.Find(e => e.Id == updatedEvent.Id);
            if (existingEvent == null)
                return false;

            ValidateTimeRange(updatedEvent.StartTime, updatedEvent.EndTime);
            existingEvent.Name = updatedEvent.Name;
            existingEvent.Description = updatedEvent.Description;
            existingEvent.Link = updatedEvent.Link;
            existingEvent.StartTime = updatedEvent.StartTime;
            existingEvent.EndTime = updatedEvent.EndTime;
            _persistenceService.SaveData(_data);
            return true;
        }

        /// <inheritdoc />
        public bool DeleteEvent(Guid id)
        {
            var eventToDelete = _data.Events.FirstOrDefault(e => e.Id == id);
            if (eventToDelete == null)
            {
                return false;
            }

            _data.Events.Remove(eventToDelete);
            _persistenceService.SaveData(_data);
            return true;
        }

        /// <inheritdoc />
        public bool ArchiveEvent(Guid id)
        {
            var eventToArchive = _data.Events.FirstOrDefault(e => e.Id == id);
            if (eventToArchive == null)
                return false;

            eventToArchive.ArchiveGroupId = eventToArchive.SeriesId ?? eventToArchive.Id;
            _persistenceService.ArchiveEvents(new[] { eventToArchive });
            _data.Events.Remove(eventToArchive);
            _persistenceService.SaveData(_data);
            return true;
        }

        /// <inheritdoc />
        public bool ArchiveOccurrence(Guid seriesId, DateTime occurrenceDate, RecurrenceEditTarget target)
        {
            var seriesEvent = _data.Events.Find(e => e.Id == seriesId);
            if (seriesEvent?.RecurrenceRule == null)
                return false;

            var occurrenceDateOnly = occurrenceDate.Date;
            var occurrence = GetEventOccurrences(occurrenceDateOnly, occurrenceDateOnly)
                .FirstOrDefault(o => o.Event.Id == seriesId &&
                    o.OriginalOccurrenceDate?.Date == occurrenceDateOnly);
            if (occurrence == null)
                return false;

            if (target == RecurrenceEditTarget.AllOccurrences)
                return ArchiveEvent(seriesId);

            Event archivedEvent;
            switch (target)
            {
                case RecurrenceEditTarget.ThisOccurrence:
                    archivedEvent = new Event
                    {
                        Id = Guid.NewGuid(),
                        Name = occurrence.Event.Name,
                        Description = occurrence.Event.Description,
                        Link = occurrence.Event.Link,
                        StartTime = occurrence.Event.StartTime,
                        EndTime = occurrence.Event.EndTime,
                        ArchiveGroupId = seriesId
                    };
                    _persistenceService.ArchiveEvents(new[] { archivedEvent });
                    seriesEvent.Exceptions.RemoveAll(e => e.OriginalOccurrenceDate.Date == occurrenceDateOnly);
                    seriesEvent.Exceptions.Add(new RecurrenceException
                    {
                        OriginalOccurrenceDate = occurrenceDateOnly,
                        IsCancelled = true
                    });
                    seriesEvent.OccurrenceOverrides.RemoveAll(o => o.OriginalOccurrenceDate.Date == occurrenceDateOnly);
                    break;

                case RecurrenceEditTarget.ThisAndFollowing:
                    var split = RecurrenceSplitHelper.Split(
                        seriesEvent.RecurrenceRule,
                        seriesEvent.Exceptions,
                        occurrenceDateOnly);
                    var archivedId = Guid.NewGuid();
                    archivedEvent = new Event
                    {
                        Id = archivedId,
                        SeriesId = archivedId,
                        Name = seriesEvent.Name,
                        Description = seriesEvent.Description,
                        Link = seriesEvent.Link,
                        StartTime = occurrenceDateOnly.Add(seriesEvent.StartTime.TimeOfDay),
                        EndTime = occurrenceDateOnly.Add(seriesEvent.StartTime.TimeOfDay)
                            .Add(seriesEvent.EndTime - seriesEvent.StartTime),
                        RecurrenceRule = split.NewRule,
                        Exceptions = split.NewExceptions,
                        ArchiveGroupId = seriesId,
                        OccurrenceOverrides = seriesEvent.OccurrenceOverrides
                            .Where(o => o.OriginalOccurrenceDate.Date >= occurrenceDateOnly)
                            .Select(o => o.Clone())
                            .ToList()
                    };
                    _persistenceService.ArchiveEvents(new[] { archivedEvent });

                    var priorOccurrences = new RecurrenceExpansionService().GetOccurrences(
                        seriesEvent.RecurrenceRule,
                        Array.Empty<RecurrenceException>(),
                        seriesEvent.RecurrenceRule.SeriesStartDate,
                        occurrenceDateOnly.AddDays(-1));
                    if (priorOccurrences.Count == 0)
                    {
                        _data.Events.Remove(seriesEvent);
                    }
                    else
                    {
                        seriesEvent.RecurrenceRule = split.PriorRule;
                        seriesEvent.Exceptions = split.PriorExceptions;
                        seriesEvent.OccurrenceOverrides = seriesEvent.OccurrenceOverrides
                            .Where(o => o.OriginalOccurrenceDate.Date < occurrenceDateOnly)
                            .ToList();
                    }
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(target));
            }

            _persistenceService.SaveData(_data);
            return true;
        }

        /// <inheritdoc />
        public List<Event> GetArchivedEvents() => _persistenceService.GetArchivedEvents();

        /// <inheritdoc />
        public bool RestoreArchivedEvent(Guid eventId)
        {
            var archivedEvent = _persistenceService.GetArchivedEvents().FirstOrDefault(e => e.Id == eventId);
            if (archivedEvent == null)
                return false;

            _persistenceService.RemoveArchivedEvent(eventId);
            archivedEvent.ArchiveGroupId = null;
            _data.Events.Add(archivedEvent);
            _persistenceService.SaveData(_data);
            return true;
        }

        /// <inheritdoc />
        public bool RestoreArchivedEventGroup(Guid groupId)
        {
            var archivedEvents = _persistenceService.GetArchivedEvents()
                .Where(e => (e.ArchiveGroupId ?? e.SeriesId ?? e.Id) == groupId)
                .ToList();
            if (archivedEvents.Count == 0)
                return false;

            _persistenceService.RemoveArchivedEvents(archivedEvents.Select(e => e.Id));
            foreach (var archivedEvent in archivedEvents)
            {
                archivedEvent.ArchiveGroupId = null;
                _data.Events.Add(archivedEvent);
            }
            _persistenceService.SaveData(_data);
            return true;
        }

        /// <inheritdoc />
        public void ClearEvents()
        {
            _data.Events.Clear();
            _persistenceService.SaveData(_data);
        }

        /// <inheritdoc />
        public bool EditOccurrence(Guid seriesId, DateTime occurrenceDate, string name, DateTime startTime, DateTime endTime, RecurrenceEditTarget target, RecurrenceRule? recurrenceRule = null, string description = "", string? link = null)
        {
            ValidateTimeRange(startTime, endTime);
            var seriesEvent = _data.Events.Find(e => e.Id == seriesId);
            if (seriesEvent?.RecurrenceRule == null)
                return false;

            var occurrenceDateOnly = occurrenceDate.Date;
            if (new RecurrenceExpansionService().GetOccurrences(seriesEvent.RecurrenceRule,
                    Array.Empty<RecurrenceException>(), occurrenceDateOnly, occurrenceDateOnly).Count == 0)
                return false;

            var replacementRule = recurrenceRule?.Clone();
            if (replacementRule != null && target == RecurrenceEditTarget.ThisAndFollowing)
            {
                replacementRule.SeriesStartDate = occurrenceDateOnly;
                if (new RecurrenceExpansionService().GetOccurrences(replacementRule,
                        Array.Empty<RecurrenceException>(), occurrenceDateOnly, occurrenceDateOnly).Count == 0)
                    throw new ArgumentException("The replacement recurrence must include the selected occurrence date.", nameof(recurrenceRule));
            }

            switch (target)
            {
                case RecurrenceEditTarget.ThisOccurrence:
                    // A single occurrence override has no pattern of its own, so any supplied
                    // recurrenceRule is ignored here.
                    seriesEvent.OccurrenceOverrides.RemoveAll(o => o.OriginalOccurrenceDate.Date == occurrenceDateOnly);
                    seriesEvent.Exceptions.RemoveAll(e => e.OriginalOccurrenceDate.Date == occurrenceDateOnly);
                    seriesEvent.OccurrenceOverrides.Add(new EventOccurrenceOverride
                    {
                        OriginalOccurrenceDate = occurrenceDateOnly,
                        Name = name,
                        Description = description,
                        Link = link,
                        StartTime = startTime,
                        EndTime = endTime
                    });
                    break;

                case RecurrenceEditTarget.ThisAndFollowing:
                        if (new RecurrenceExpansionService().GetOccurrences(seriesEvent.RecurrenceRule,
                            Array.Empty<RecurrenceException>(), seriesEvent.RecurrenceRule.SeriesStartDate, occurrenceDateOnly.AddDays(-1)).Count == 0)
                    {
                        seriesEvent.Name = name;
                        seriesEvent.Description = description;
                        seriesEvent.Link = link;
                        seriesEvent.StartTime = occurrenceDateOnly.Add(startTime.TimeOfDay);
                        seriesEvent.EndTime = seriesEvent.StartTime.Add(endTime - startTime);
                        seriesEvent.RecurrenceRule = replacementRule ?? seriesEvent.RecurrenceRule;
                        seriesEvent.Exceptions.RemoveAll(e => e.OriginalOccurrenceDate.Date == occurrenceDateOnly);
                        seriesEvent.OccurrenceOverrides.RemoveAll(o => o.OriginalOccurrenceDate.Date == occurrenceDateOnly);
                        if (startTime.Date != occurrenceDateOnly)
                            seriesEvent.OccurrenceOverrides.Add(new EventOccurrenceOverride
                            {
                                OriginalOccurrenceDate = occurrenceDateOnly,
                                Name = name,
                                Description = description,
                                Link = link,
                                StartTime = startTime,
                                EndTime = endTime
                            });
                        break;
                    }
                    var split = RecurrenceSplitHelper.Split(seriesEvent.RecurrenceRule, seriesEvent.Exceptions, occurrenceDateOnly);
                    var priorOverrides = seriesEvent.OccurrenceOverrides.Where(o => o.OriginalOccurrenceDate.Date < occurrenceDateOnly).ToList();
                    var followingOverrides = seriesEvent.OccurrenceOverrides.Where(o => o.OriginalOccurrenceDate.Date > occurrenceDateOnly).ToList();

                    seriesEvent.RecurrenceRule = split.PriorRule;
                    seriesEvent.Exceptions = split.PriorExceptions;
                    seriesEvent.OccurrenceOverrides = priorOverrides;

                    var newSeriesEvent = new Event
                    {
                        Id = Guid.NewGuid(),
                        Name = name,
                        Description = description,
                        Link = link,
                        StartTime = occurrenceDateOnly.Add(startTime.TimeOfDay),
                        EndTime = occurrenceDateOnly.Add(startTime.TimeOfDay).Add(endTime - startTime),
                        RecurrenceRule = replacementRule ?? split.NewRule,
                        Exceptions = split.NewExceptions.Where(e => e.OriginalOccurrenceDate.Date != occurrenceDateOnly).ToList(),
                        OccurrenceOverrides = followingOverrides
                    };
                    if (startTime.Date != occurrenceDateOnly)
                        newSeriesEvent.OccurrenceOverrides.Add(new EventOccurrenceOverride { OriginalOccurrenceDate = occurrenceDateOnly, Name = name, Description = description, Link = link, StartTime = startTime, EndTime = endTime });
                    newSeriesEvent.SeriesId = newSeriesEvent.Id;
                    _data.Events.Add(newSeriesEvent);
                    break;

                case RecurrenceEditTarget.AllOccurrences:
                    seriesEvent.Name = name;
                    seriesEvent.Description = description;
                    seriesEvent.Link = link;
                    seriesEvent.StartTime = seriesEvent.StartTime.Date.Add(startTime.TimeOfDay);
                    seriesEvent.EndTime = seriesEvent.StartTime.Add(endTime - startTime);
                    if (replacementRule != null)
                    {
                        replacementRule.SeriesStartDate = seriesEvent.RecurrenceRule.SeriesStartDate;
                        seriesEvent.RecurrenceRule = replacementRule;
                    }
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(target));
            }

            _persistenceService.SaveData(_data);
            return true;
        }

        private static void ValidateTimeRange(DateTime startTime, DateTime endTime)
        {
            if (endTime <= startTime)
            {
                throw new ArgumentException("Event end time must be after its start time.", nameof(endTime));
            }
        }

        /// <inheritdoc />
        public bool DeleteOccurrence(Guid seriesId, DateTime occurrenceDate, RecurrenceEditTarget target)
        {
            var seriesEvent = _data.Events.Find(e => e.Id == seriesId);
            if (seriesEvent?.RecurrenceRule == null)
                return false;

            var occurrenceDateOnly = occurrenceDate.Date;
            if (new RecurrenceExpansionService().GetOccurrences(seriesEvent.RecurrenceRule,
                    Array.Empty<RecurrenceException>(), occurrenceDateOnly, occurrenceDateOnly).Count == 0)
                return false;

            switch (target)
            {
                case RecurrenceEditTarget.ThisOccurrence:
                    seriesEvent.Exceptions.RemoveAll(e => e.OriginalOccurrenceDate.Date == occurrenceDateOnly);
                    seriesEvent.Exceptions.Add(new RecurrenceException { OriginalOccurrenceDate = occurrenceDateOnly, IsCancelled = true });
                    seriesEvent.OccurrenceOverrides.RemoveAll(o => o.OriginalOccurrenceDate.Date == occurrenceDateOnly);
                    break;

                case RecurrenceEditTarget.ThisAndFollowing:
                        if (new RecurrenceExpansionService().GetOccurrences(seriesEvent.RecurrenceRule,
                            Array.Empty<RecurrenceException>(), seriesEvent.RecurrenceRule.SeriesStartDate, occurrenceDateOnly.AddDays(-1)).Count == 0)
                    {
                        _data.Events.Remove(seriesEvent);
                        break;
                    }
                    var split = RecurrenceSplitHelper.Split(seriesEvent.RecurrenceRule, seriesEvent.Exceptions, occurrenceDateOnly);
                    seriesEvent.RecurrenceRule = split.PriorRule;
                    seriesEvent.Exceptions = split.PriorExceptions;
                    seriesEvent.OccurrenceOverrides = seriesEvent.OccurrenceOverrides
                        .Where(o => o.OriginalOccurrenceDate.Date < occurrenceDateOnly)
                        .ToList();
                    break;

                case RecurrenceEditTarget.AllOccurrences:
                    _data.Events.Remove(seriesEvent);
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(target));
            }

            _persistenceService.SaveData(_data);
            return true;
        }
    }
}

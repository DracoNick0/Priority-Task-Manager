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

        /// <inheritdoc />
        public bool UpdateEvent(Event updatedEvent)
        {
            var existingEvent = _data.Events.Find(e => e.Id == updatedEvent.Id);
            if (existingEvent == null)
                return false;

            existingEvent.Name = updatedEvent.Name;
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
        public void ClearEvents()
        {
            _data.Events.Clear();
            _persistenceService.SaveData(_data);
        }

        /// <inheritdoc />
        public bool EditOccurrence(Guid seriesId, DateTime occurrenceDate, string name, DateTime startTime, DateTime endTime, RecurrenceEditTarget target)
        {
            var seriesEvent = _data.Events.Find(e => e.Id == seriesId);
            if (seriesEvent?.RecurrenceRule == null)
                return false;

            var occurrenceDateOnly = occurrenceDate.Date;

            switch (target)
            {
                case RecurrenceEditTarget.ThisOccurrence:
                    seriesEvent.OccurrenceOverrides.RemoveAll(o => o.OriginalOccurrenceDate.Date == occurrenceDateOnly);
                    seriesEvent.OccurrenceOverrides.Add(new EventOccurrenceOverride
                    {
                        OriginalOccurrenceDate = occurrenceDateOnly,
                        Name = name,
                        StartTime = startTime,
                        EndTime = endTime
                    });
                    break;

                case RecurrenceEditTarget.ThisAndFollowing:
                    var split = RecurrenceSplitHelper.Split(seriesEvent.RecurrenceRule, seriesEvent.Exceptions, occurrenceDateOnly);
                    var priorOverrides = seriesEvent.OccurrenceOverrides.Where(o => o.OriginalOccurrenceDate.Date < occurrenceDateOnly).ToList();
                    var followingOverrides = seriesEvent.OccurrenceOverrides.Where(o => o.OriginalOccurrenceDate.Date >= occurrenceDateOnly).ToList();

                    seriesEvent.RecurrenceRule = split.PriorRule;
                    seriesEvent.Exceptions = split.PriorExceptions;
                    seriesEvent.OccurrenceOverrides = priorOverrides;

                    var newSeriesEvent = new Event
                    {
                        Id = Guid.NewGuid(),
                        Name = name,
                        StartTime = startTime,
                        EndTime = endTime,
                        RecurrenceRule = split.NewRule,
                        Exceptions = split.NewExceptions,
                        OccurrenceOverrides = followingOverrides
                    };
                    newSeriesEvent.SeriesId = newSeriesEvent.Id;
                    _data.Events.Add(newSeriesEvent);
                    break;

                case RecurrenceEditTarget.AllOccurrences:
                    seriesEvent.Name = name;
                    seriesEvent.StartTime = startTime;
                    seriesEvent.EndTime = endTime;
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(target));
            }

            _persistenceService.SaveData(_data);
            return true;
        }

        /// <inheritdoc />
        public bool DeleteOccurrence(Guid seriesId, DateTime occurrenceDate, RecurrenceEditTarget target)
        {
            var seriesEvent = _data.Events.Find(e => e.Id == seriesId);
            if (seriesEvent?.RecurrenceRule == null)
                return false;

            var occurrenceDateOnly = occurrenceDate.Date;

            switch (target)
            {
                case RecurrenceEditTarget.ThisOccurrence:
                    seriesEvent.Exceptions.RemoveAll(e => e.OriginalOccurrenceDate.Date == occurrenceDateOnly);
                    seriesEvent.Exceptions.Add(new RecurrenceException { OriginalOccurrenceDate = occurrenceDateOnly, IsCancelled = true });
                    seriesEvent.OccurrenceOverrides.RemoveAll(o => o.OriginalOccurrenceDate.Date == occurrenceDateOnly);
                    break;

                case RecurrenceEditTarget.ThisAndFollowing:
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

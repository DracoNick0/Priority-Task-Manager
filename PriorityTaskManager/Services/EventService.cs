using PriorityTaskManager.Models;

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
            newEvent.Id = _data.NextEventId++;
            _data.Events.Add(newEvent);
            _persistenceService.SaveData(_data);
        }

        /// <inheritdoc />
        public IEnumerable<Event> GetAllEvents()
        {
            return _data.Events;
        }

        /// <inheritdoc />
        public Event? GetEvent(int id)
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
        public bool DeleteEvent(int id)
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
    }
}

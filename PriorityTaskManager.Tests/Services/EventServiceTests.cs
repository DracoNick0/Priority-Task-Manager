using PriorityTaskManager.Models;
using PriorityTaskManager.Services;

namespace PriorityTaskManager.Tests.Services
{
    public class EventServiceTests
    {
        private static (EventService service, DataContainer data) CreateService()
        {
            var data = new DataContainer();
            var persistence = new SpySaveCountPersistenceService();
            var service = new EventService(persistence, data);
            return (service, data);
        }

        [Fact]
        public void AddEvent_AssignsIdAndPersists()
        {
            var (service, data) = CreateService();
            var newEvent = new Event { Name = "Doctor", StartTime = new DateTime(2026, 7, 10, 9, 0, 0), EndTime = new DateTime(2026, 7, 10, 10, 0, 0) };

            service.AddEvent(newEvent);

            Assert.NotEqual(Guid.Empty, newEvent.Id);
            Assert.Single(data.Events);
        }

        [Fact]
        public void GetEvent_ReturnsMatchingEvent_WhenPresent()
        {
            var (service, _) = CreateService();
            var newEvent = new Event { Name = "Doctor" };
            service.AddEvent(newEvent);

            var found = service.GetEvent(newEvent.Id);

            Assert.NotNull(found);
            Assert.Equal("Doctor", found!.Name);
        }

        [Fact]
        public void GetEvent_ReturnsNull_WhenMissing()
        {
            var (service, _) = CreateService();

            Assert.Null(service.GetEvent(Guid.NewGuid()));
        }

        [Fact]
        public void UpdateEvent_ModifiesExistingEvent_AndReturnsTrue()
        {
            var (service, _) = CreateService();
            var newEvent = new Event { Name = "Doctor", StartTime = new DateTime(2026, 7, 10, 9, 0, 0), EndTime = new DateTime(2026, 7, 10, 10, 0, 0) };
            service.AddEvent(newEvent);

            var updated = new Event { Id = newEvent.Id, Name = "Dentist", StartTime = new DateTime(2026, 7, 11, 9, 0, 0), EndTime = new DateTime(2026, 7, 11, 10, 0, 0) };
            var result = service.UpdateEvent(updated);

            Assert.True(result);
            Assert.Equal("Dentist", service.GetEvent(newEvent.Id)!.Name);
        }

        [Fact]
        public void UpdateEvent_ReturnsFalse_WhenEventDoesNotExist()
        {
            var (service, _) = CreateService();

            var result = service.UpdateEvent(new Event { Id = Guid.NewGuid(), Name = "Missing" });

            Assert.False(result);
        }

        [Fact]
        public void DeleteEvent_RemovesEvent_AndReturnsTrue()
        {
            var (service, data) = CreateService();
            var newEvent = new Event { Name = "Doctor" };
            service.AddEvent(newEvent);

            var result = service.DeleteEvent(newEvent.Id);

            Assert.True(result);
            Assert.Empty(data.Events);
        }

        [Fact]
        public void DeleteEvent_ReturnsFalse_WhenEventDoesNotExist()
        {
            var (service, _) = CreateService();

            Assert.False(service.DeleteEvent(Guid.NewGuid()));
        }

        [Fact]
        public void ClearEvents_RemovesAllEvents()
        {
            var (service, data) = CreateService();
            service.AddEvent(new Event { Name = "A" });
            service.AddEvent(new Event { Name = "B" });

            service.ClearEvents();

            Assert.Empty(data.Events);
        }

        [Fact]
        public void GetAllEvents_ReturnsAllAddedEvents()
        {
            var (service, _) = CreateService();
            service.AddEvent(new Event { Name = "A" });
            service.AddEvent(new Event { Name = "B" });

            Assert.Equal(2, service.GetAllEvents().Count());
        }

        private class SpySaveCountPersistenceService : IPersistenceService
        {
            public int SaveCount { get; private set; }

            public DataContainer LoadData() => new DataContainer();

            public void SaveData(DataContainer data) => SaveCount++;

            public void ArchiveTasks(IEnumerable<TaskItem> tasksToArchive) { }
        }
    }
}

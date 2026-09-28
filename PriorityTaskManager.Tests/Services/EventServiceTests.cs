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
        public void AddEvent_WithRecurrenceRule_AssignsSeriesIdMatchingId()
        {
            var (service, _) = CreateService();
            var newEvent = new Event
            {
                Name = "Standup",
                StartTime = new DateTime(2026, 7, 10, 9, 0, 0),
                EndTime = new DateTime(2026, 7, 10, 9, 15, 0),
                RecurrenceRule = new WeeklyRecurrenceRule
                {
                    SeriesStartDate = new DateTime(2026, 7, 10),
                    DaysOfWeek = new List<DayOfWeek> { DayOfWeek.Friday }
                }
            };

            service.AddEvent(newEvent);

            Assert.Equal(newEvent.Id, newEvent.SeriesId);
        }

        [Fact]
        public void AddEvent_WithoutRecurrenceRule_LeavesSeriesIdNull()
        {
            var (service, _) = CreateService();
            var newEvent = new Event { Name = "Doctor" };

            service.AddEvent(newEvent);

            Assert.Null(newEvent.SeriesId);
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

        private static Event AddRecurringWeeklyEvent(EventService service)
        {
            var seriesEvent = new Event
            {
                Name = "Standup",
                StartTime = new DateTime(2026, 7, 6, 9, 0, 0),
                EndTime = new DateTime(2026, 7, 6, 9, 15, 0),
                RecurrenceRule = new WeeklyRecurrenceRule
                {
                    SeriesStartDate = new DateTime(2026, 7, 6),
                    DaysOfWeek = new List<DayOfWeek> { DayOfWeek.Monday }
                }
            };
            service.AddEvent(seriesEvent);
            return seriesEvent;
        }

        [Fact]
        public void EditOccurrence_ThisOccurrence_AddsOverride_AndLeavesRuleUnchanged()
        {
            var (service, _) = CreateService();
            var seriesEvent = AddRecurringWeeklyEvent(service);
            var occurrenceDate = new DateTime(2026, 7, 13);

            var result = service.EditOccurrence(seriesEvent.Id, occurrenceDate, "Standup (moved)",
                new DateTime(2026, 7, 13, 10, 0, 0), new DateTime(2026, 7, 13, 10, 15, 0), RecurrenceEditTarget.ThisOccurrence);

            Assert.True(result);
            var updated = service.GetEvent(seriesEvent.Id)!;
            Assert.Same(seriesEvent.RecurrenceRule, updated.RecurrenceRule);
            var over = Assert.Single(updated.OccurrenceOverrides);
            Assert.Equal(occurrenceDate, over.OriginalOccurrenceDate);
            Assert.Equal("Standup (moved)", over.Name);
        }

        [Fact]
        public void EditOccurrence_ThisAndFollowing_SplitsIntoNewSeries()
        {
            var (service, data) = CreateService();
            var seriesEvent = AddRecurringWeeklyEvent(service);
            var occurrenceDate = new DateTime(2026, 7, 13);

            var result = service.EditOccurrence(seriesEvent.Id, occurrenceDate, "Standup (renamed)",
                new DateTime(2026, 7, 13, 10, 0, 0), new DateTime(2026, 7, 13, 10, 15, 0), RecurrenceEditTarget.ThisAndFollowing);

            Assert.True(result);
            Assert.Equal(2, data.Events.Count);
            var newSeries = data.Events.Single(e => e.Id != seriesEvent.Id);
            Assert.Equal(newSeries.Id, newSeries.SeriesId);
            Assert.Equal("Standup (renamed)", newSeries.Name);
            Assert.IsType<UntilDateEndCondition>(seriesEvent.RecurrenceRule!.EndCondition);
        }

        [Fact]
        public void EditOccurrence_AllOccurrences_UpdatesBaseFields()
        {
            var (service, _) = CreateService();
            var seriesEvent = AddRecurringWeeklyEvent(service);

            var result = service.EditOccurrence(seriesEvent.Id, new DateTime(2026, 7, 13), "Standup (renamed)",
                new DateTime(2026, 7, 6, 10, 0, 0), new DateTime(2026, 7, 6, 10, 15, 0), RecurrenceEditTarget.AllOccurrences);

            Assert.True(result);
            var updated = service.GetEvent(seriesEvent.Id)!;
            Assert.Equal("Standup (renamed)", updated.Name);
            Assert.Equal(new DateTime(2026, 7, 6, 10, 0, 0), updated.StartTime);
        }

        [Fact]
        public void EditOccurrence_ReturnsFalse_WhenSeriesNotFoundOrNotRecurring()
        {
            var (service, _) = CreateService();
            service.AddEvent(new Event { Name = "Plain" });

            Assert.False(service.EditOccurrence(Guid.NewGuid(), DateTime.Today, "X", DateTime.Today, DateTime.Today.AddHours(1), RecurrenceEditTarget.ThisOccurrence));
        }

        [Fact]
        public void DeleteOccurrence_ThisOccurrence_AddsCancellationException()
        {
            var (service, _) = CreateService();
            var seriesEvent = AddRecurringWeeklyEvent(service);
            var occurrenceDate = new DateTime(2026, 7, 13);

            var result = service.DeleteOccurrence(seriesEvent.Id, occurrenceDate, RecurrenceEditTarget.ThisOccurrence);

            Assert.True(result);
            var updated = service.GetEvent(seriesEvent.Id)!;
            var exception = Assert.Single(updated.Exceptions);
            Assert.Equal(occurrenceDate, exception.OriginalOccurrenceDate);
            Assert.True(exception.IsCancelled);
        }

        [Fact]
        public void DeleteOccurrence_ThisAndFollowing_ClosesOffSeriesWithoutCreatingNewOne()
        {
            var (service, data) = CreateService();
            var seriesEvent = AddRecurringWeeklyEvent(service);

            var result = service.DeleteOccurrence(seriesEvent.Id, new DateTime(2026, 7, 13), RecurrenceEditTarget.ThisAndFollowing);

            Assert.True(result);
            Assert.Single(data.Events);
            Assert.IsType<UntilDateEndCondition>(seriesEvent.RecurrenceRule!.EndCondition);
        }

        [Fact]
        public void DeleteOccurrence_AllOccurrences_RemovesEntireSeries()
        {
            var (service, data) = CreateService();
            var seriesEvent = AddRecurringWeeklyEvent(service);

            var result = service.DeleteOccurrence(seriesEvent.Id, new DateTime(2026, 7, 13), RecurrenceEditTarget.AllOccurrences);

            Assert.True(result);
            Assert.Empty(data.Events);
        }

        [Fact]
        public void DeleteOccurrence_ReturnsFalse_WhenSeriesNotFoundOrNotRecurring()
        {
            var (service, _) = CreateService();
            service.AddEvent(new Event { Name = "Plain" });

            Assert.False(service.DeleteOccurrence(Guid.NewGuid(), DateTime.Today, RecurrenceEditTarget.ThisOccurrence));
        }

        private class SpySaveCountPersistenceService : IPersistenceService
        {
            public int SaveCount { get; private set; }

            public DataContainer LoadData() => new DataContainer();

            public void SaveData(DataContainer data) => SaveCount++;

            public void ArchiveTasks(IEnumerable<TaskItem> tasksToArchive) { }

            public List<TaskItem> GetArchivedTasks() => new List<TaskItem>();

            public bool RemoveArchivedTask(Guid taskId) => false;
        }
    }
}

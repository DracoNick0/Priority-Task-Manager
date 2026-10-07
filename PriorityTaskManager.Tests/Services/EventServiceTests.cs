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

        private static Event CreateValidEvent(string name) => new()
        {
            Name = name,
            StartTime = new DateTime(2026, 7, 10, 9, 0, 0),
            EndTime = new DateTime(2026, 7, 10, 10, 0, 0)
        };

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
        public void EventDescriptionAndLink_ArePersistedAndUpdated()
        {
            var (service, _) = CreateService();
            var evt = CreateValidEvent("Appointment");
            evt.Description = "Annual checkup";
            evt.Link = "https://example.com";
            service.AddEvent(evt);

            var update = new Event
            {
                Id = evt.Id,
                Name = evt.Name,
                Description = "Bring insurance card",
                Link = "https://example.org",
                StartTime = evt.StartTime,
                EndTime = evt.EndTime
            };

            Assert.True(service.UpdateEvent(update));
            Assert.Equal("Bring insurance card", service.GetEvent(evt.Id)!.Description);
            Assert.Equal("https://example.org", service.GetEvent(evt.Id)!.Link);
        }

        [Fact]
        public void AddEvent_RejectsEndTimeBeforeStartTime()
        {
            var (service, data) = CreateService();
            var invalidEvent = new Event
            {
                Name = "Invalid",
                StartTime = new DateTime(2026, 7, 10, 10, 0, 0),
                EndTime = new DateTime(2026, 7, 10, 9, 0, 0)
            };

            Assert.Throws<ArgumentException>(() => service.AddEvent(invalidEvent));
            Assert.Empty(data.Events);
            Assert.Equal(Guid.Empty, invalidEvent.Id);
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
            var newEvent = CreateValidEvent("Doctor");

            service.AddEvent(newEvent);

            Assert.Null(newEvent.SeriesId);
        }

        [Fact]
        public void GetEvent_ReturnsMatchingEvent_WhenPresent()
        {
            var (service, _) = CreateService();
            var newEvent = CreateValidEvent("Doctor");
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
        public void UpdateEvent_RejectsEndTimeBeforeStartTime_WithoutChangingEvent()
        {
            var (service, _) = CreateService();
            var original = new Event
            {
                Name = "Doctor",
                StartTime = new DateTime(2026, 7, 10, 9, 0, 0),
                EndTime = new DateTime(2026, 7, 10, 10, 0, 0)
            };
            service.AddEvent(original);

            var invalidUpdate = new Event
            {
                Id = original.Id,
                Name = "Invalid",
                StartTime = new DateTime(2026, 7, 11, 10, 0, 0),
                EndTime = new DateTime(2026, 7, 11, 9, 0, 0)
            };

            Assert.Throws<ArgumentException>(() => service.UpdateEvent(invalidUpdate));
            Assert.Equal("Doctor", service.GetEvent(original.Id)!.Name);
            Assert.Equal(new DateTime(2026, 7, 10, 9, 0, 0), original.StartTime);
        }

        [Fact]
        public void DeleteEvent_RemovesEvent_AndReturnsTrue()
        {
            var (service, data) = CreateService();
            var newEvent = CreateValidEvent("Doctor");
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
            service.AddEvent(CreateValidEvent("A"));
            service.AddEvent(CreateValidEvent("B"));

            service.ClearEvents();

            Assert.Empty(data.Events);
        }

        [Fact]
        public void GetAllEvents_ReturnsAllAddedEvents()
        {
            var (service, _) = CreateService();
            service.AddEvent(CreateValidEvent("A"));
            service.AddEvent(CreateValidEvent("B"));

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
                new DateTime(2026, 7, 13, 10, 0, 0), new DateTime(2026, 7, 13, 10, 15, 0),
                RecurrenceEditTarget.ThisOccurrence, description: "Updated agenda", link: "https://example.com");

            Assert.True(result);
            var updated = service.GetEvent(seriesEvent.Id)!;
            Assert.Same(seriesEvent.RecurrenceRule, updated.RecurrenceRule);
            var over = Assert.Single(updated.OccurrenceOverrides);
            Assert.Equal(occurrenceDate, over.OriginalOccurrenceDate);
            Assert.Equal("Standup (moved)", over.Name);
            Assert.Equal("Updated agenda", over.Description);
            Assert.Equal("https://example.com", over.Link);
            var occurrence = Assert.Single(service.GetEventOccurrences(occurrenceDate, occurrenceDate));
            Assert.Equal("Updated agenda", occurrence.Event.Description);
            Assert.Equal("https://example.com", occurrence.Event.Link);
        }

        [Fact]
        public void EditOccurrence_RejectsEndTimeBeforeStartTime()
        {
            var (service, _) = CreateService();
            var seriesEvent = AddRecurringWeeklyEvent(service);
            var occurrenceDate = new DateTime(2026, 7, 13);

            Assert.Throws<ArgumentException>(() => service.EditOccurrence(
                seriesEvent.Id,
                occurrenceDate,
                "Invalid",
                occurrenceDate.AddHours(10),
                occurrenceDate.AddHours(9),
                RecurrenceEditTarget.ThisOccurrence));
            Assert.Empty(seriesEvent.OccurrenceOverrides);
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
        public void EditOccurrence_ThisAndFollowing_PreservesFiniteOccurrenceCount()
        {
            var (service, data) = CreateService();
            var seriesEvent = AddRecurringWeeklyEvent(service);
            var rule = (WeeklyRecurrenceRule)seriesEvent.RecurrenceRule!;
            rule.DaysOfWeek.Add(DayOfWeek.Wednesday);
            rule.EndCondition = new AfterOccurrencesEndCondition { OccurrenceCount = 5 };

            Assert.True(service.EditOccurrence(seriesEvent.Id, new DateTime(2026, 7, 13), "Later",
                new DateTime(2026, 7, 13, 10, 0, 0), new DateTime(2026, 7, 13, 10, 15, 0), RecurrenceEditTarget.ThisAndFollowing));

            var expansion = new RecurrenceExpansionService();
            var newSeries = data.Events.Single(e => e.Id != seriesEvent.Id);
            Assert.Equal(new[] { new DateTime(2026, 7, 6), new DateTime(2026, 7, 8) },
                expansion.GetOccurrences(seriesEvent.RecurrenceRule!, seriesEvent.Exceptions, new DateTime(2026, 7, 6), new DateTime(2026, 8, 1)));
            Assert.Equal(new[] { new DateTime(2026, 7, 13), new DateTime(2026, 7, 15), new DateTime(2026, 7, 20) },
                expansion.GetOccurrences(newSeries.RecurrenceRule!, newSeries.Exceptions, new DateTime(2026, 7, 6), new DateTime(2026, 8, 1)));
        }

        [Fact]
        public void EditOccurrence_ThisOccurrence_ReplacesCancellationWithoutChangingOtherDates()
        {
            var (service, _) = CreateService();
            var series = AddRecurringWeeklyEvent(service);
            var date = new DateTime(2026, 7, 13);
            Assert.True(service.DeleteOccurrence(series.Id, date, RecurrenceEditTarget.ThisOccurrence));

            Assert.True(service.EditOccurrence(series.Id, date, "Restored", date.AddHours(11), date.AddHours(12), RecurrenceEditTarget.ThisOccurrence));

            Assert.Empty(series.Exceptions);
            Assert.Equal("Restored", Assert.Single(series.OccurrenceOverrides).Name);
            Assert.Equal(new[] { new DateTime(2026, 7, 6), date, new DateTime(2026, 7, 20) },
                new RecurrenceExpansionService().GetOccurrences(series.RecurrenceRule!, series.Exceptions, new DateTime(2026, 7, 6), new DateTime(2026, 7, 20)));
        }

        [Fact]
        public void GetEventOccurrences_MovedDateOutsideOriginalWindow_IsAddressableOnce()
        {
            var (service, _) = CreateService();
            var series = AddRecurringWeeklyEvent(service);
            var original = new DateTime(2026, 7, 13);
            var moved = new DateTime(2026, 7, 14, 10, 0, 0);
            service.EditOccurrence(series.Id, original, "Moved", moved, moved.AddMinutes(15), RecurrenceEditTarget.ThisOccurrence);

            var occurrence = Assert.Single(service.GetEventOccurrences(moved.Date, moved.Date));
            Assert.Equal(original, occurrence.OriginalOccurrenceDate);
            Assert.Equal(series.Id, occurrence.Event.SeriesId);
            Assert.Equal(moved, occurrence.Event.StartTime);
            Assert.DoesNotContain(service.GetEventOccurrences(original, original), o => o.Event.Name == "Moved");
        }

        [Fact]
        public void GetEventOccurrences_OvernightStartBeforeWindow_IncludesNextDayOverlap()
        {
            var (service, _) = CreateService();
            var series = AddRecurringWeeklyEvent(service);
            series.StartTime = new DateTime(2026, 7, 6, 23, 0, 0);
            series.EndTime = new DateTime(2026, 7, 7, 1, 0, 0);

            var occurrence = Assert.Single(service.GetEventOccurrences(new DateTime(2026, 7, 7), new DateTime(2026, 7, 7)));
            Assert.Equal(new DateTime(2026, 7, 6), occurrence.OriginalOccurrenceDate);
            Assert.Equal(new DateTime(2026, 7, 7, 1, 0, 0), occurrence.Event.EndTime);
        }

        [Fact]
        public void EditOccurrence_MiddleSplitMovedDate_KeepsLaterRecurrencesOnOriginalDates()
        {
            var (service, data) = CreateService();
            var series = AddRecurringWeeklyEvent(service);
            var moved = new DateTime(2026, 7, 14, 10, 0, 0);

            service.EditOccurrence(series.Id, new DateTime(2026, 7, 13), "Later", moved, moved.AddMinutes(15), RecurrenceEditTarget.ThisAndFollowing);

            var next = data.Events.Single(e => e.Id != series.Id);
            Assert.Equal(new DateTime(2026, 7, 13), next.RecurrenceRule!.SeriesStartDate.Date);
            Assert.Equal(new DateTime(2026, 7, 13), Assert.Single(next.OccurrenceOverrides).OriginalOccurrenceDate);
            Assert.Equal(new[] { new DateTime(2026, 7, 14), new DateTime(2026, 7, 20) },
                service.GetEventOccurrences(new DateTime(2026, 7, 14), new DateTime(2026, 7, 20))
                    .Select(o => o.Event.StartTime.Date).ToArray());
        }

        [Fact]
        public void EditOccurrence_ThisAndFollowing_ReplacesSelectedOverrideAndKeepsEarlierExceptions()
        {
            var (service, data) = CreateService();
            var series = AddRecurringWeeklyEvent(service);
            var selected = new DateTime(2026, 7, 20);
            service.DeleteOccurrence(series.Id, new DateTime(2026, 7, 6), RecurrenceEditTarget.ThisOccurrence);
            service.EditOccurrence(series.Id, selected, "Old edit", selected.AddHours(12), selected.AddHours(13), RecurrenceEditTarget.ThisOccurrence);
            service.EditOccurrence(series.Id, new DateTime(2026, 7, 27), "Later edit",
                new DateTime(2026, 7, 27, 12, 0, 0), new DateTime(2026, 7, 27, 13, 0, 0), RecurrenceEditTarget.ThisOccurrence);

            Assert.True(service.EditOccurrence(series.Id, selected, "New edit", selected.AddHours(10), selected.AddHours(11), RecurrenceEditTarget.ThisAndFollowing));

            var next = data.Events.Single(e => e.Id != series.Id);
            Assert.Single(series.Exceptions);
            Assert.Empty(series.OccurrenceOverrides);
            Assert.Equal("Later edit", Assert.Single(next.OccurrenceOverrides).Name);
            Assert.Equal(selected.AddHours(10), next.StartTime);
            Assert.Equal(new DateTime(2026, 7, 13), Assert.Single(new RecurrenceExpansionService().GetOccurrences(
                series.RecurrenceRule!, series.Exceptions, new DateTime(2026, 7, 6), selected)));
        }

        [Fact]
        public void EditOccurrence_FirstActualOccurrence_DoesNotLeaveEmptyPriorSeries()
        {
            var (service, data) = CreateService();
            var series = AddRecurringWeeklyEvent(service);
            series.RecurrenceRule!.SeriesStartDate = new DateTime(2026, 7, 5);

            Assert.True(service.EditOccurrence(series.Id, new DateTime(2026, 7, 6), "New", new DateTime(2026, 7, 6, 10, 0, 0),
                new DateTime(2026, 7, 6, 11, 0, 0), RecurrenceEditTarget.ThisAndFollowing));

            Assert.Single(data.Events);
            Assert.Equal(series.Id, series.SeriesId);
            Assert.Equal("New", series.Name);
        }

        [Fact]
        public void DeleteOccurrence_FirstActualOccurrence_RemovesSeries()
        {
            var (service, data) = CreateService();
            var series = AddRecurringWeeklyEvent(service);
            series.RecurrenceRule!.SeriesStartDate = new DateTime(2026, 7, 5);

            Assert.True(service.DeleteOccurrence(series.Id, new DateTime(2026, 7, 6), RecurrenceEditTarget.ThisAndFollowing));
            Assert.Empty(data.Events);
        }

        [Fact]
        public void DeleteOccurrence_LastFiniteOccurrence_PreservesOnlyEarlierDates()
        {
            var (service, data) = CreateService();
            var series = AddRecurringWeeklyEvent(service);
            series.RecurrenceRule!.EndCondition = new AfterOccurrencesEndCondition { OccurrenceCount = 3 };

            Assert.True(service.DeleteOccurrence(series.Id, new DateTime(2026, 7, 20), RecurrenceEditTarget.ThisAndFollowing));
            Assert.Single(data.Events);
            Assert.Equal(new[] { new DateTime(2026, 7, 6), new DateTime(2026, 7, 13) },
                new RecurrenceExpansionService().GetOccurrences(series.RecurrenceRule!, series.Exceptions, new DateTime(2026, 7, 6), new DateTime(2026, 8, 10)));
        }

        [Fact]
        public void DeleteOccurrence_AfterExistingExceptions_KeepsEarlierEditsAndCancellations()
        {
            var (service, data) = CreateService();
            var series = AddRecurringWeeklyEvent(service);
            var first = new DateTime(2026, 7, 6);
            var second = new DateTime(2026, 7, 13);
            service.DeleteOccurrence(series.Id, first, RecurrenceEditTarget.ThisOccurrence);
            service.EditOccurrence(series.Id, second, "Earlier edit", second.AddHours(10), second.AddHours(11), RecurrenceEditTarget.ThisOccurrence);
            service.EditOccurrence(series.Id, new DateTime(2026, 7, 20), "Later edit",
                new DateTime(2026, 7, 20, 10, 0, 0), new DateTime(2026, 7, 20, 11, 0, 0), RecurrenceEditTarget.ThisOccurrence);

            Assert.True(service.DeleteOccurrence(series.Id, new DateTime(2026, 7, 20), RecurrenceEditTarget.ThisAndFollowing));

            Assert.Single(data.Events);
            Assert.Equal(first, Assert.Single(series.Exceptions).OriginalOccurrenceDate);
            Assert.Equal(second, Assert.Single(series.OccurrenceOverrides).OriginalOccurrenceDate);
            Assert.Equal(second, Assert.Single(new RecurrenceExpansionService().GetOccurrences(series.RecurrenceRule!, series.Exceptions, first, new DateTime(2026, 8, 1))));
        }

        [Fact]
        public void EditOccurrence_SplitSeries_RoundTripsRuleAndOriginalDateThroughJson()
        {
            var (service, data) = CreateService();
            var series = AddRecurringWeeklyEvent(service);
            series.RecurrenceRule!.EndCondition = new AfterOccurrencesEndCondition { OccurrenceCount = 3 };
            var selected = new DateTime(2026, 7, 13);
            service.EditOccurrence(series.Id, selected, "Moved", new DateTime(2026, 7, 14, 10, 0, 0),
                new DateTime(2026, 7, 14, 11, 0, 0), RecurrenceEditTarget.ThisAndFollowing);

            var json = System.Text.Json.JsonSerializer.Serialize(data.Events);
            var restored = System.Text.Json.JsonSerializer.Deserialize<List<Event>>(json)!;
            var next = restored.Single(e => e.Id != series.Id);

            Assert.Equal(next.Id, next.SeriesId);
            Assert.Equal(2, Assert.IsType<AfterOccurrencesEndCondition>(next.RecurrenceRule!.EndCondition).OccurrenceCount);
            Assert.Equal(selected, Assert.Single(next.OccurrenceOverrides).OriginalOccurrenceDate);
            Assert.Equal(new DateTime(2026, 7, 14, 10, 0, 0), Assert.Single(next.OccurrenceOverrides).StartTime);
        }

        [Fact]
        public void EditOccurrence_ChangedFutureRule_DoesNotChangeEarlierPattern()
        {
            var (service, data) = CreateService();
            var series = AddRecurringWeeklyEvent(service);
            var replacement = new WeeklyRecurrenceRule
            {
                SeriesStartDate = new DateTime(2026, 7, 6),
                DaysOfWeek = new List<DayOfWeek> { DayOfWeek.Monday, DayOfWeek.Wednesday },
                EndCondition = new UntilDateEndCondition { UntilDate = new DateTime(2026, 7, 22) }
            };

            Assert.True(service.EditOccurrence(series.Id, new DateTime(2026, 7, 13), "New",
                new DateTime(2026, 7, 13, 10, 0, 0), new DateTime(2026, 7, 13, 11, 0, 0),
                RecurrenceEditTarget.ThisAndFollowing, replacement));

            var next = data.Events.Single(e => e.Id != series.Id);
            Assert.Equal(new DateTime(2026, 7, 6), replacement.SeriesStartDate);
            Assert.Equal(new DateTime(2026, 7, 13), next.RecurrenceRule!.SeriesStartDate);
            Assert.Equal(new[] { new DateTime(2026, 7, 6) },
                new RecurrenceExpansionService().GetOccurrences(series.RecurrenceRule!, series.Exceptions, new DateTime(2026, 7, 6), new DateTime(2026, 7, 22)));
            Assert.Equal(new[] { new DateTime(2026, 7, 13), new DateTime(2026, 7, 15), new DateTime(2026, 7, 20), new DateTime(2026, 7, 22) },
                new RecurrenceExpansionService().GetOccurrences(next.RecurrenceRule!, next.Exceptions, new DateTime(2026, 7, 6), new DateTime(2026, 7, 22)));
        }

        [Fact]
        public void DeleteOccurrence_RejectsNonOccurrenceDateWithoutPersisting()
        {
            var (service, data) = CreateService();
            var series = AddRecurringWeeklyEvent(service);
            series.RecurrenceRule!.EndCondition = new AfterOccurrencesEndCondition { OccurrenceCount = 2 };

            Assert.False(service.DeleteOccurrence(series.Id, new DateTime(2026, 7, 14), RecurrenceEditTarget.ThisOccurrence));
            Assert.False(service.EditOccurrence(series.Id, new DateTime(2026, 7, 20), "Extra", new DateTime(2026, 7, 20, 10, 0, 0),
                new DateTime(2026, 7, 20, 11, 0, 0), RecurrenceEditTarget.ThisAndFollowing));
            Assert.Single(data.Events);
            Assert.Empty(series.Exceptions);
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
            service.AddEvent(CreateValidEvent("Plain"));

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
            service.AddEvent(CreateValidEvent("Plain"));

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

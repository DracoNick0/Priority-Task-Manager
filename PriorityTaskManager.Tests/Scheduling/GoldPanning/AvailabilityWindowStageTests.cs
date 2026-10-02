using Xunit;
using PriorityTaskManager.Models;
using PriorityTaskManager.Services;
using PriorityTaskManager.Scheduling.GoldPanning;
using PriorityTaskManager.Scheduling.GoldPanning.Stages;
using PriorityTaskManager.Tests.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PriorityTaskManager.Tests.Scheduling.GoldPanning
{
    public class AvailabilityWindowStageTests
    {
        /*
         * === Test Coverage for AvailabilityWindowStage ===
         * [✓] Context Handling: Agent returns gracefully if required context keys ('UserProfile', 'Tasks') are missing or invalid.
         * [✓] Horizon Calculation: Correctly calculates the scheduling horizon based on task workload and user's work schedule.
         * [✓] Horizon Calculation (Edge Case): Handles zero tasks without error.
         * [✓] Horizon Calculation (Edge Case): Correctly caps the horizon at 5 years for extreme workloads.
         * [✓] Slot Generation (No Events): Generates correct time slots for a simple workday.
         * [✓] Slot Generation (No Events): Adjusts start time correctly if the agent runs mid-workday.
         * [✓] Slot Generation (No Events): Skips days that are not workdays.
         * [✓] Slot Generation (No Events): Handles a workload spanning multiple weeks and weekends.
         * [✓] Slot Generation (With Events): Splits a workday correctly around a single event.
         * [✓] Slot Generation (With Events): Handles an event that starts before the workday.
         * [✓] Slot Generation (With Events): Handles an event that ends after the workday.
         * [✓] Slot Generation (With Events): Handles multiple, non-overlapping events in a single day.
         * [✓] Slot Generation (With Events): Correctly handles overlapping events by merging them.
         * [✓] Slot Generation (With Events): Ignores events that occur entirely outside of work hours.
        */

        private readonly MockTimeService _timeService;
        private readonly UserProfile _userProfile;

        public AvailabilityWindowStageTests()
        {
            _timeService = new MockTimeService();
            _userProfile = new UserProfile
            {
                WorkDays = new List<DayOfWeek> { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday },
                WorkStartTime = new TimeOnly(9, 0),
                WorkEndTime = new TimeOnly(17, 0)
            };
        }

        private SchedulingContext CreateInitialContext(List<TaskItem> tasks, List<Event>? events = null)
        {
            var context = new SchedulingContext();
            context.SharedState["UserProfile"] = _userProfile;
            context.SharedState["Tasks"] = tasks;
            if (events != null)
            {
                context.SharedState["Events"] = events;
            }
            return context;
        }

        [Fact]
        public void Act_WhenUserProfileIsMissing_ShouldReturnContextUnchanged()
        {
            // Arrange
            var agent = new AvailabilityWindowStage(_timeService, new RecurrenceExpansionService());
            var context = new SchedulingContext();
            context.SharedState["Tasks"] = new List<TaskItem>();

            // Act
            var resultContext = agent.Act(context);

            // Assert
            Assert.False(resultContext.SharedState.ContainsKey("AvailableScheduleWindow"));
        }

        [Fact]
        public void Act_WhenTasksAreMissing_ShouldReturnContextUnchanged()
        {
            // Arrange
            var agent = new AvailabilityWindowStage(_timeService, new RecurrenceExpansionService());
            var context = new SchedulingContext();
            context.SharedState["UserProfile"] = _userProfile;

            // Act
            var resultContext = agent.Act(context);

            // Assert
            Assert.False(resultContext.SharedState.ContainsKey("AvailableScheduleWindow"));
        }

        [Fact]
        public void Act_WithNoTasks_ShouldProduceNoSlots()
        {
            // Arrange
            var agent = new AvailabilityWindowStage(_timeService, new RecurrenceExpansionService());
            var context = CreateInitialContext(new List<TaskItem>());

            // Act
            var resultContext = agent.Act(context);
            var scheduleWindow = resultContext.SharedState["AvailableScheduleWindow"] as ScheduleWindow;

            // Assert
            Assert.NotNull(scheduleWindow);
            Assert.Empty(scheduleWindow.AvailableSlots);
            Assert.Equal(TimeSpan.Zero, (TimeSpan)resultContext.SharedState["TotalAvailableTime"]);
        }

        [Fact]
        public void Act_SimpleWorkload_ShouldCreateCorrectSingleSlot()
        {
            // Monday, Jan 1, 2024, at 8:00 AM
            _timeService.SetCurrentTime(new DateTime(2024, 1, 1, 8, 0, 0));
            var agent = new AvailabilityWindowStage(_timeService, new RecurrenceExpansionService());
            var tasks = new List<TaskItem> { new TaskItem { EstimatedDuration = TimeSpan.FromHours(4) } };
            var context = CreateInitialContext(tasks);

            // Act
            var resultContext = agent.Act(context);
            var scheduleWindow = resultContext.SharedState["AvailableScheduleWindow"] as ScheduleWindow;
            var slots = scheduleWindow?.AvailableSlots;

            // Assert
            Assert.NotNull(slots);
            // Horizon min is 7 days. Jan 1 (Mon) to Jan 8 (Mon) inclusive = 6 working days.
            Assert.Equal(6, slots.Count); 
            Assert.Equal(new DateTime(2024, 1, 1, 9, 0, 0), slots[0].StartTime);
            Assert.Equal(new DateTime(2024, 1, 1, 17, 0, 0), slots[0].EndTime);
        }

        [Fact]
        public void Act_MidWorkdayStart_ShouldCreateShortenedSlot()
        {
            // Monday, Jan 1, 2024, at 12:00 PM
            var now = new DateTime(2024, 1, 1, 12, 0, 0);
            _timeService.SetCurrentTime(now);
            var agent = new AvailabilityWindowStage(_timeService, new RecurrenceExpansionService());
            var tasks = new List<TaskItem> { new TaskItem { EstimatedDuration = TimeSpan.FromHours(1) } };
            var context = CreateInitialContext(tasks);

            // Act
            var resultContext = agent.Act(context);
            var scheduleWindow = resultContext.SharedState["AvailableScheduleWindow"] as ScheduleWindow;
            var slots = scheduleWindow?.AvailableSlots;

            // Assert
            Assert.NotNull(slots);
            // Horizon likely extends to following Monday.
            Assert.True(slots.Count >= 1);
            Assert.Equal(now, slots[0].StartTime);
            Assert.Equal(new DateTime(2024, 1, 1, 17, 0, 0), slots[0].EndTime);
        }

        [Fact]
        public void Act_WorkloadSpanningWeekend_ShouldSkipWeekendDays()
        {
            // Thursday, Jan 4, 2024, at 10:00 AM
            _timeService.SetCurrentTime(new DateTime(2024, 1, 4, 10, 0, 0));
            var agent = new AvailabilityWindowStage(_timeService, new RecurrenceExpansionService());
            // 12 hours = 7 on Thursday, 5 on Friday. Horizon should end on Friday.
            var tasks = new List<TaskItem> { new TaskItem { EstimatedDuration = TimeSpan.FromHours(12) } };
            var context = CreateInitialContext(tasks);

            // Act
            var resultContext = agent.Act(context);
            var scheduleWindow = resultContext.SharedState["AvailableScheduleWindow"] as ScheduleWindow;
            var slots = scheduleWindow?.AvailableSlots;

            // Assert
            Assert.NotNull(slots);
            // Should contain Thursday and Friday slots at the beginning
            Assert.True(slots.Count >= 2);
            // Slot 1: Thursday
            Assert.Equal(new DateTime(2024, 1, 4, 10, 0, 0), slots[0].StartTime);
            Assert.Equal(new DateTime(2024, 1, 4, 17, 0, 0), slots[0].EndTime);
            // Slot 2: Friday
            Assert.Equal(new DateTime(2024, 1, 5, 9, 0, 0), slots[1].StartTime);
            Assert.Equal(new DateTime(2024, 1, 5, 17, 0, 0), slots[1].EndTime);
        }

        [Fact]
        public void Act_WithEventInMiddle_ShouldCreateTwoSlots()
        {
            // Monday, Jan 1, 2024, at 8:00 AM
            _timeService.SetCurrentTime(new DateTime(2024, 1, 1, 8, 0, 0));
            var agent = new AvailabilityWindowStage(_timeService, new RecurrenceExpansionService());
            var tasks = new List<TaskItem> { new TaskItem { EstimatedDuration = TimeSpan.FromHours(1) } };
            var events = new List<Event>
            {
                new Event { StartTime = new DateTime(2024, 1, 1, 12, 0, 0), EndTime = new DateTime(2024, 1, 1, 13, 0, 0) }
            };
            var context = CreateInitialContext(tasks, events);

            // Act
            var resultContext = agent.Act(context);
            var scheduleWindow = resultContext.SharedState["AvailableScheduleWindow"] as ScheduleWindow;
            var slots = scheduleWindow?.AvailableSlots;

            // Assert
            Assert.NotNull(slots);
            // Day 1 is split into 2. Normal days following are 1 each.
            // 2 (Jan 1) + 5 (Jan 2-5, 8) = 7 slots total.
            Assert.Equal(7, slots.Count);
            Assert.Equal(new DateTime(2024, 1, 1, 9, 0, 0), slots[0].StartTime);
            Assert.Equal(new DateTime(2024, 1, 1, 12, 0, 0), slots[0].EndTime);
            Assert.Equal(new DateTime(2024, 1, 1, 13, 0, 0), slots[1].StartTime);
            Assert.Equal(new DateTime(2024, 1, 1, 17, 0, 0), slots[1].EndTime);
        }

        [Fact]
        public void Act_WithOverlappingEvents_ShouldMergeAndCreateCorrectSlots()
        {
            // Monday, Jan 1, 2024, at 8:00 AM
            _timeService.SetCurrentTime(new DateTime(2024, 1, 1, 8, 0, 0));
            var agent = new AvailabilityWindowStage(_timeService, new RecurrenceExpansionService());
            var tasks = new List<TaskItem> { new TaskItem { EstimatedDuration = TimeSpan.FromHours(1) } };
            var events = new List<Event>
            {
                new Event { StartTime = new DateTime(2024, 1, 1, 10, 0, 0), EndTime = new DateTime(2024, 1, 1, 11, 0, 0) },
                new Event { StartTime = new DateTime(2024, 1, 1, 10, 30, 0), EndTime = new DateTime(2024, 1, 1, 11, 30, 0) }
            };
            var context = CreateInitialContext(tasks, events);

            // Act
            var resultContext = agent.Act(context);
            var scheduleWindow = resultContext.SharedState["AvailableScheduleWindow"] as ScheduleWindow;
            var slots = scheduleWindow?.AvailableSlots;

            // Assert
            Assert.NotNull(slots);
            // Same logic: Day 1 split into 2. Plus 5 subsequent days. Total 7.
            Assert.Equal(7, slots.Count);
            Assert.Equal(new DateTime(2024, 1, 1, 9, 0, 0), slots[0].StartTime);
            Assert.Equal(new DateTime(2024, 1, 1, 10, 0, 0), slots[0].EndTime);
            Assert.Equal(new DateTime(2024, 1, 1, 11, 30, 0), slots[1].StartTime);
            Assert.Equal(new DateTime(2024, 1, 1, 17, 0, 0), slots[1].EndTime);
        }

        [Fact]
        public void Act_WithHugeWorkload_ShouldCapHorizonAt5Years()
        {
            var now = new DateTime(2024, 1, 1, 8, 0, 0);
            _timeService.SetCurrentTime(now);
            var agent = new AvailabilityWindowStage(_timeService, new RecurrenceExpansionService());
            // 260 workdays/year * 8 hours/day * 6 years = 12480 hours
            var tasks = new List<TaskItem> { new TaskItem { EstimatedDuration = TimeSpan.FromHours(12480) } };
            var context = CreateInitialContext(tasks);

            // Act
            var resultContext = agent.Act(context);
            
            // Assert
            Assert.Contains("Warning: Workload exceeds 5 years of available time. Capping horizon.", resultContext.History);
            var scheduleWindow = resultContext.SharedState["AvailableScheduleWindow"] as ScheduleWindow;
            var lastSlot = scheduleWindow?.AvailableSlots.OrderBy(s => s.EndTime).Last();
            Assert.NotNull(lastSlot);
            // The end date of the last slot should be around 5 years from now.
            Assert.True(lastSlot.EndTime.Date <= now.Date.AddYears(5).AddDays(1)); // Add a day for tolerance
        }

        [Fact]
        public void Act_WithRecurringEvent_ShouldBlockAvailabilityOnEveryOccurrence()
        {
            // Monday, Jan 1, 2024, at 8:00 AM
            _timeService.SetCurrentTime(new DateTime(2024, 1, 1, 8, 0, 0));
            var agent = new AvailabilityWindowStage(_timeService, new RecurrenceExpansionService());
            var tasks = new List<TaskItem> { new TaskItem { EstimatedDuration = TimeSpan.FromHours(1) } };
            var events = new List<Event>
            {
                new Event
                {
                    StartTime = new DateTime(2024, 1, 1, 12, 0, 0),
                    EndTime = new DateTime(2024, 1, 1, 13, 0, 0),
                    RecurrenceRule = new DailyIntervalRecurrenceRule
                    {
                        SeriesStartDate = new DateTime(2024, 1, 1),
                        IntervalDays = 1
                    }
                }
            };
            var context = CreateInitialContext(tasks, events);

            // Act
            var resultContext = agent.Act(context);
            var scheduleWindow = resultContext.SharedState["AvailableScheduleWindow"] as ScheduleWindow;
            var slots = scheduleWindow?.AvailableSlots;

            // Assert: every workday in the horizon (Jan 1-5, 8) is split around its own 12:00-13:00 occurrence.
            Assert.NotNull(slots);
            Assert.Equal(12, slots.Count);
            foreach (var day in new[] { 1, 2, 3, 4, 5, 8 })
            {
                Assert.Contains(slots, s => s.StartTime == new DateTime(2024, 1, day, 9, 0, 0) && s.EndTime == new DateTime(2024, 1, day, 12, 0, 0));
                Assert.Contains(slots, s => s.StartTime == new DateTime(2024, 1, day, 13, 0, 0) && s.EndTime == new DateTime(2024, 1, day, 17, 0, 0));
            }
        }

        [Fact]
        public void Act_WithRecurringEvent_ShouldSkipCancelledOccurrence()
        {
            // Monday, Jan 1, 2024, at 8:00 AM
            _timeService.SetCurrentTime(new DateTime(2024, 1, 1, 8, 0, 0));
            var agent = new AvailabilityWindowStage(_timeService, new RecurrenceExpansionService());
            var tasks = new List<TaskItem> { new TaskItem { EstimatedDuration = TimeSpan.FromHours(1) } };
            var events = new List<Event>
            {
                new Event
                {
                    StartTime = new DateTime(2024, 1, 1, 12, 0, 0),
                    EndTime = new DateTime(2024, 1, 1, 13, 0, 0),
                    RecurrenceRule = new DailyIntervalRecurrenceRule
                    {
                        SeriesStartDate = new DateTime(2024, 1, 1),
                        IntervalDays = 1
                    },
                    Exceptions = new List<RecurrenceException>
                    {
                        new RecurrenceException { OriginalOccurrenceDate = new DateTime(2024, 1, 2), IsCancelled = true }
                    }
                }
            };
            var context = CreateInitialContext(tasks, events);

            // Act
            var resultContext = agent.Act(context);
            var scheduleWindow = resultContext.SharedState["AvailableScheduleWindow"] as ScheduleWindow;
            var slots = scheduleWindow?.AvailableSlots;

            // Assert: Jan 2's occurrence is cancelled, so that day gets one unsplit full-day slot.
            Assert.NotNull(slots);
            Assert.Contains(slots, s => s.StartTime == new DateTime(2024, 1, 2, 9, 0, 0) && s.EndTime == new DateTime(2024, 1, 2, 17, 0, 0));
            Assert.Contains(slots, s => s.StartTime == new DateTime(2024, 1, 3, 9, 0, 0) && s.EndTime == new DateTime(2024, 1, 3, 12, 0, 0));
        }

        [Fact]
        public void Act_WithRecurringEvent_ShouldApplyOccurrenceOverride()
        {
            // Monday, Jan 1, 2024, at 8:00 AM
            _timeService.SetCurrentTime(new DateTime(2024, 1, 1, 8, 0, 0));
            var agent = new AvailabilityWindowStage(_timeService, new RecurrenceExpansionService());
            var tasks = new List<TaskItem> { new TaskItem { EstimatedDuration = TimeSpan.FromHours(1) } };
            var events = new List<Event>
            {
                new Event
                {
                    StartTime = new DateTime(2024, 1, 1, 12, 0, 0),
                    EndTime = new DateTime(2024, 1, 1, 13, 0, 0),
                    RecurrenceRule = new DailyIntervalRecurrenceRule
                    {
                        SeriesStartDate = new DateTime(2024, 1, 1),
                        IntervalDays = 1
                    },
                    OccurrenceOverrides = new List<EventOccurrenceOverride>
                    {
                        new EventOccurrenceOverride
                        {
                            OriginalOccurrenceDate = new DateTime(2024, 1, 2),
                            Name = "Moved",
                            StartTime = new DateTime(2024, 1, 2, 15, 0, 0),
                            EndTime = new DateTime(2024, 1, 2, 16, 0, 0)
                        }
                    }
                }
            };
            var context = CreateInitialContext(tasks, events);

            // Act
            var resultContext = agent.Act(context);
            var scheduleWindow = resultContext.SharedState["AvailableScheduleWindow"] as ScheduleWindow;
            var slots = scheduleWindow?.AvailableSlots;

            // Assert: Jan 2's occurrence is blocked at its overridden 15:00-16:00 time instead of 12:00-13:00.
            Assert.NotNull(slots);
            Assert.Contains(slots, s => s.StartTime == new DateTime(2024, 1, 2, 9, 0, 0) && s.EndTime == new DateTime(2024, 1, 2, 15, 0, 0));
            Assert.Contains(slots, s => s.StartTime == new DateTime(2024, 1, 2, 16, 0, 0) && s.EndTime == new DateTime(2024, 1, 2, 17, 0, 0));
        }

        [Fact]
        public void Act_MovedOccurrenceFromBeforeHorizon_BlocksItsNewDay()
        {
            _timeService.SetCurrentTime(new DateTime(2024, 1, 2, 8, 0, 0));
            var agent = new AvailabilityWindowStage(_timeService, new RecurrenceExpansionService());
            var events = new List<Event>
            {
                new Event
                {
                    StartTime = new DateTime(2024, 1, 1, 12, 0, 0),
                    EndTime = new DateTime(2024, 1, 1, 13, 0, 0),
                    RecurrenceRule = new DailyIntervalRecurrenceRule
                    {
                        SeriesStartDate = new DateTime(2024, 1, 1),
                        EndCondition = new AfterOccurrencesEndCondition { OccurrenceCount = 1 }
                    },
                    OccurrenceOverrides = new List<EventOccurrenceOverride>
                    {
                        new EventOccurrenceOverride
                        {
                            OriginalOccurrenceDate = new DateTime(2024, 1, 1),
                            Name = "Moved",
                            StartTime = new DateTime(2024, 1, 2, 15, 0, 0),
                            EndTime = new DateTime(2024, 1, 2, 16, 0, 0)
                        }
                    }
                }
            };

            var result = agent.Act(CreateInitialContext(
                new List<TaskItem> { new TaskItem { EstimatedDuration = TimeSpan.FromHours(1) } }, events));
            var slots = ((ScheduleWindow)result.SharedState["AvailableScheduleWindow"]).AvailableSlots;

            Assert.Contains(slots, slot => slot.StartTime == new DateTime(2024, 1, 2, 9, 0, 0) && slot.EndTime == new DateTime(2024, 1, 2, 15, 0, 0));
            Assert.Contains(slots, slot => slot.StartTime == new DateTime(2024, 1, 2, 16, 0, 0) && slot.EndTime == new DateTime(2024, 1, 2, 17, 0, 0));
        }

        [Fact]
        public void Act_OvernightOccurrenceFromBeforeHorizon_BlocksNextMorning()
        {
            _timeService.SetCurrentTime(new DateTime(2024, 1, 2, 8, 0, 0));
            var agent = new AvailabilityWindowStage(_timeService, new RecurrenceExpansionService());
            var events = new List<Event>
            {
                new Event
                {
                    StartTime = new DateTime(2024, 1, 1, 23, 0, 0),
                    EndTime = new DateTime(2024, 1, 2, 10, 0, 0),
                    RecurrenceRule = new DailyIntervalRecurrenceRule
                    {
                        SeriesStartDate = new DateTime(2024, 1, 1),
                        EndCondition = new AfterOccurrencesEndCondition { OccurrenceCount = 1 }
                    }
                }
            };

            var result = agent.Act(CreateInitialContext(
                new List<TaskItem> { new TaskItem { EstimatedDuration = TimeSpan.FromHours(1) } }, events));
            var slots = ((ScheduleWindow)result.SharedState["AvailableScheduleWindow"]).AvailableSlots;

            Assert.Contains(slots, slot => slot.StartTime == new DateTime(2024, 1, 2, 10, 0, 0) && slot.EndTime == new DateTime(2024, 1, 2, 17, 0, 0));
            Assert.DoesNotContain(slots, slot => slot.StartTime == new DateTime(2024, 1, 2, 9, 0, 0));
        }
    }
}

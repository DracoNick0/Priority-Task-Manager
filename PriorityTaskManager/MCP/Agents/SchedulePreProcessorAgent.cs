using PriorityTaskManager.MCP;
using PriorityTaskManager.Models;
using PriorityTaskManager.Services;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PriorityTaskManager.MCP.Agents
{
    /// <summary>
    /// Agent responsible for pre-processing tasks before scheduling.
    /// It calculates the available time slots for work based on the user's profile, tasks, and existing events.
    /// </summary>
    public class SchedulePreProcessorAgent : IAgent
    {
        private readonly ITimeService _timeService;

        public SchedulePreProcessorAgent(ITimeService timeService)
        {
            _timeService = timeService;
        }

        /// <inheritdoc />
        public MCPContext Act(MCPContext context)
        {
            context.History.Add("Phase 1: Analyzing user's schedule constraints...");

            if (!context.SharedState.TryGetValue("UserProfile", out var userProfileObj) || userProfileObj is not UserProfile userProfile)
            {
                context.History.Add("Error: UserProfile not found in context.");
                return context;
            }
            
            if (!context.SharedState.TryGetValue("Tasks", out var tasksObj) || tasksObj is not List<TaskItem> tasks)
            {
                context.History.Add("Error: Task list not found in context.");
                return context;
            }

            var now = _timeService.GetCurrentTime();
            var totalWorkloadDuration = TimeSpan.FromTicks(tasks.Sum(t => t.EstimatedDuration.Ticks));

            // If there's no work to be done, there's no need to calculate slots.
            if (totalWorkloadDuration <= TimeSpan.Zero)
            {
                context.History.Add("SchedulePreProcessorAgent: No tasks to schedule, no time slots generated.");
                context.SharedState["AvailableScheduleWindow"] = new ScheduleWindow { AvailableSlots = new List<TimeSlot>() };
                context.SharedState["TotalAvailableTime"] = TimeSpan.Zero;
                return context;
            }

            // Step 1: Calculate the Core Horizon End Date
            var coreHorizonEndDate = CalculateHorizon(now, totalWorkloadDuration, userProfile, context.History);

            // Step 2: Generate available time slots within the horizon
            var slots = GenerateAvailableSlots(now, coreHorizonEndDate, userProfile, context);

            context.History.Add("  -> Calculated Available Time Slots:");
            foreach (var slot in slots.OrderBy(s => s.StartTime))
            {
                context.History.Add($"    - Slot: {slot.StartTime} to {slot.EndTime} (Duration: {slot.Duration.TotalHours:F2}h)");
            }

            var scheduleWindow = new ScheduleWindow { AvailableSlots = slots };
            context.SharedState["AvailableScheduleWindow"] = scheduleWindow;

            var totalAvailableTime = slots.Sum(slot => slot.Duration.Ticks);
            context.SharedState["TotalAvailableTime"] = TimeSpan.FromTicks(totalAvailableTime);

            return context;
        }

        private DateTime CalculateHorizon(DateTime now, TimeSpan totalWorkloadDuration, UserProfile userProfile, List<string> history)
        {
            var accumulatedAvailableTime = TimeSpan.Zero;
            var horizonDate = now.Date;
            var dailyWorkDuration = userProfile.WorkEndTime - userProfile.WorkStartTime;

            while (accumulatedAvailableTime < totalWorkloadDuration)
            {
                if (userProfile.WorkDays.Contains(horizonDate.DayOfWeek))
                {
                    accumulatedAvailableTime += dailyWorkDuration;
                }

                if (horizonDate > now.Date.AddYears(5)) 
                {
                    history.Add("Warning: Workload exceeds 5 years of available time. Capping horizon.");
                    break;
                }
                
                if (accumulatedAvailableTime < totalWorkloadDuration)
                {
                    horizonDate = horizonDate.AddDays(1);
                }
            }
            return horizonDate;
        }

        private List<TimeSlot> GenerateAvailableSlots(DateTime now, DateTime coreHorizonEndDate, UserProfile userProfile, MCPContext context)
        {
            var slots = new List<TimeSlot>();
            var events = (context.SharedState.TryGetValue("Events", out var eventsObj) && eventsObj is List<Event> ev) ? ev : new List<Event>();
            var sortedEvents = events.OrderBy(e => e.StartTime).ToList();

            for (var day = now.Date; day <= coreHorizonEndDate; day = day.AddDays(1))
            {
                if (!userProfile.WorkDays.Contains(day.DayOfWeek))
                    continue;

                var workStart = day.Add(userProfile.WorkStartTime.ToTimeSpan());
                var workEnd = day.Add(userProfile.WorkEndTime.ToTimeSpan());

                // Adjust start time if we are already past the work start time today.
                if (day == now.Date && now > workStart)
                {
                    workStart = now;
                }

                // If the effective start is after the end, there's no time to work today.
                if (workStart >= workEnd)
                    continue;

                // Merge overlapping events for the current day
                var dayEvents = MergeOverlappingEvents(sortedEvents.Where(e => e.EndTime > workStart && e.StartTime < workEnd).ToList());

                var currentStart = workStart;
                foreach (var dayEvent in dayEvents)
                {
                    // Add a slot for the free time before the current event
                    if (dayEvent.StartTime > currentStart)
                    {
                        slots.Add(new TimeSlot { StartTime = currentStart, EndTime = dayEvent.StartTime });
                    }
                    // Move the pointer to the end of the current event
                    currentStart = dayEvent.EndTime > currentStart ? dayEvent.EndTime : currentStart;
                }

                // Add the final slot for the remaining time in the workday
                if (currentStart < workEnd)
                {
                    slots.Add(new TimeSlot { StartTime = currentStart, EndTime = workEnd });
                }
            }
            return slots;
        }

        private List<Event> MergeOverlappingEvents(List<Event> events)
        {
            if (events.Count <= 1)
                return events;

            var mergedEvents = new List<Event>();
            var currentEvent = events[0];

            for (int i = 1; i < events.Count; i++)
            {
                var nextEvent = events[i];
                if (nextEvent.StartTime < currentEvent.EndTime)
                {
                    // Overlap detected, merge by extending the current event's end time
                    currentEvent.EndTime = nextEvent.EndTime > currentEvent.EndTime ? nextEvent.EndTime : currentEvent.EndTime;
                }
                else
                {
                    // No overlap, add the completed event and start a new one
                    mergedEvents.Add(currentEvent);
                    currentEvent = nextEvent;
                }
            }
            mergedEvents.Add(currentEvent); // Add the last event

            return mergedEvents;
        }
    }
}

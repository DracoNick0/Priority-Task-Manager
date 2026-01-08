using PriorityTaskManager.MCP;
using System;

namespace PriorityTaskManager.Services.Agents
{
    /// <summary>
    /// Agent responsible for pre-processing tasks before scheduling.
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
            if (!context.SharedState.TryGetValue("UserProfile", out var userProfileObj) || userProfileObj is not Models.UserProfile userProfile)
                return context;
            
            if (!context.SharedState.TryGetValue("Tasks", out var tasksObj) || tasksObj is not List<Models.TaskItem> tasks)
                return context;

            var now = _timeService.GetCurrentTime();

            // Step 3.1: Calculate the Core Horizon End Date
            var totalWorkloadDuration = TimeSpan.FromTicks(tasks.Sum(t => t.EstimatedDuration.Ticks));
            var accumulatedAvailableTime = TimeSpan.Zero;
            var coreHorizonEndDate = now.Date;
            var dailyWorkDuration = userProfile.WorkEndTime.ToTimeSpan() - userProfile.WorkStartTime.ToTimeSpan();

            while (accumulatedAvailableTime < totalWorkloadDuration)
            {
                if (userProfile.WorkDays.Contains(coreHorizonEndDate.DayOfWeek))
                {
                    accumulatedAvailableTime += dailyWorkDuration;
                }
                // Stop if we look more than 5 years into the future to prevent infinite loops
                if (coreHorizonEndDate > now.Date.AddYears(5)) 
                {
                    context.History.Add("Warning: Workload exceeds 5 years of available time. Capping horizon.");
                    break;
                }
                coreHorizonEndDate = coreHorizonEndDate.AddDays(1);
            }

            var scheduleWindow = new PriorityTaskManager.Models.ScheduleWindow();
            var slots = new List<PriorityTaskManager.Models.TimeSlot>();

            // Step 3.2: Generate slots for each workday, splitting around events (if any)
            if (context.SharedState.TryGetValue("Events", out var eventsObj) && eventsObj is List<PriorityTaskManager.Models.Event> events)
            {
                var sortedEvents = events.OrderBy(e => e.StartTime).ToList();
                for (var day = now.Date; day <= coreHorizonEndDate; day = day.AddDays(1))
                {
                    if (!userProfile.WorkDays.Contains(day.DayOfWeek))
                        continue;

                    var start = day.Add(userProfile.WorkStartTime.ToTimeSpan());
                    var end = day.Add(userProfile.WorkEndTime.ToTimeSpan());

                    // If the day is today, adjust start time if we are already past the work start time.
                    if (day == now.Date)
                    {
                        if (now >= end)
                            continue;
                        if (now > start)
                            start = now;
                    }
                    if (end <= start)
                        continue;

                    // Gather all events for this day that overlap the work window
                    var dayEvents = sortedEvents.Where(ev => ev.EndTime > start && ev.StartTime < end).OrderBy(ev => ev.StartTime).ToList();
                    var currentStart = start;
                    foreach (var ev in dayEvents)
                    {
                        if (ev.StartTime > currentStart)
                        {
                            slots.Add(new PriorityTaskManager.Models.TimeSlot { StartTime = currentStart, EndTime = ev.StartTime });
                        }
                        currentStart = ev.EndTime > currentStart ? ev.EndTime : currentStart;
                    }
                    if (currentStart < end)
                    {
                        slots.Add(new PriorityTaskManager.Models.TimeSlot { StartTime = currentStart, EndTime = end });
                    }
                }
            }
            else
            {
                for (var day = now.Date; day <= coreHorizonEndDate; day = day.AddDays(1))
                {
                    if (!userProfile.WorkDays.Contains(day.DayOfWeek))
                        continue;

                    var start = day.Add(userProfile.WorkStartTime.ToTimeSpan());
                    var end = day.Add(userProfile.WorkEndTime.ToTimeSpan());

                    if (day == now.Date)
                    {
                        if (now >= end)
                            continue;
                        if (now > start)
                            start = now;
                    }
                    if (end > start)
                    {
                        slots.Add(new PriorityTaskManager.Models.TimeSlot { StartTime = start, EndTime = end });
                    }
                }
            }

            context.History.Add("  -> Calculated Available Time Slots:");
            foreach (var slot in slots.OrderBy(s => s.StartTime))
            {
                context.History.Add($"    - Slot: {slot.StartTime} to {slot.EndTime}");
            }

            scheduleWindow.AvailableSlots = slots;
            context.SharedState["AvailableScheduleWindow"] = scheduleWindow;

            // Calculate total available time from all slots
            var totalAvailableTime = slots.Sum(slot => (slot.EndTime - slot.StartTime).Ticks);
            context.SharedState["TotalAvailableTime"] = TimeSpan.FromTicks(totalAvailableTime);

            // Ensure we never return null (method signature is non-nullable)
            return context;
        }
    }
}

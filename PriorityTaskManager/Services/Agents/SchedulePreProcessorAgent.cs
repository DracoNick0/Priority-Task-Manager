using PriorityTaskManager.MCP;

namespace PriorityTaskManager.Services.Agents
{
    /// <summary>
    /// Agent responsible for pre-processing tasks before scheduling.
    /// </summary>
    public class SchedulePreProcessorAgent : IAgent
    {
        /// <inheritdoc />
        public MCPContext Act(MCPContext context)
        {
            context.History.Add("Phase 1: Analyzing user's schedule constraints...");
            if (!context.SharedState.TryGetValue("UserProfile", out var userProfileObj) || userProfileObj is not Models.UserProfile userProfile)
                return context;
            
            if (!context.SharedState.TryGetValue("Tasks", out var tasksObj) || tasksObj is not List<Models.TaskItem> tasks)
                return context;

            // Step 3.1: Calculate the Core Horizon End Date
            var totalWorkloadDuration = TimeSpan.FromTicks(tasks.Sum(t => t.EstimatedDuration.Ticks));
            var accumulatedAvailableTime = TimeSpan.Zero;
            var coreHorizonEndDate = DateTime.Today;
            var dailyWorkDuration = userProfile.WorkEndTime.ToTimeSpan() - userProfile.WorkStartTime.ToTimeSpan();

            while (accumulatedAvailableTime < totalWorkloadDuration)
            {
                if (userProfile.WorkDays.Contains(coreHorizonEndDate.DayOfWeek))
                {
                    accumulatedAvailableTime += dailyWorkDuration;
                }
                // Stop if we look more than 5 years into the future to prevent infinite loops
                if (coreHorizonEndDate > DateTime.Today.AddYears(5)) 
                {
                    context.History.Add("Warning: Workload exceeds 5 years of available time. Capping horizon.");
                    break;
                }
                coreHorizonEndDate = coreHorizonEndDate.AddDays(1);
            }

            var scheduleWindow = new PriorityTaskManager.Models.ScheduleWindow();
            var slots = new List<PriorityTaskManager.Models.TimeSlot>();
            var now = DateTime.Now;

            // Step 3.2: Generate slots within the calculated horizon
            for (var day = DateTime.Today; day <= coreHorizonEndDate; day = day.AddDays(1))
            {
                if (userProfile.WorkDays.Contains(day.DayOfWeek))
                {
                    var start = day.Add(userProfile.WorkStartTime.ToTimeSpan());
                    var end = day.Add(userProfile.WorkEndTime.ToTimeSpan());

                    // If the day is today, adjust start time if we are already past the work start time.
                    if (day == DateTime.Today)
                    {
                        // If it's already past the end of the workday, skip today entirely.
                        if (now >= end)
                        {
                            continue;
                        }
                        // If we are currently within the workday, the earliest we can start is now.
                        if (now > start)
                        {
                            start = now;
                        }
                    }

                    // Only add the slot if it represents a positive duration.
                    if (end > start)
                    {
                        slots.Add(new PriorityTaskManager.Models.TimeSlot
                        {
                            StartTime = start,
                            EndTime = end
                        });
                    }
                }
            }

            // Subtract events from the available time slots
            if (context.SharedState.TryGetValue("Events", out var eventsObj) && eventsObj is List<PriorityTaskManager.Models.Event> events)
            {
                var finalSlots = new List<PriorityTaskManager.Models.TimeSlot>();
                var sortedEvents = events.OrderBy(e => e.StartTime).ToList();

                foreach (var slot in slots)
                {
                    var currentStartTime = slot.StartTime;
                    foreach (var ev in sortedEvents)
                    {
                        // If event is outside the current slot, ignore it
                        if (ev.EndTime <= slot.StartTime || ev.StartTime >= slot.EndTime)
                        {
                            continue;
                        }

                        // If there's a gap between the current start time and the event's start, add it as a new slot
                        if (ev.StartTime > currentStartTime)
                        {
                            finalSlots.Add(new PriorityTaskManager.Models.TimeSlot { StartTime = currentStartTime, EndTime = ev.StartTime });
                        }
                        
                        // Move the current start time to after the event
                        currentStartTime = ev.EndTime > currentStartTime ? ev.EndTime : currentStartTime;
                    }

                    // If there's any remaining time in the slot after all events, add it
                    if (currentStartTime < slot.EndTime)
                    {
                        finalSlots.Add(new PriorityTaskManager.Models.TimeSlot { StartTime = currentStartTime, EndTime = slot.EndTime });
                    }
                }
                slots = finalSlots;
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

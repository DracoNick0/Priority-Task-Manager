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
            if (!context.SharedState.ContainsKey("UserProfile"))
                return context;

            var userProfile = context.SharedState["UserProfile"] as PriorityTaskManager.Models.UserProfile;
            if (userProfile == null)
                return context;

            var scheduleWindow = new PriorityTaskManager.Models.ScheduleWindow();
            var slots = new List<PriorityTaskManager.Models.TimeSlot>();
            var today = DateTime.Today;
            var now = DateTime.Now;
            for (int i = 0; i < 7; i++)
            {
                var day = today.AddDays(i);
                if (userProfile.WorkDays.Contains(day.DayOfWeek))
                {
                    var start = day.Add(userProfile.WorkStartTime.ToTimeSpan());
                    var end = day.Add(userProfile.WorkEndTime.ToTimeSpan());
                    // If today
                    if (i == 0)
                    {
                        // If after work end, skip today entirely
                        if (now >= end)
                        {
                            continue;
                        }
                        // If after work start but before work end, trim start to now
                        if (now > start && now < end)
                        {
                            start = now;
                        }
                    }
                    // Only add slot if end is after start (skip if workday is already over)
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

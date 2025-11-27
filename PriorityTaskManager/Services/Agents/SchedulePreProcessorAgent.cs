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
                var newSlots = new List<PriorityTaskManager.Models.TimeSlot>();
                foreach (var slot in slots)
                {
                    var currentSlots = new List<PriorityTaskManager.Models.TimeSlot> { slot };
                    foreach (var ev in events)
                    {
                        var tempSlots = new List<PriorityTaskManager.Models.TimeSlot>();
                        foreach (var currentSlot in currentSlots)
                        {
                            // Case 1: Event is completely outside the slot
                            if (ev.EndTime <= currentSlot.StartTime || ev.StartTime >= currentSlot.EndTime)
                            {
                                tempSlots.Add(currentSlot);
                                continue;
                            }

                            // Case 2: Event creates a new slot before it
                            if (ev.StartTime > currentSlot.StartTime)
                            {
                                tempSlots.Add(new PriorityTaskManager.Models.TimeSlot { StartTime = currentSlot.StartTime, EndTime = ev.StartTime });
                            }

                            // Case 3: Event creates a new slot after it
                            if (ev.EndTime < currentSlot.EndTime)
                            {
                                tempSlots.Add(new PriorityTaskManager.Models.TimeSlot { StartTime = ev.EndTime, EndTime = currentSlot.EndTime });
                            }
                        }
                        currentSlots = tempSlots;
                    }
                    newSlots.AddRange(currentSlots);
                }
                slots = newSlots;
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

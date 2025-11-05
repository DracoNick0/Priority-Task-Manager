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
            if (context == null || !context.SharedState.ContainsKey("UserProfile"))
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
                    // If today, trim start to now if now is after work start but before work end
                    if (i == 0 && now > start && now < end)
                    {
                        start = now;
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
            scheduleWindow.AvailableSlots = slots;
            context.SharedState["AvailableScheduleWindow"] = scheduleWindow;

            // Calculate total available time from all slots
            var totalAvailableTime = slots.Sum(slot => (slot.EndTime - slot.StartTime).Ticks);
            context.SharedState["TotalAvailableTime"] = TimeSpan.FromTicks(totalAvailableTime);

            return context;
        }
    }
}

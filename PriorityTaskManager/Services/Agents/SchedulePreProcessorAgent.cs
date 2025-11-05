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
            for (int i = 0; i < 7; i++)
            {
                var day = today.AddDays(i);
                if (userProfile.WorkDays.Contains(day.DayOfWeek))
                {
                    var start = day.Add(userProfile.WorkStartTime.ToTimeSpan());
                    var end = day.Add(userProfile.WorkEndTime.ToTimeSpan());
                    slots.Add(new PriorityTaskManager.Models.TimeSlot
                    {
                        StartTime = start,
                        EndTime = end
                    });
                }
            }
            scheduleWindow.AvailableSlots = slots;
            context.SharedState["AvailableScheduleWindow"] = scheduleWindow;
            return context;
        }
    }
}

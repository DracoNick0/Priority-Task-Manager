using System.Collections.Generic;

namespace PriorityTaskManager.Models
{
    public class ScheduleWindow
    {
        public List<TimeSlot> AvailableSlots { get; set; } = new List<TimeSlot>();
    }
}

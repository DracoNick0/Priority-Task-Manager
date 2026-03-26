using System.Collections.Generic;

namespace PriorityTaskManager.Models
{
    /// <summary>
    /// Represents the set of all available time slots for scheduling within a given horizon.
    /// This is calculated by taking the user's work hours and subtracting any existing events.
    /// </summary>
    public class ScheduleWindow
    {
        /// <summary>
        /// Gets or sets the list of continuous time slots where work can be scheduled.
        /// </summary>
        public List<TimeSlot> AvailableSlots { get; set; } = new List<TimeSlot>();
    }
}

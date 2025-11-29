namespace PriorityTaskManager.Models
{
    public class TimeSlot
    {
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }

        public TimeSpan Duration => EndTime - StartTime;
    }
}

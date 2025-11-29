using System;

namespace PriorityTaskManager.Models
{
    public class ScheduledChunk
    {
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public TimeSpan Duration => EndTime - StartTime;

        public ScheduledChunk Clone()
        {
            return new ScheduledChunk
            {
                StartTime = this.StartTime,
                EndTime = this.EndTime
            };
        }
    }
}

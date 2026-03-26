using System;

namespace PriorityTaskManager.Models
{
    /// <summary>
    /// Represents a single, contiguous block of time allocated to a `TaskItem`.
    /// A task may be composed of multiple chunks if it is split across days or interruptions.
    /// </summary>
    public class ScheduledChunk
    {
        /// <summary>
        /// Gets or sets the specific start time for this chunk of work.
        /// </summary>
        public DateTime StartTime { get; set; }

        /// <summary>
        /// Gets or sets the specific end time for this chunk of work.
        /// </summary>
        public DateTime EndTime { get; set; }

        /// <summary>
        /// Gets the calculated duration of this time chunk.
        /// </summary>
        public TimeSpan Duration => EndTime - StartTime;

        /// <summary>
        /// Creates a deep copy of this ScheduledChunk.
        /// </summary>
        /// <returns>A new `ScheduledChunk` instance with the same time values.</returns>
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

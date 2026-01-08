using System;

namespace PriorityTaskManager.Services
{
    public class TimeService : ITimeService
    {
        private DateTime? _simulatedTime;

        public DateTime GetCurrentTime()
        {
            return _simulatedTime ?? DateTime.Now;
        }

        public void SetSimulatedTime(DateTime? time)
        {
            _simulatedTime = time;
        }

        public void ClearSimulatedTime()
        {
            _simulatedTime = null;
        }

        public bool IsSimulated()
        {
            return _simulatedTime.HasValue;
        }

        public DateTime? GetSimulatedTime()
        {
            return _simulatedTime;
        }
    }
}

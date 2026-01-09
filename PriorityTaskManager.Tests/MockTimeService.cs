using System;
using PriorityTaskManager.Services;

namespace PriorityTaskManager.Tests
{
    /// <summary>
    /// Mock implementation of ITimeService for controlling time in unit tests.
    /// </summary>
    public class MockTimeService : ITimeService
    {
        private DateTime? _simulatedTime;

        public DateTime GetCurrentTime() => _simulatedTime ?? DateTime.Now;

        public void SetSimulatedTime(DateTime? time)
        {
            _simulatedTime = time;
        }

        public void ClearSimulatedTime()
        {
            _simulatedTime = null;
        }

        public bool IsSimulated() => _simulatedTime.HasValue;

        public DateTime? GetSimulatedTime() => _simulatedTime;

        // This method is for testing convenience and is not part of ITimeService
        public void SetCurrentTime(DateTime customTime)
        {
            _simulatedTime = customTime;
        }
    }
}

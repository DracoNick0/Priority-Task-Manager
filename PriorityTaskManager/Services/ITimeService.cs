using System;

namespace PriorityTaskManager.Services
{
    public interface ITimeService
    {
        DateTime GetCurrentTime();
        void SetSimulatedTime(DateTime? time);
        void ClearSimulatedTime();
        bool IsSimulated();
        DateTime? GetSimulatedTime();
    }
}

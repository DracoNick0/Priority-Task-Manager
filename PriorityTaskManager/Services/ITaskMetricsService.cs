using System;
using PriorityTaskManager.Models;

namespace PriorityTaskManager.Services
{
    public interface ITaskMetricsService
    {
        DateTime FindTargetDayForSlackMeter(DateTime currentTime, UserProfile profile);
        TimeSpan CalculateRealisticSlack(TaskItem task, UserProfile userProfile);
        TimeSpan CalculateActualSlack(TaskItem task);
    }
}

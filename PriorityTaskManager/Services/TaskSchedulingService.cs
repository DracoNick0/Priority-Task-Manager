using PriorityTaskManager.Models;

namespace PriorityTaskManager.Services
{
    /// <summary>
    /// Provides methods for task metrics and slack calculations.
    /// </summary>
    public class TaskMetricsService
    {
        /// <summary>
        /// Determines the target day for the slack meter.
        /// </summary>
        /// <param name="currentTime">The current time.</param>
        /// <param name="profile">The user profile.</param>
        /// <returns>The target day for the slack meter.</returns>
        public DateTime FindTargetDayForSlackMeter(DateTime currentTime, UserProfile profile)
        {
            var endOfWorkday = currentTime.Date.Add(profile.WorkEndTime.ToTimeSpan());
            var isWorkday = profile.WorkDays.Contains(currentTime.DayOfWeek);
            if (isWorkday && currentTime <= endOfWorkday)
            {
                return currentTime.Date;
            }

            var checkDate = currentTime.Date.AddDays(1);
            for (int i = 0; i < 14; i++) // Safety: max 2 weeks
            {
                if (profile.WorkDays.Contains(checkDate.DayOfWeek))
                {
                    return checkDate;
                }
                checkDate = checkDate.AddDays(1);
            }

            return currentTime.Date;
        }

        /// <summary>
        /// Calculates the slack time for a task.
        /// </summary>
        /// <param name="task">The task to calculate slack for.</param>
        /// <param name="userProfile">The user profile.</param>
        /// <returns>The calculated slack time.</returns>
        public TimeSpan CalculateSlack(TaskItem task, UserProfile userProfile)
        {
            if (!task.ScheduledStartTime.HasValue)
                return TimeSpan.Zero;

            var startTime = task.ScheduledStartTime.Value;
            var effectiveDueTime = GetEffectiveDueTime(task, userProfile);

            TimeSpan totalSlack = TimeSpan.Zero;
            var currentDay = startTime.Date;

            while (currentDay <= effectiveDueTime.Date)
            {
                if (userProfile.WorkDays.Contains(currentDay.DayOfWeek))
                {
                    var workStart = currentDay.Add(userProfile.WorkStartTime.ToTimeSpan());
                    var workEnd = currentDay.Add(userProfile.WorkEndTime.ToTimeSpan());

                    if (currentDay == startTime.Date)
                    {
                        totalSlack += workEnd - (startTime > workStart ? startTime : workStart);
                    }
                    else if (currentDay == effectiveDueTime.Date)
                    {
                        totalSlack += (effectiveDueTime < workEnd ? effectiveDueTime : workEnd) - workStart;
                    }
                    else
                    {
                        totalSlack += workEnd - workStart;
                    }
                }

                currentDay = currentDay.AddDays(1);
            }

            return totalSlack - task.EstimatedDuration;
        }

        private DateTime GetEffectiveDueTime(TaskItem task, UserProfile userProfile)
        {
            var workEnd = task.DueDate.Date.Add(userProfile.WorkEndTime.ToTimeSpan());
            return task.DueDate < workEnd ? task.DueDate : workEnd;
        }
    }
}
using PriorityTaskManager.API.Profile;
using PriorityTaskManager.Models;

namespace PriorityTaskManager.Tests.API
{
    public class ProfileDtoExtensionsTests
    {
        [Fact]
        public void ToResponse_MapsAllFieldsFromUserProfile()
        {
            var profile = new UserProfile
            {
                DefaultListSortOption = SortOption.DueDate,
                DesiredBreatherDuration = TimeSpan.FromMinutes(20),
                WorkStartTime = new TimeOnly(8, 30),
                WorkEndTime = new TimeOnly(16, 0),
                WorkDays = new List<DayOfWeek> { DayOfWeek.Monday, DayOfWeek.Wednesday },
                SchedulingMode = SchedulingMode.ConstraintOptimization,
                SlackThresholdDire = 0.25,
                SlackThresholdPressing = 0.75,
                SlackThresholdFocus = 2.0,
                SlackThresholdSafe = 4.0
            };

            var response = profile.ToResponse();

            Assert.Equal(SortOption.DueDate, response.DefaultListSortOption);
            Assert.Equal(TimeSpan.FromMinutes(20), response.DesiredBreatherDuration);
            Assert.Equal(new TimeOnly(8, 30), response.WorkStartTime);
            Assert.Equal(new TimeOnly(16, 0), response.WorkEndTime);
            Assert.Equal(new List<DayOfWeek> { DayOfWeek.Monday, DayOfWeek.Wednesday }, response.WorkDays);
            Assert.Equal(SchedulingMode.ConstraintOptimization, response.SchedulingMode);
            Assert.Equal(0.25, response.SlackThresholdDire);
            Assert.Equal(0.75, response.SlackThresholdPressing);
            Assert.Equal(2.0, response.SlackThresholdFocus);
            Assert.Equal(4.0, response.SlackThresholdSafe);
        }

        [Fact]
        public void ToUserProfile_MapsAllFieldsFromRequest_AndCopiesWorkDaysList()
        {
            var workDays = new List<DayOfWeek> { DayOfWeek.Tuesday, DayOfWeek.Thursday };
            var request = new ProfileRequest(
                SortOption.Alphabetical,
                TimeSpan.FromMinutes(10),
                new TimeOnly(9, 0),
                new TimeOnly(17, 0),
                workDays,
                SchedulingMode.GoldPanning,
                0.5,
                1.0,
                3.0,
                5.0);

            var profile = request.ToUserProfile();

            Assert.Equal(SortOption.Alphabetical, profile.DefaultListSortOption);
            Assert.Equal(TimeSpan.FromMinutes(10), profile.DesiredBreatherDuration);
            Assert.Equal(new TimeOnly(9, 0), profile.WorkStartTime);
            Assert.Equal(new TimeOnly(17, 0), profile.WorkEndTime);
            Assert.Equal(workDays, profile.WorkDays);
            Assert.Equal(SchedulingMode.GoldPanning, profile.SchedulingMode);
            Assert.Equal(0.5, profile.SlackThresholdDire);
            Assert.Equal(1.0, profile.SlackThresholdPressing);
            Assert.Equal(3.0, profile.SlackThresholdFocus);
            Assert.Equal(5.0, profile.SlackThresholdSafe);

            // The mapped profile must not share the request's list instance.
            workDays.Add(DayOfWeek.Friday);
            Assert.DoesNotContain(DayOfWeek.Friday, profile.WorkDays);
        }
    }
}

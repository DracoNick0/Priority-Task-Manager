using PriorityTaskManager.Services;

namespace PriorityTaskManager.Tests.Services
{
    public class TimeServiceTests
    {
        [Fact]
        public void SetSimulatedTime_ShouldSwitchServiceToSimulatedMode()
        {
            var service = new TimeService();
            var simulated = new DateTime(2026, 7, 8, 10, 15, 0);

            service.SetSimulatedTime(simulated);

            Assert.True(service.IsSimulated());
            Assert.Equal(simulated, service.GetSimulatedTime());
            Assert.Equal(simulated, service.GetCurrentTime());
        }

        [Fact]
        public void ClearSimulatedTime_ShouldReturnServiceToRealTimeMode()
        {
            var service = new TimeService();
            service.SetSimulatedTime(new DateTime(2026, 1, 1, 9, 0, 0));

            service.ClearSimulatedTime();

            Assert.False(service.IsSimulated());
            Assert.Null(service.GetSimulatedTime());
        }

        [Fact]
        public void GetCurrentTime_WhenNotSimulated_ShouldTrackRealClock()
        {
            var service = new TimeService();
            var before = DateTime.Now;

            var now = service.GetCurrentTime();

            var after = DateTime.Now;
            Assert.InRange(now, before, after);
        }
    }
}

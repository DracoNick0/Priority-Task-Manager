using PriorityTaskManager.Models;
using PriorityTaskManager.Scheduling.GoldPanning;
using PriorityTaskManager.Services;

namespace PriorityTaskManager.Tests.Scheduling.GoldPanning
{
    /// <summary>
    /// Runs the shared, algorithm-agnostic <see cref="SchedulingInvariantTestsBase"/> suite against the
    /// Gold Panning strategy. This class should stay a thin factory wrapper; add new general scheduling
    /// invariants to the base class, not here. Only Gold-Panning-specific invariant edge cases (if any)
    /// belong in this subclass.
    /// </summary>
    public class GoldPanningInvariantTests : SchedulingInvariantTestsBase
    {
        protected override IUrgencyStrategy CreateStrategy(UserProfile profile, List<Event> events, ITimeService timeService)
        {
            return new GoldPanningStrategy(profile, events, timeService);
        }
    }
}

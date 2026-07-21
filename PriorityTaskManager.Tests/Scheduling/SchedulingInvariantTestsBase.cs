using PriorityTaskManager.Models;
using PriorityTaskManager.Services;
using PriorityTaskManager.Tests.Infrastructure;

namespace PriorityTaskManager.Tests.Scheduling
{
    /// <summary>
    /// Algorithm-agnostic invariant suite for any <see cref="IUrgencyStrategy"/> implementation.
    /// Written purely against the <see cref="IUrgencyStrategy"/>/<see cref="PrioritizationResult"/> contract
    /// so the same invariant checks can be reused by every scheduling strategy (Gold Panning today,
    /// the Constraint Solver in the future) by subclassing and supplying <see cref="CreateStrategy"/>.
    ///
    /// Ownership: general scheduling invariants (no overlap, time bounds, duration adherence, determinism,
    /// etc.) belong here and should be asserted once. Algorithm-specific pipeline stage tests
    /// (e.g. under `Scheduling/GoldPanning`) should only cover mechanics unique to that algorithm's
    /// internal stages, not re-derive these invariants.
    /// See docs/TESTING_STRATEGY.md (Scheduling Algorithms) and docs/TODO.md ((B) 1/5) for the source rules.
    /// </summary>
    public abstract class SchedulingInvariantTestsBase
    {
        /// <summary>
        /// Constructs the concrete <see cref="IUrgencyStrategy"/> under test for a given scenario.
        /// </summary>
        protected abstract IUrgencyStrategy CreateStrategy(UserProfile profile, List<Event> events, ITimeService timeService);

        [Fact]
        public void CalculateUrgency_WhenWorkloadFitsWindow_ShouldPreserveCoreSchedulingInvariants()
        {
            var now = new DateTime(2026, 7, 6, 9, 0, 0); // Monday
            var profile = CreateProfile();
            var events = CreateEvents();

            var tasks = new List<TaskItem>
            {
                CreateTask(1, "Design", 3.0, new DateTime(2026, 7, 8, 17, 0, 0), importance: 8, complexity: 6.0),
                CreateTask(2, "Implementation", 4.0, new DateTime(2026, 7, 9, 17, 0, 0), importance: 9, complexity: 7.0),
                CreateTask(3, "Review", 2.0, new DateTime(2026, 7, 10, 17, 0, 0), importance: 6, complexity: 4.0)
            };

            var expectedDurations = tasks.ToDictionary(t => t.Id, t => t.EstimatedDuration);
            var strategy = CreateStrategy(profile, events, DeterministicTestFixtures.CreateMockTimeService(now));

            var result = strategy.CalculateUrgency(tasks);
            var allChunks = result.Tasks
                .Where(t => expectedDurations.ContainsKey(t.Id))
                .SelectMany(t => t.ScheduledParts)
                .OrderBy(c => c.StartTime)
                .ToList();

            Assert.NotEmpty(allChunks);

            // Invariant: No Overlapping Tasks - no scheduled chunk may overlap another chunk.
            for (var i = 1; i < allChunks.Count; i++)
            {
                Assert.True(allChunks[i - 1].EndTime <= allChunks[i].StartTime,
                    $"Found overlap between chunks ending at {allChunks[i - 1].EndTime:O} and starting at {allChunks[i].StartTime:O}.");
            }

            // Invariant: Time Bounds + Event Blocking - each chunk must fit inside working-day bounds
            // and avoid blocked event windows.
            foreach (var chunk in allChunks)
            {
                Assert.Contains(chunk.StartTime.DayOfWeek, profile.WorkDays);
                Assert.Equal(chunk.StartTime.Date, chunk.EndTime.Date);

                var dayStart = chunk.StartTime.Date.Add(profile.WorkStartTime.ToTimeSpan());
                var dayEnd = chunk.StartTime.Date.Add(profile.WorkEndTime.ToTimeSpan());

                Assert.True(chunk.StartTime >= dayStart, $"Chunk started before workday start: {chunk.StartTime:O}");
                Assert.True(chunk.EndTime <= dayEnd, $"Chunk ended after workday end: {chunk.EndTime:O}");

                Assert.DoesNotContain(events,
                    e => chunk.StartTime < e.EndTime && chunk.EndTime > e.StartTime);
            }

            // Invariant: Task Duration Adherence - when capacity is sufficient, each task keeps its
            // full planned duration.
            foreach (var scheduledTask in result.Tasks.Where(t => expectedDurations.ContainsKey(t.Id)))
            {
                var scheduledDuration = TimeSpan.FromTicks(scheduledTask.ScheduledParts.Sum(c => c.Duration.Ticks));
                Assert.Equal(expectedDurations[scheduledTask.Id], scheduledDuration);
            }

            // Invariant: Task Dropping - Must-schedule/feasible tasks are not dropped when capacity is sufficient.
            Assert.Empty(result.UnscheduledTasks);
        }

        [Fact]
        public void CalculateUrgency_WithIdenticalInputs_ShouldProduceDeterministicReplay()
        {
            var now = new DateTime(2026, 7, 6, 9, 0, 0);
            var profile = CreateProfile();
            var events = CreateEvents();

            var taskTemplates = new List<TaskItem>
            {
                CreateTask(10, "A", 2.5, new DateTime(2026, 7, 8, 17, 0, 0), importance: 7, complexity: 5.0),
                CreateTask(11, "B", 1.5, new DateTime(2026, 7, 7, 17, 0, 0), importance: 8, complexity: 6.0),
                CreateTask(12, "C", 3.0, null, importance: 4, complexity: 3.0)
            };

            // Invariant: Idempotency - identical inputs must produce an identical schedule.
            var firstRunSignature = RunAndCaptureSignature(taskTemplates, profile, events, now);
            var secondRunSignature = RunAndCaptureSignature(taskTemplates, profile, events, now);

            Assert.Equal(firstRunSignature, secondRunSignature);
        }

        private string RunAndCaptureSignature(
            List<TaskItem> taskTemplates,
            UserProfile profileTemplate,
            List<Event> eventTemplates,
            DateTime now)
        {
            var tasks = taskTemplates.Select(t => t.Clone()).ToList();
            var profile = CloneProfile(profileTemplate);
            var events = eventTemplates
                .Select(e => new Event
                {
                    Id = e.Id,
                    Name = e.Name,
                    StartTime = e.StartTime,
                    EndTime = e.EndTime
                })
                .ToList();

            var strategy = CreateStrategy(profile, events, DeterministicTestFixtures.CreateMockTimeService(now));
            var result = strategy.CalculateUrgency(tasks);

            var taskSignature = string.Join(";", result.Tasks
                .OrderBy(t => t.Id)
                .Select(t =>
                {
                    var chunks = string.Join(",", t.ScheduledParts
                        .OrderBy(c => c.StartTime)
                        .Select(c => $"{c.StartTime:O}>{c.EndTime:O}"));
                    return $"{t.Id}:{chunks}";
                }));

            var unscheduledSignature = string.Join(",", result.UnscheduledTasks
                .OrderBy(t => t.Id)
                .Select(t => t.Id));

            return $"{taskSignature}|U:{unscheduledSignature}";
        }

        protected static UserProfile CreateProfile()
        {
            return new UserProfile
            {
                WorkStartTime = new TimeOnly(9, 0),
                WorkEndTime = new TimeOnly(17, 0),
                WorkDays = new List<DayOfWeek>
                {
                    DayOfWeek.Monday,
                    DayOfWeek.Tuesday,
                    DayOfWeek.Wednesday,
                    DayOfWeek.Thursday,
                    DayOfWeek.Friday
                }
            };
        }

        protected static UserProfile CloneProfile(UserProfile profile)
        {
            return new UserProfile
            {
                DefaultListSortOption = profile.DefaultListSortOption,
                DesiredBreatherDuration = profile.DesiredBreatherDuration,
                WorkStartTime = profile.WorkStartTime,
                WorkEndTime = profile.WorkEndTime,
                WorkDays = new List<DayOfWeek>(profile.WorkDays),
                SchedulingMode = profile.SchedulingMode,
                SlackThresholdDire = profile.SlackThresholdDire,
                SlackThresholdPressing = profile.SlackThresholdPressing,
                SlackThresholdFocus = profile.SlackThresholdFocus,
                SlackThresholdSafe = profile.SlackThresholdSafe
            };
        }

        protected static List<Event> CreateEvents()
        {
            return new List<Event>
            {
                new Event
                {
                    Id = 1,
                    Name = "Standup",
                    StartTime = new DateTime(2026, 7, 6, 12, 0, 0),
                    EndTime = new DateTime(2026, 7, 6, 13, 0, 0)
                },
                new Event
                {
                    Id = 2,
                    Name = "Review Meeting",
                    StartTime = new DateTime(2026, 7, 7, 10, 0, 0),
                    EndTime = new DateTime(2026, 7, 7, 11, 0, 0)
                }
            };
        }

        protected static TaskItem CreateTask(
            int id,
            string title,
            double durationHours,
            DateTime? dueDate,
            int importance,
            double complexity)
        {
            var task = DeterministicTestFixtures.CreateTask(
                title,
                importance: importance,
                complexity: complexity,
                durationHours: durationHours,
                dueDate: dueDate);

            task.Id = id;
            return task;
        }
    }
}

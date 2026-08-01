using PriorityTaskManager.Models;
using PriorityTaskManager.Scheduling.GoldPanning;

namespace PriorityTaskManager.Services
{
	/// <summary>
	/// The concrete set of core services produced by <see cref="ServiceComposer.Compose"/>.
	/// Every front end (CLI, API, or future clients) should consume this same service graph
	/// instead of re-deriving its own wiring conventions.
	/// </summary>
	/// <param name="PersistenceService">The persistence service used to load/save the data directory.</param>
	/// <param name="DataContainer">The loaded data (tasks, lists, profile, events) for the data directory.</param>
	/// <param name="TimeService">The time service used for all "current time" reads, real or simulated.</param>
	/// <param name="TaskManagerService">The coordinating service for task/list/profile operations.</param>
	/// <param name="TaskMetricsService">The service used to compute task-level scheduling metrics.</param>
	public record ComposedServices(
		IPersistenceService PersistenceService,
		DataContainer DataContainer,
		ITimeService TimeService,
		TaskManagerService TaskManagerService,
		ITaskMetricsService TaskMetricsService);

	/// <summary>
	/// Builds the shared core service graph (<see cref="TaskManagerService"/>, <see cref="IPersistenceService"/>,
	/// <see cref="ITimeService"/>, and related services) so front ends do not duplicate manual wiring.
	/// See docs/ARCHITECTURE_INTEGRATIONS.md for the boundary this helper satisfies.
	/// </summary>
	public static class ServiceComposer
	{
		/// <summary>
		/// Constructs the core service graph backed by the JSON data files in <paramref name="dataDirectory"/>.
		/// </summary>
		/// <param name="dataDirectory">The directory containing the persisted task/list/profile/event data.</param>
		/// <returns>The composed core services, ready for a front end to build handlers/endpoints on top of.</returns>
		public static ComposedServices Compose(string dataDirectory)
		{
			var persistenceService = new PersistenceService(dataDirectory);
			var dataContainer = persistenceService.LoadData();
			var timeService = new TimeService();

			var urgencyStrategy = new GoldPanningStrategy(dataContainer.UserProfile, dataContainer.Events, timeService);
			var taskManagerService = new TaskManagerService(urgencyStrategy, persistenceService, dataContainer);
			var taskMetricsService = new TaskMetricsService();
			taskManagerService.ApplyListTimePreference(taskManagerService.GetActiveListId(), timeService);

			return new ComposedServices(persistenceService, dataContainer, timeService, taskManagerService, taskMetricsService);
		}
	}
}

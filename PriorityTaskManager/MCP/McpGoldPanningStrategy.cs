using PriorityTaskManager.MCP.Agents;
using PriorityTaskManager.Models;
using PriorityTaskManager.Services;
using PriorityTaskManager.Services.Helpers;
using System.Collections.Generic;
using System.Linq;

namespace PriorityTaskManager.MCP
{
    /// <summary>
    /// Implements the "Gold Panning" scheduling strategy using the MCP (Master Control Program) agent pipeline.
    /// This strategy orchestrates a series of agents to transform a raw list of tasks into a concrete,
    /// day-by-day schedule.
    /// </summary>
    public class McpGoldPanningStrategy : IUrgencyStrategy
    {
        private readonly UserProfile _userProfile;
        private readonly List<Event> _events;
        private readonly ITimeService _timeService;
        
        // The sequence of agents that defines the Gold Panning pipeline.
        private readonly List<IAgent> _agentChain;

        public McpGoldPanningStrategy(UserProfile userProfile, List<Event> events, ITimeService timeService)
        {
            _userProfile = userProfile;
            _events = events;
            _timeService = timeService;

            // The agent chain defines the step-by-step process of the scheduling strategy.
            // Each agent performs a specific transformation on the data.
            _agentChain = new List<IAgent>
            {
                new TaskAnalyzerAgent(),          // 1. Cleans up task data (applies defaults).
                new SchedulePreProcessorAgent(timeService), // 2. Calculates available time slots.
                new PrioritizationAgent(),        // 3. "Weighs" tasks based on urgency and importance.
                new ScheduleSpreaderAgent(),      // 4. Distributes tasks into daily buckets.
                new DaySequencingAgent()          // 5. Arranges tasks within each day.
            };
        }

        /// <summary>
        /// Executes the Gold Panning scheduling pipeline.
        /// </summary>
        /// <param name="tasks">The list of all user tasks.</param>
        /// <returns>A result containing the scheduled tasks and execution history.</returns>
        public PrioritizationResult CalculateUrgency(List<TaskItem> tasks)
        {
            // Separate completed tasks; they are not part of the scheduling process
            // but will be added back to the final result.
            var completedTasks = tasks.Where(t => t.IsCompleted).ToList();
            var activeTasks = tasks.Where(t => !t.IsCompleted).ToList();

            // --- Pipeline Setup ---
            // Clone tasks to ensure the original list is not mutated by the pipeline (e.g., by task splitting).
            var pipelineTasks = activeTasks.Select(t => t.Clone()).ToList();
            
            // Ensure cloned tasks start with a clean slate for scheduling.
            foreach (var t in pipelineTasks) t.ScheduledParts.Clear();

            // Create the initial context for the MCP, providing all necessary data.
            var context = new MCPContext();
            context.SharedState["Tasks"] = pipelineTasks;
            context.SharedState["UserProfile"] = _userProfile;
            context.SharedState["Events"] = _events;
            context.SharedState["TimeService"] = _timeService;

            // --- Pipeline Execution ---
            // The MCP coordinates the agent chain, passing the context from one agent to the next.
            var finalContext = MCP.Coordinate(_agentChain, context);

            // --- Result Aggregation ---
            // After the pipeline runs, the scheduled tasks (including any split fragments) are retrieved.
            var scheduledFragments = finalContext.SharedState.ContainsKey("Tasks")
                ? (List<TaskItem>)finalContext.SharedState["Tasks"]
                : new List<TaskItem>();

            var unschedulable = finalContext.SharedState.ContainsKey("UnschedulableTasks")
                ? (List<TaskItem>)finalContext.SharedState["UnschedulableTasks"]
                : new List<TaskItem>();

            // --- Re-map Scheduled Parts back to Original Tasks ---
            // The pipeline operates on clones. Now, we must transfer the scheduling results
            // (the `ScheduledParts`) back to the original `activeTasks` instances.
            foreach (var task in activeTasks)
            {
                task.ScheduledParts.Clear();
            }

            // Create a map of fragments for efficient lookup.
            var fragmentMap = scheduledFragments.GroupBy(t => t.Id).ToDictionary(g => g.Key, g => g.ToList());

            // For each original active task, find its corresponding fragments and aggregate their scheduled parts.
            foreach (var originalTask in activeTasks)
            {
                if (fragmentMap.TryGetValue(originalTask.Id, out var fragments))
                {
                    foreach (var fragment in fragments)
                    {
                        originalTask.ScheduledParts.AddRange(fragment.ScheduledParts);
                    }
                    // Ensure the chunks are in chronological order.
                    originalTask.ScheduledParts.Sort((a, b) => a.StartTime.CompareTo(b.StartTime));
                }
            }

            // Combine the now-scheduled active tasks with the previously filtered completed tasks.
            var finalTasks = activeTasks.Concat(completedTasks).ToList();

            var result = new PrioritizationResult
            {
                Tasks = finalTasks,
                History = finalContext.History,
                UnscheduledTasks = unschedulable
            };
            return result;
        }
    }
}
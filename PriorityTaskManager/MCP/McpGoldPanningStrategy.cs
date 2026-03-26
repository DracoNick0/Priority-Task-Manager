using PriorityTaskManager.MCP.Agents;
using PriorityTaskManager.Models;
using PriorityTaskManager.Services;
using PriorityTaskManager.Services.Helpers;

namespace PriorityTaskManager.MCP
{
    public class McpGoldPanningStrategy : IUrgencyStrategy
    {
        private readonly UserProfile _userProfile;
        private readonly TaskAnalyzerAgent _taskAnalyzerAgent;
        private readonly SchedulePreProcessorAgent _schedulePreProcessorAgent;
        private readonly PrioritizationAgent _prioritizationAgent;
        private readonly ScheduleSpreaderAgent _scheduleSpreaderAgent;
        private readonly DaySequencingAgent _daySequencingAgent;
        private readonly ITimeService _timeService;
        
        private readonly List<Event> _events;

        public McpGoldPanningStrategy(UserProfile userProfile, List<Event> events, ITimeService timeService)
        {
            _userProfile = userProfile;
            _events = events;
            _timeService = timeService;
            var dependencyHelper = new DependencyGraphHelper();
            _taskAnalyzerAgent = new TaskAnalyzerAgent();
            _schedulePreProcessorAgent = new SchedulePreProcessorAgent(_timeService);
            _prioritizationAgent = new PrioritizationAgent();
            _scheduleSpreaderAgent = new ScheduleSpreaderAgent();
            _daySequencingAgent = new DaySequencingAgent();
            
        }

        public PrioritizationResult CalculateUrgency(List<TaskItem> tasks)
        {
            // Filter out completed tasks so they are not scheduled
            var completedTasks = tasks.Where(t => t.IsCompleted).ToList();
            var activeTasks = tasks.Where(t => !t.IsCompleted).ToList();

            // NEW PIPELINE: TaskAnalyzer -> PreProcessor -> Prioritizer -> Spreader -> Sequencer
            var agentChain = new List<IAgent>
            {
                _taskAnalyzerAgent,
                _schedulePreProcessorAgent,
                _prioritizationAgent,
                _scheduleSpreaderAgent, // Replaces ComplexityBalancer + SchedulingAgent
                _daySequencingAgent
            };

            // Clone tasks for the pipeline so we don't mutate the originals during splitting/optimization
            var pipelineTasks = activeTasks.Select(t => t.Clone()).ToList();
            
            // Initial clear of scheduled parts on clones
            foreach (var t in pipelineTasks) t.ScheduledParts.Clear();

            // Create and populate the context with only active tasks (Clones)
            var context = new MCPContext();
            context.SharedState["Tasks"] = pipelineTasks;
            context.SharedState["UserProfile"] = _userProfile;
            context.SharedState["Events"] = _events;

            // Execute the agent chain
            var finalContext = MCP.Coordinate(agentChain, context);

            // Re-Aggregate result
            var scheduledFragments = finalContext.SharedState.ContainsKey("Tasks")
                ? (List<TaskItem>)finalContext.SharedState["Tasks"]
                : new List<TaskItem>();

            var unschedulable = finalContext.SharedState.ContainsKey("UnschedulableTasks")
                ? (List<TaskItem>)finalContext.SharedState["UnschedulableTasks"]
                : new List<TaskItem>();

            // --- Re-Aggregate Scheduled Parts back to Original Tasks ---
            // Clear old scheduling from originals
            foreach (var task in activeTasks)
            {
                task.ScheduledParts.Clear();
            }

            // Map fragments by ID
            var fragmentMap = scheduledFragments.GroupBy(t => t.Id).ToDictionary(g => g.Key, g => g.ToList());

            // Iterate active tasks and pull parts from fragments
            foreach (var originalTask in activeTasks)
            {
                if (fragmentMap.TryGetValue(originalTask.Id, out var fragments))
                {
                    foreach (var fragment in fragments)
                    {
                        originalTask.ScheduledParts.AddRange(fragment.ScheduledParts);
                    }
                    // Sort chunks by time
                    originalTask.ScheduledParts.Sort((a, b) => a.StartTime.CompareTo(b.StartTime));
                }
            }

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
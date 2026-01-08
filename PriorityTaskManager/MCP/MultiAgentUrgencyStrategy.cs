using PriorityTaskManager.MCP.Agents;
using PriorityTaskManager.Models;
using PriorityTaskManager.Services;
using PriorityTaskManager.Services.Helpers;

namespace PriorityTaskManager.MCP
{
    public class MultiAgentUrgencyStrategy : IUrgencyStrategy
    {
        private readonly UserProfile _userProfile;
        private readonly TaskAnalyzerAgent _taskAnalyzerAgent;
        private readonly SchedulePreProcessorAgent _schedulePreProcessorAgent;
        private readonly PrioritizationAgent _prioritizationAgent;
        private readonly SchedulingAgent _schedulingAgent;
        private readonly ComplexityBalancerAgent _complexityBalancerAgent;
        private readonly ITimeService _timeService;
        
        private readonly List<Event> _events;

        public MultiAgentUrgencyStrategy(UserProfile userProfile, List<Event> events, ITimeService timeService)
        {
            _userProfile = userProfile;
            _events = events;
            _timeService = timeService;
            var dependencyHelper = new DependencyGraphHelper();
            _taskAnalyzerAgent = new TaskAnalyzerAgent();
            _schedulePreProcessorAgent = new SchedulePreProcessorAgent(_timeService);
            _prioritizationAgent = new PrioritizationAgent();
            _complexityBalancerAgent = new ComplexityBalancerAgent();
            _schedulingAgent = new SchedulingAgent(dependencyHelper);
            
        }

        public PrioritizationResult CalculateUrgency(List<TaskItem> tasks)
        {
            // Build the agent chain
            var agentChain = new List<IAgent>
            {
                _taskAnalyzerAgent,
                _schedulePreProcessorAgent,
                _prioritizationAgent,
                _complexityBalancerAgent,
                _schedulingAgent
            };

            // Create and populate the context
            var context = new MCPContext();
            context.SharedState["Tasks"] = tasks;
            context.SharedState["UserProfile"] = _userProfile;
            context.SharedState["Events"] = _events;

            // Execute the agent chain
            var finalContext = MCP.Coordinate(agentChain, context);

            // Retrieve the final data
            var finalTasks = finalContext.SharedState.ContainsKey("Tasks")
                ? (List<TaskItem>)finalContext.SharedState["Tasks"]
                : new List<TaskItem>();

            var unschedulable = finalContext.SharedState.ContainsKey("UnschedulableTasks")
                ? (List<TaskItem>)finalContext.SharedState["UnschedulableTasks"]
                : new List<TaskItem>();

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
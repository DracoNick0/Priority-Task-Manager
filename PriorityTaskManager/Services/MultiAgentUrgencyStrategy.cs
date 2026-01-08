using System.Collections.Generic;
using PriorityTaskManager.Models;

namespace PriorityTaskManager.Services
{
    public class MultiAgentUrgencyStrategy : IUrgencyStrategy
    {
        private readonly UserProfile _userProfile;
        private readonly Agents.TaskAnalyzerAgent _taskAnalyzerAgent;
        private readonly Agents.SchedulePreProcessorAgent _schedulePreProcessorAgent;
        private readonly Agents.PrioritizationAgent _prioritizationAgent;
        private readonly Agents.SchedulingAgent _schedulingAgent;
        private readonly Agents.ComplexityBalancerAgent _complexityBalancerAgent;
        private readonly ITimeService _timeService;
        
        private readonly List<Event> _events;

        public MultiAgentUrgencyStrategy(UserProfile userProfile, List<Event> events, ITimeService timeService)
        {
            _userProfile = userProfile;
            _events = events;
            _timeService = timeService;
            var dependencyHelper = new Helpers.DependencyGraphHelper();
            _taskAnalyzerAgent = new Agents.TaskAnalyzerAgent();
            _schedulePreProcessorAgent = new Agents.SchedulePreProcessorAgent(_timeService);
            _prioritizationAgent = new Agents.PrioritizationAgent();
            _complexityBalancerAgent = new Agents.ComplexityBalancerAgent();
            _schedulingAgent = new Agents.SchedulingAgent(dependencyHelper);
            
        }

        public PrioritizationResult CalculateUrgency(List<TaskItem> tasks)
        {
            // Build the agent chain
            var agentChain = new List<MCP.IAgent>
            {
                _taskAnalyzerAgent,
                _schedulePreProcessorAgent,
                _prioritizationAgent,
                _complexityBalancerAgent,
                _schedulingAgent
            };

            // Create and populate the context
            var context = new MCP.MCPContext();
            context.SharedState["Tasks"] = tasks;
            context.SharedState["UserProfile"] = _userProfile;
            context.SharedState["Events"] = _events;

            // Execute the agent chain
            var finalContext = MCP.MCP.Coordinate(agentChain, context);

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
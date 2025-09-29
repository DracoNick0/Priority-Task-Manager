using System;
using PriorityTaskManager.MCP;
using PriorityTaskManager.Services;

namespace PriorityTaskManager.CLI.MCP.Agents.Cleanup
{
    public class UpdateDependenciesAgent : IAgent
    {
        private readonly TaskManagerService _taskManagerService;

        public UpdateDependenciesAgent(TaskManagerService taskManagerService)
        {
            _taskManagerService = taskManagerService;
        }

        public MCPContext Act(MCPContext context)
        {
            return context;
        }
    }
}
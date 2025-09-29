using System;
using PriorityTaskManager.MCP;
using PriorityTaskManager.Services;

namespace PriorityTaskManager.CLI.MCP.Agents.Cleanup
{
    public class FindCompletedTasksAgent : IAgent
    {
        private readonly TaskManagerService _taskManagerService;

        public FindCompletedTasksAgent(TaskManagerService taskManagerService)
        {
            _taskManagerService = taskManagerService;
        }

        public MCPContext Act(MCPContext context)
        {
            return context;
        }
    }
}
using System;
using PriorityTaskManager.MCP;
using PriorityTaskManager.Services;

namespace PriorityTaskManager.CLI.MCP.Agents.Cleanup
{
    public class DeleteTasksAgent : IAgent
    {
        private readonly TaskManagerService _taskManagerService;

        public DeleteTasksAgent(TaskManagerService taskManagerService)
        {
            _taskManagerService = taskManagerService;
        }

        public MCPContext Act(MCPContext context)
        {
            return context;
        }
    }
}
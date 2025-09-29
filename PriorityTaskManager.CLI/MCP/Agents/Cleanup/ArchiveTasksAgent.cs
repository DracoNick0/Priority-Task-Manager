using System;
using PriorityTaskManager.MCP;
using PriorityTaskManager.Services;

namespace PriorityTaskManager.CLI.MCP.Agents.Cleanup
{
    public class ArchiveTasksAgent : IAgent
    {
        private readonly TaskManagerService _taskManagerService;

        public ArchiveTasksAgent(TaskManagerService taskManagerService)
        {
            _taskManagerService = taskManagerService;
        }

        public MCPContext Act(MCPContext context)
        {
            return context;
        }
    }
}
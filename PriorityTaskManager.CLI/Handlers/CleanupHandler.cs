using System;
using PriorityTaskManager.Interfaces;
using PriorityTaskManager.Services;

namespace PriorityTaskManager.CLI.Handlers
{
    public class CleanupHandler : ICommandHandler
    {
        private readonly TaskManagerService _taskManagerService;

        public CleanupHandler(TaskManagerService taskManagerService)
        {
            _taskManagerService = taskManagerService;
        }

        public void Handle(string[] args)
        {
            // Implementation will be added later
        }
    }
}
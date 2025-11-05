using PriorityTaskManager.Services;
using PriorityTaskManager.CLI.Interfaces;
using PriorityTaskManager.Models;

namespace PriorityTaskManager.CLI.Handlers
{
    /// <summary>
    /// Handles the 'mode' command, allowing users to switch urgency strategy modes.
    /// </summary>
    public class ModeHandler : ICommandHandler
    {
        public void Execute(TaskManagerService service, string[] args)
        {
            if (args.Length == 0)
            {
                var current = service.UserProfile.ActiveUrgencyMode;
                Console.WriteLine($"Current mode: {current}. Available modes: SingleAgent, MultiAgent.");
                return;
            }

            var input = args[0].Trim().ToLower();
            switch (input)
            {
                case "singleagent":
                    service.SetActiveUrgencyMode(UrgencyMode.SingleAgent);
                    Console.WriteLine("Active mode set to SingleAgent.");
                    break;
                case "multiagent":
                    service.SetActiveUrgencyMode(UrgencyMode.MultiAgent);
                    Console.WriteLine("Active mode set to MultiAgent.");
                    break;
                default:
                    Console.WriteLine("Error: Invalid mode. Available modes: SingleAgent, MultiAgent.");
                    break;
            }
        }
    }
}

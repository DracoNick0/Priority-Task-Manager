using PriorityTaskManager.Services;

namespace PriorityTaskManager.CLI.Interfaces
{
    /// <summary>
    /// Defines the contract for all command handlers in the CLI application.
    /// </summary>
    public interface ICommandHandler
    {
        /// <summary>
        /// Executes the command logic using the provided service and arguments.
        /// </summary>
        /// <param name="service">The task manager service to interact with tasks.</param>
        /// <param name="args">The arguments provided by the user for the command.</param>
        void Execute(TaskManagerService service, string[] args);
    }
}

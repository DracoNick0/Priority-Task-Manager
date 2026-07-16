using PriorityTaskManager.Services;

namespace PriorityTaskManager.CLI.Interfaces
{
    /// <summary>
    /// Defines a command handler contract that returns a structured command result.
    /// </summary>
    public interface ICommandResultHandler
    {
        /// <summary>
        /// Executes the command and returns a structured outcome.
        /// </summary>
        /// <param name="service">The task manager service to interact with tasks.</param>
        /// <param name="args">The arguments provided by the user for the command.</param>
        /// <returns>A structured result describing the command outcome.</returns>
        CommandResult ExecuteWithResult(TaskManagerService service, string[] args);
    }
}
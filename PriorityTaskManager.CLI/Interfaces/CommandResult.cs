namespace PriorityTaskManager.CLI.Interfaces
{
    /// <summary>
    /// Represents the outcome of a command execution.
    /// </summary>
    public sealed class CommandResult
    {
        /// <summary>
        /// Gets the result status used for presentation and diagnostics.
        /// </summary>
        public CommandResultStatus Status { get; init; } = CommandResultStatus.Info;

        /// <summary>
        /// Gets the user-facing message associated with this command outcome.
        /// </summary>
        public string Message { get; init; } = string.Empty;

        /// <summary>
        /// Gets a value indicating whether the dashboard should be refreshed after this command.
        /// </summary>
        public bool ShouldRefreshDashboard { get; init; }
    }

    /// <summary>
    /// Defines high-level result classifications for command execution.
    /// </summary>
    public enum CommandResultStatus
    {
        Info,
        Success,
        Warning,
        Error,
        Usage
    }
}
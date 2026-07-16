using PriorityTaskManager.Services;
using System.Text;

namespace PriorityTaskManager.CLI.Utils
{
    /// <summary>
    /// Provides shared parsing and message helpers for non-interactive result-based handlers.
    /// </summary>
    public static class NonInteractiveCommandResultHelper
    {
        /// <summary>
        /// Parses comma-separated display IDs and resolves them to real task IDs in the active list.
        /// </summary>
        public static ParsedTaskIdInput ParseDisplayIds(TaskManagerService service, string[] args)
        {
            var result = new ParsedTaskIdInput();
            if (args == null || args.Length == 0)
            {
                return result;
            }

            result.HasInput = true;
            string input = string.Join(string.Empty, args);
            string[] potentialDisplayIds = input.Split(',', StringSplitOptions.RemoveEmptyEntries);
            int activeListId = service.GetActiveListId();

            foreach (var idString in potentialDisplayIds)
            {
                string trimmedId = idString.Trim();

                if (!int.TryParse(trimmedId, out int displayId))
                {
                    result.InvalidTokens.Add(trimmedId);
                    continue;
                }

                var task = service.GetTaskByDisplayId(displayId, activeListId);
                if (task == null)
                {
                    result.NotFoundDisplayIds.Add(displayId);
                    continue;
                }

                result.ValidTasks.Add((displayId, task.Id));
            }

            return result;
        }

        /// <summary>
        /// Builds a usage-oriented message when no valid IDs are found.
        /// </summary>
        public static string BuildNoValidIdsMessage(ParsedTaskIdInput parseResult, string usage)
        {
            var messageBuilder = new StringBuilder();

            if (!parseResult.HasInput)
            {
                messageBuilder.AppendLine("No task IDs provided.");
            }
            else
            {
                messageBuilder.AppendLine("No valid task IDs provided.");
            }

            AppendParseWarnings(messageBuilder, parseResult);
            messageBuilder.Append(usage);

            return messageBuilder.ToString();
        }

        /// <summary>
        /// Appends parse warnings for invalid and not-found IDs.
        /// </summary>
        public static void AppendParseWarnings(StringBuilder messageBuilder, ParsedTaskIdInput parseResult)
        {
            if (parseResult.InvalidTokens.Count > 0)
            {
                messageBuilder.AppendLine($"Invalid IDs: {string.Join(", ", parseResult.InvalidTokens)}.");
            }

            if (parseResult.NotFoundDisplayIds.Count > 0)
            {
                messageBuilder.AppendLine($"Not found in active list: {string.Join(", ", parseResult.NotFoundDisplayIds)}.");
            }
        }
    }

    /// <summary>
    /// Represents parsed task ID input for non-interactive command handlers.
    /// </summary>
    public sealed class ParsedTaskIdInput
    {
        /// <summary>
        /// Gets or sets whether any command arguments were provided.
        /// </summary>
        public bool HasInput { get; set; }

        /// <summary>
        /// Gets the resolved display ID to real task ID pairs.
        /// </summary>
        public List<(int DisplayId, int RealId)> ValidTasks { get; } = new();

        /// <summary>
        /// Gets invalid non-integer argument fragments.
        /// </summary>
        public List<string> InvalidTokens { get; } = new();

        /// <summary>
        /// Gets display IDs that were valid integers but not found in the active list.
        /// </summary>
        public List<int> NotFoundDisplayIds { get; } = new();
    }
}
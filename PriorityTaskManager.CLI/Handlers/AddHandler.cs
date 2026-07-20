using System;
using System.Collections.Generic;
using System.Text;
using PriorityTaskManager.Services;
using PriorityTaskManager.CLI.Interfaces;
using PriorityTaskManager.Models;
using PriorityTaskManager.CLI.Utils;

namespace PriorityTaskManager.CLI.Handlers
{
    /// <summary>
    /// Handles the 'add' command.
    /// With no arguments, guides the user through interactive prompts to create a new task.
    /// With arguments, parses the title and optional flags non-interactively so the command
    /// can be scripted or exercised by automated tests.
    /// </summary>
    public class AddHandler : ICommandResultHandler
    {
        private const int DefaultImportance = 5;
        private const int DefaultComplexity = 5;
        private const bool DefaultPinned = false;
        private static readonly TimeSpan DefaultDuration = TimeSpan.FromHours(1);

        public AddHandler(ScheduleSnapshotProvider snapshotProvider, ITaskMetricsService taskMetricsService)
        {
            // Dependencies intentionally retained in the constructor to avoid breaking current wiring while this handler migrates toward Program-driven dashboard rendering.
        }

        /// <inheritdoc/>
        public CommandResult ExecuteWithResult(TaskManagerService service, string[] args)
        {
            return args.Length == 0
                ? RunInteractiveAdd(service)
                : ParseArguments(service, args);
        }

        /// <summary>
        /// Interactive flow used when no arguments are supplied. Relies on cursor-based date
        /// input, so it cannot be exercised by an automated test with redirected console input.
        /// </summary>
        private CommandResult RunInteractiveAdd(TaskManagerService service)
        {
            Console.Write("Task Title: ");
            string title = Console.ReadLine() ?? string.Empty;
            while (string.IsNullOrWhiteSpace(title))
            {
                Console.WriteLine("Title is required.");
                Console.Write("Task Title: ");
                title = Console.ReadLine() ?? string.Empty;
            }

            Console.WriteLine($"Adding task: '{title}'");

            // --- Gather Task Details Interactively ---
            int importance = ConsoleInputHelper.PromptForInt("Importance", 1, 10, DefaultImportance);
            int complexity = ConsoleInputHelper.PromptForInt("Complexity", 1, 10, DefaultComplexity);
            bool isPinned = ConsoleInputHelper.PromptForBool("Must Schedule Today?", DefaultPinned);
            TimeSpan duration = ConsoleInputHelper.PromptForDuration("Duration", DefaultDuration);

            Console.WriteLine("Due Date (use arrows to adjust, Enter to confirm):");
            DateTime? dueDate = ConsoleInputHelper.InteractiveDateInput(null);

            // If a due date is provided, set the time to the end of that day (23:59:59).
            // This ensures the task is due by the end of the selected day.
            if (dueDate.HasValue)
            {
                dueDate = dueDate.Value.Date.AddHours(23).AddMinutes(59).AddSeconds(59);
            }

            var task = BuildTask(service, title, importance, complexity, isPinned, duration, dueDate);
            service.AddTask(task);

            return new CommandResult
            {
                Status = CommandResultStatus.Success,
                Message = $"\nTask '{task.Title}' added successfully (ID: {task.DisplayId}).",
                ShouldRefreshDashboard = true
            };
        }

        /// <summary>
        /// Non-interactive flow: <c>add &lt;Title&gt; [--importance &lt;1-10&gt;] [--complexity &lt;1-10&gt;]
        /// [--pinned|--not-pinned] [--duration &lt;e.g. 1.5h|30m&gt;] [--due &lt;yyyy-MM-dd[ HH:mm]&gt;]</c>.
        /// Any attribute not supplied falls back to the same defaults used by the interactive flow.
        /// </summary>
        private static CommandResult ParseArguments(TaskManagerService service, string[] args)
        {
            var titleTokens = new List<string>();
            int index = 0;
            while (index < args.Length && !args[index].StartsWith("--", StringComparison.Ordinal))
            {
                titleTokens.Add(args[index]);
                index++;
            }

            string title = string.Join(" ", titleTokens);
            if (string.IsNullOrWhiteSpace(title))
            {
                return new CommandResult
                {
                    Status = CommandResultStatus.Usage,
                    Message = "Usage: add <Title> [--importance <1-10>] [--complexity <1-10>] [--pinned|--not-pinned] [--duration <1.5h|30m>] [--due <yyyy-MM-dd[ HH:mm]>]",
                    ShouldRefreshDashboard = false
                };
            }

            int importance = DefaultImportance;
            int complexity = DefaultComplexity;
            bool isPinned = DefaultPinned;
            TimeSpan duration = DefaultDuration;
            DateTime? dueDate = null;
            var messageBuilder = new StringBuilder();
            var hadError = false;

            for (; index < args.Length; index++)
            {
                switch (args[index])
                {
                    case "--importance":
                        if (index + 1 < args.Length && int.TryParse(args[index + 1], out int importanceValue) && importanceValue >= 1 && importanceValue <= 10)
                        {
                            importance = importanceValue;
                            index++;
                        }
                        else
                        {
                            messageBuilder.AppendLine("Error: --importance requires an integer between 1 and 10.");
                            hadError = true;
                            if (index + 1 < args.Length) index++;
                        }
                        break;
                    case "--complexity":
                        if (index + 1 < args.Length && int.TryParse(args[index + 1], out int complexityValue) && complexityValue >= 1 && complexityValue <= 10)
                        {
                            complexity = complexityValue;
                            index++;
                        }
                        else
                        {
                            messageBuilder.AppendLine("Error: --complexity requires an integer between 1 and 10.");
                            hadError = true;
                            if (index + 1 < args.Length) index++;
                        }
                        break;
                    case "--pinned":
                        isPinned = true;
                        break;
                    case "--not-pinned":
                        isPinned = false;
                        break;
                    case "--duration":
                        if (index + 1 < args.Length && ConsoleInputHelper.TryParseDuration(args[index + 1], out var durationValue))
                        {
                            duration = durationValue;
                            index++;
                        }
                        else
                        {
                            messageBuilder.AppendLine("Error: --duration requires a value like '1.5h' or '30m'.");
                            hadError = true;
                            if (index + 1 < args.Length) index++;
                        }
                        break;
                    case "--due":
                        if (index + 1 < args.Length && DateTime.TryParse(args[index + 1], out var dueValue))
                        {
                            // Date-only input (no time component) is treated as due by end of that day,
                            // matching the interactive flow's due-date behavior.
                            dueDate = args[index + 1].Contains(':') ? dueValue : dueValue.Date.AddHours(23).AddMinutes(59).AddSeconds(59);
                            index++;
                        }
                        else
                        {
                            messageBuilder.AppendLine("Error: --due requires a valid date, e.g. '2026-08-01' or '2026-08-01 14:30'.");
                            hadError = true;
                            if (index + 1 < args.Length) index++;
                        }
                        break;
                    default:
                        messageBuilder.AppendLine($"Error: Unknown option '{args[index]}'.");
                        hadError = true;
                        break;
                }
            }

            var task = BuildTask(service, title, importance, complexity, isPinned, duration, dueDate);
            service.AddTask(task);

            messageBuilder.AppendLine($"Task '{task.Title}' added successfully (ID: {task.DisplayId}).");

            return new CommandResult
            {
                Status = hadError ? CommandResultStatus.Warning : CommandResultStatus.Success,
                Message = messageBuilder.ToString().TrimEnd(),
                ShouldRefreshDashboard = true
            };
        }

        private static TaskItem BuildTask(TaskManagerService service, string title, int importance, int complexity, bool isPinned, TimeSpan duration, DateTime? dueDate)
        {
            return new TaskItem
            {
                Title = title,
                Description = "", // Description is left empty for quick add. Can be added via 'edit'.
                Importance = importance,
                Complexity = (double)complexity,
                IsPinned = isPinned,
                DueDate = dueDate,
                IsCompleted = false,
                EstimatedDuration = duration,
                Progress = 0.0,
                Dependencies = new List<int>(),
                ListId = service.GetActiveListId()
            };
        }
    }
}

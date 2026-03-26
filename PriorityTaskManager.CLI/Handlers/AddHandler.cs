using System;
using System.Collections.Generic;
using PriorityTaskManager.Services;
using PriorityTaskManager.CLI.Interfaces;
using PriorityTaskManager.Models;
using PriorityTaskManager.CLI.Utils;

namespace PriorityTaskManager.CLI.Handlers
{
    /// <summary>
    /// Handles the 'add' command.
    /// Guides the user through a series of prompts to create a new task with all necessary details.
    /// </summary>
    public class AddHandler : ICommandHandler
    {
        private const int DefaultImportance = 5;
        private const int DefaultComplexity = 5;
        private const bool DefaultPinned = false;
        private static readonly TimeSpan DefaultDuration = TimeSpan.FromHours(1);

        /// <inheritdoc/>
        public void Execute(TaskManagerService service, string[] args)
        {
            string title;
            // If the title is provided as an argument, use it. Otherwise, prompt the user.
            if (args.Length > 0)
            {
                title = string.Join(" ", args);
            }
            else
            {
                Console.Write("Task Title: ");
                title = Console.ReadLine() ?? string.Empty;
                while (string.IsNullOrWhiteSpace(title))
                {
                    Console.WriteLine("Title is required.");
                    Console.Write("Task Title: ");
                    title = Console.ReadLine() ?? string.Empty;
                }
            }

            Console.WriteLine($"Adding task: '{title}'");

            // --- Gather Task Details Interactively ---
            int importance = ConsoleInputHelper.PromptForInt("Importance", 1, 10, DefaultImportance);
            int complexity = ConsoleInputHelper.PromptForInt("Complexity", 1, 10, DefaultComplexity);
            bool isPinned = ConsoleInputHelper.PromptForBool("Must Schedule Today?", DefaultPinned);
            TimeSpan duration = ConsoleInputHelper.PromptForDuration("Duration", DefaultDuration);
            
            Console.WriteLine("Due Date (use arrows to adjust, Enter to confirm):");
            DateTime? dueDate = ConsoleInputHelper.HandleInteractiveDateInput(null);
            
            // If a due date is provided, set the time to the end of that day (23:59:59).
            // This ensures the task is due by the end of the selected day.
            if (dueDate.HasValue)
            {
                dueDate = dueDate.Value.Date.AddHours(23).AddMinutes(59).AddSeconds(59);
            }

            // Create the new task object with the collected information.
            var task = new TaskItem
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

            // Add the task to the system via the TaskManagerService.
            service.AddTask(task);

            Console.WriteLine($"\nTask '{task.Title}' added successfully (ID: {task.DisplayId}).");
        }
    }
}

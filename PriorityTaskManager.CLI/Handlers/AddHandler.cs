using System;
using System.Collections.Generic;
using PriorityTaskManager.Services;
using PriorityTaskManager.CLI.Interfaces;
using PriorityTaskManager.Models;
using PriorityTaskManager.CLI.Utils;

namespace PriorityTaskManager.CLI.Handlers
{
    /// <summary>
    /// Handles the 'add' command, allowing users to quickly add new tasks using a streamlined flow.
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

            // 1. Importance (1-10)
            int importance = ConsoleInputHelper.PromptForInt("Importance", 1, 10, DefaultImportance);

            // 2. Complexity (1-10)
            int complexity = ConsoleInputHelper.PromptForInt("Complexity", 1, 10, DefaultComplexity);

            // 3. Must Schedule (IsPinned)
            bool isPinned = ConsoleInputHelper.PromptForBool("Must Schedule Today?", DefaultPinned);

            // 4. Duration
            TimeSpan duration = ConsoleInputHelper.PromptForDuration("Duration", DefaultDuration);

            // 5. Due Date (Interactive)
            Console.WriteLine("Due Date (use arrows to adjust, Enter to confirm):");
            DateTime? dueDate = ConsoleInputHelper.HandleInteractiveDateInput(null);
            
            // Set time
            if (dueDate.HasValue)
            {
                // Default to End of Day, but allow user to change it if they want? 
                // The requirements were to "apply this time thing to settings too".
                // I should probably offer time adjustment here as well for consistency, 
                // or at least make it clear it is end of day.
                // However, user asked for AddHandler prompt streamlining.
                // Let's stick to the current plan for Add: Date input, default to EOD.
                // But wait, the user said "For edit, lets separate the field...". 
                // And "I'd also like to apply this time thing to settings too."
                // They didn't explicitly ask for AddHandler change, but "Streamline prompts" was the goal.
                
                // Let's just ensure it defaults to EOD properly.
                dueDate = dueDate.Value.Date.AddHours(23).AddMinutes(59).AddSeconds(59);
            }

            // Defaults for other fields
            string description = "";
            List<int> dependencies = new List<int>();

            var task = new TaskItem
            {
                Title = title,
                Description = description,
                Importance = importance,
                Complexity = (double)complexity,
                IsPinned = isPinned,
                DueDate = dueDate,
                IsCompleted = false,
                EstimatedDuration = duration,
                Progress = 0.0,
                Dependencies = dependencies,
                ListId = service.GetActiveListId()
            };

            service.AddTask(task);

            Console.WriteLine($"\nTask '{task.Title}' added successfully (ID: {task.Id}).");
        }
    }
}

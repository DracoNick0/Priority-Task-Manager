using System;
using System.Collections.Generic;
using System.Linq;
using PriorityTaskManager.Services;
using PriorityTaskManager.CLI.Interfaces;
using PriorityTaskManager.CLI.Utils;
using PriorityTaskManager.Models;

namespace PriorityTaskManager.CLI.Handlers
{
    /// <summary>
    /// Handles the 'edit' command, providing an interactive menu for modifying tasks.
    /// </summary>
    public class EditHandler : ICommandHandler
    {
        /// <inheritdoc/>
        public void Execute(TaskManagerService service, string[] args)
        {
            if (args.Length < 1)
            {
                Console.WriteLine("Usage: edit <DisplayID> [attribute] [value]");
                return;
            }

            if (!int.TryParse(args[0], out int displayId))
            {
                Console.WriteLine("Invalid ID format.");
                return;
            }

            var task = service.GetTaskByDisplayId(displayId, service.GetActiveListId());
            if (task == null)
            {
                Console.WriteLine($"Task with Display ID {displayId} not found.");
                return;
            }

            // CLI Single Attribute Edit
            if (args.Length > 1)
            {
                string attribute = args[1];
                string? directValue = args.Length > 2 ? string.Join(" ", args.Skip(2)) : null;
                HandleTargetedUpdate(task, attribute, directValue);
                service.UpdateTask(task);
                Console.WriteLine("Task updated successfully.");
                return;
            }

            // Interactive Mode
            RunInteractiveEdit(service, task);
        }

        private void RunInteractiveEdit(TaskManagerService service, TaskItem task)
        {
            // Clone task to avoid modifying original until save
            var currentTask = task.Clone();

            int selectedIndex = 0;
            Console.CursorVisible = false;

            while (true)
            {
                Console.Clear();
                Console.WriteLine($"Editing Task: {currentTask.Title} (ID: {currentTask.Id})");
                Console.WriteLine("---------------------------------------------");
                
                // Show current values next to menu items
                var displayItems = new List<string>
                {
                    $"Title: {currentTask.Title}",
                    $"Description: {(string.IsNullOrEmpty(currentTask.Description) ? "(none)" : currentTask.Description)}",
                    $"Importance: {currentTask.Importance}",
                    $"Complexity: {currentTask.Complexity}",
                    $"Must Schedule: {(currentTask.IsPinned ? "Yes" : "No")}",
                    $"Duration: {currentTask.EstimatedDuration.TotalHours}h",
                    $"Due Date: {(currentTask.DueDate.HasValue ? currentTask.DueDate.Value.ToString("yyyy-MM-dd") : "None")}",
                    $"Due Time: {(currentTask.DueDate.HasValue ? currentTask.DueDate.Value.ToString("HH:mm") : "End of Day")}",
                    $"Dependencies: [{string.Join(", ", currentTask.Dependencies)}]",
                    "[Save & Exit]",
                    "[Cancel]"
                };

                ConsoleHelper.DrawMenu(displayItems, selectedIndex);

                var key = Console.ReadKey(true);

                // Check for Shift+Enter to immediately save
                if (key.Key == ConsoleKey.Enter && key.Modifiers.HasFlag(ConsoleModifiers.Shift))
                {
                    service.UpdateTask(currentTask);
                    Console.CursorVisible = true;
                    Console.WriteLine("\nTask updated successfully (Shift+Enter).");
                    return;
                }

                switch (key.Key)
                {
                    case ConsoleKey.UpArrow:
                        selectedIndex = (selectedIndex - 1 + displayItems.Count) % displayItems.Count;
                        break;
                    case ConsoleKey.DownArrow:
                        selectedIndex = (selectedIndex + 1) % displayItems.Count;
                        break;
                    case ConsoleKey.Enter:
                        if (selectedIndex == displayItems.Count - 2) // Save & Exit
                        {
                            service.UpdateTask(currentTask);
                            Console.CursorVisible = true;
                            Console.WriteLine("\nTask updated successfully.");
                            return;
                        }
                        if (selectedIndex == displayItems.Count - 1) // Cancel
                        {
                            Console.CursorVisible = true;
                            Console.WriteLine("\nEdit cancelled.");
                            return;
                        }
                        
                        // Edit selected field
                        Console.CursorVisible = true;
                        Console.WriteLine(); // New line for input
                        EditField(service, currentTask, selectedIndex);
                        Console.CursorVisible = false;
                        break;
                    case ConsoleKey.Escape:
                        Console.CursorVisible = true;
                        return;
                }
            }
        }

        private void EditField(TaskManagerService service, TaskItem task, int index)
        {
            switch (index)
            {
                case 0: // Title
                    Console.Write($"New Title (current: {task.Title}): ");
                    var title = Console.ReadLine();
                    if (!string.IsNullOrWhiteSpace(title)) task.Title = title;
                    break;
                case 1: // Description
                    Console.Write($"New Description (current: {task.Description}): ");
                    task.Description = Console.ReadLine() ?? task.Description;
                    break;
                case 2: // Importance
                    task.Importance = ConsoleInputHelper.PromptForInt("New Importance", 1, 10, task.Importance);
                    break;
                case 3: // Complexity
                    task.Complexity = ConsoleInputHelper.PromptForInt("New Complexity", 1, 10, (int)task.Complexity);
                    break;
                case 4: // Must Schedule
                    task.IsPinned = ConsoleInputHelper.PromptForBool("Must Schedule Today?", task.IsPinned);
                    break;
                case 5: // Duration
                    task.EstimatedDuration = ConsoleInputHelper.PromptForDuration("New Duration", task.EstimatedDuration);
                    break;
                case 6: // Due Date
                    Console.WriteLine("Adjust Due Date:");
                    var newDate = ConsoleInputHelper.HandleInteractiveDateInput(task.DueDate);
                    if (newDate.HasValue)
                    {
                        // Preserve time if date was already set, otherwise default to end of day
                        var timePart = task.DueDate.HasValue ? task.DueDate.Value.TimeOfDay : new TimeSpan(23, 59, 59);
                        task.DueDate = newDate.Value.Date + timePart;
                    }
                    else
                    {
                        task.DueDate = null;
                    }
                    break;
                case 7: // Due Time
                    Console.WriteLine("Adjust Due Time:");
                    if (task.DueDate.HasValue)
                    {
                         var newTime = ConsoleInputHelper.HandleInteractiveTimeInput(task.DueDate.Value);
                         task.DueDate = task.DueDate.Value.Date + newTime.TimeOfDay;
                    }
                    else
                    {
                        Console.WriteLine("No due date set. Please set a Due Date first.");
                        Console.ReadKey(true);
                    }
                    break;
                case 8: // Dependencies
                    RunDependencyEditor(service, task);
                    break;
            }
        }

        private void RunDependencyEditor(TaskManagerService service, TaskItem task)
        {
            // Work on a copy of dependencies so changes can be cancelled
            var tempDependencies = new List<int>(task.Dependencies);
            int selectedDepIndex = 0;

            while (true)
            {
                Console.Clear();
                Console.WriteLine($"Editing Dependencies for '{task.Title}'");
                Console.WriteLine("-------------------------------------");

                var menuItems = new List<string>();

                // Add Existing Dependencies
                foreach (var depId in tempDependencies)
                {
                    var depTask = service.GetTaskById(depId);
                    // Handle potential null/removed tasks
                    string depLabel = depTask != null ? $"ID {depId}: {depTask.Title}" : $"ID {depId} (Not Found)";
                    menuItems.Add($"[Remove] {depLabel}");
                }

                // Add Commands
                menuItems.Add("[Add New Dependency]");
                menuItems.Add("[Save & Confirm]");
                menuItems.Add("[Cancel]");

                ConsoleHelper.DrawMenu(menuItems, selectedDepIndex);

                // Check for Shift+Enter to immediately save/confirm
                var key = Console.ReadKey(true);
                if (key.Key == ConsoleKey.Enter && key.Modifiers.HasFlag(ConsoleModifiers.Shift))
                {
                    // Apply changes
                    task.Dependencies.Clear();
                    task.Dependencies.AddRange(tempDependencies);
                    return;
                }

                switch (key.Key)
                {
                    case ConsoleKey.UpArrow:
                        selectedDepIndex = (selectedDepIndex - 1 + menuItems.Count) % menuItems.Count;
                        break;
                    case ConsoleKey.DownArrow:
                        selectedDepIndex = (selectedDepIndex + 1) % menuItems.Count;
                        break;
                    case ConsoleKey.Enter:
                        // If selecting an existing dependency (to remove)
                        if (selectedDepIndex < tempDependencies.Count)
                        {
                            Console.WriteLine("\nRemove this dependency? (y/n)");
                            // Simple prompt instead of full boolean helper to keep flow quick
                            var conf = Console.ReadLine();
                            if (!string.IsNullOrWhiteSpace(conf) && conf.ToLower().StartsWith("y"))
                            {
                                tempDependencies.RemoveAt(selectedDepIndex);
                                if (selectedDepIndex >= tempDependencies.Count && selectedDepIndex > 0)
                                    selectedDepIndex--;
                            }
                        }
                        else
                        {
                            // It's a command
                            int relativeIndex = selectedDepIndex - tempDependencies.Count;
                            
                            if (relativeIndex == 0) // [Add New Dependency]
                            {
                                Console.Write("\nEnter Task ID to depend on: ");
                                var input = Console.ReadLine();
                                if (int.TryParse(input, out int newId))
                                {
                                    if (newId == task.Id)
                                    {
                                        Console.WriteLine("Cannot depend on self. Press key to continue.");
                                        Console.ReadKey(true);
                                    }
                                    else if (tempDependencies.Contains(newId))
                                    {
                                        Console.WriteLine("Dependency already exists. Press key to continue.");
                                        Console.ReadKey(true);
                                    }
                                    else
                                    {
                                        var exists = service.GetTaskById(newId);
                                        if (exists == null)
                                        {
                                            Console.Write($"Warning: Task {newId} not found. Add anyway? (y/n): ");
                                            var force = Console.ReadLine();
                                            if (string.IsNullOrWhiteSpace(force) || !force.ToLower().StartsWith("y"))
                                                continue;
                                        }
                                        tempDependencies.Add(newId);
                                    }
                                }
                            }
                            else if (relativeIndex == 1) // [Save & Confirm]
                            {
                                // Apply changes
                                task.Dependencies.Clear();
                                task.Dependencies.AddRange(tempDependencies);
                                return;
                            }
                            else if (relativeIndex == 2) // [Cancel]
                            {
                                return;
                            }
                        }
                        break;
                    case ConsoleKey.Escape:
                        return; // Cancel logic (no save)
                }
            }
        }

        private void HandleTargetedUpdate(TaskItem task, string attribute, string? directValue)
        {
             switch (attribute.ToLower())
            {
                case "title":
                    if (directValue != null) task.Title = directValue;
                    break;
                case "description":
                    if (directValue != null) task.Description = directValue;
                    break;
                case "importance":
                    if (int.TryParse(directValue, out int imp)) task.Importance = Math.Clamp(imp, 1, 10);
                    break;
                case "complexity":
                    if (double.TryParse(directValue, out double comp)) task.Complexity = Math.Clamp(comp, 1, 10);
                    break;
                case "pinned":
                case "pin":
                    task.IsPinned = !task.IsPinned;
                    break;
                case "duration":
                     if (double.TryParse(directValue, out double dur)) task.EstimatedDuration = TimeSpan.FromHours(dur);
                    break;
                case "duedate":
                    Console.WriteLine("Direct value editing for due date is not supported via CLI argument. Use interactive mode.");
                    break;
                default:
                    Console.WriteLine($"Unknown attribute: {attribute}");
                    break;
            }
        }
    }
}

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
        private readonly ScheduleSnapshotProvider _snapshotProvider;
        private readonly ITaskMetricsService _taskMetricsService;
        private readonly IInteractiveConsoleFacade _console;

        public EditHandler(ScheduleSnapshotProvider snapshotProvider, ITaskMetricsService taskMetricsService)
            : this(snapshotProvider, taskMetricsService, null)
        {
        }

        public EditHandler(ScheduleSnapshotProvider snapshotProvider, ITaskMetricsService taskMetricsService, IInteractiveConsoleFacade? console)
        {
            _snapshotProvider = snapshotProvider;
            _taskMetricsService = taskMetricsService;
            _console = console ?? new InteractiveConsoleFacade();
        }

        /// <inheritdoc/>
        public void Execute(TaskManagerService service, string[] args)
        {
            if (args.Length < 1)
            {
                _console.WriteLine("Usage: edit <DisplayID> [attribute] [value]");
                return;
            }

            if (!int.TryParse(args[0], out int displayId))
            {
                _console.WriteLine("Invalid ID format.");
                return;
            }

            var task = service.GetTaskByDisplayId(displayId, service.GetActiveListId());
            if (task == null)
            {
                _console.WriteLine($"Task with Display ID {displayId} not found.");
                return;
            }

            // CLI Single Attribute Edit
            if (args.Length > 1)
            {
                string attribute = args[1];
                string? directValue = args.Length > 2 ? string.Join(" ", args.Skip(2)) : null;
                HandleTargetedUpdate(task, attribute, directValue);
                service.UpdateTask(task);
                _snapshotProvider.RefreshActiveListSnapshot(out _);
                _console.ClearAndRenderDashboard(_snapshotProvider, _taskMetricsService);
                _console.WriteLine("Task updated successfully.");
                return;
            }

            // Interactive Mode
            InteractiveEdit(service, task);
        }

        private void InteractiveEdit(TaskManagerService service, TaskItem task)
        {
            // Clone task to avoid modifying original until save
            var currentTask = task.Clone();

            int selectedIndex = 0;
            _console.CursorVisible = false;

            _console.ClearAndRenderDashboard(_snapshotProvider, _taskMetricsService);
            _console.WriteLine($"Editing Task: {currentTask.Title} (ID: {currentTask.Id})");
            _console.WriteLine("---------------------------------------------");
            int selectorTop = _console.CursorTop;

            var displayItems = BuildEditMenuItems(currentTask);
            _console.DrawMenuItems(displayItems, selectedIndex, selectorTop);

            while (true)
            {
                var key = _console.ReadKey(true);

                // Check for Shift+Enter to immediately save
                if (key.Key == ConsoleKey.Enter && key.Modifiers.HasFlag(ConsoleModifiers.Shift))
                {
                    service.UpdateTask(currentTask);
                    _console.CursorVisible = true;
                    _snapshotProvider.RefreshActiveListSnapshot(out _);
                    _console.ClearAndRenderDashboard(_snapshotProvider, _taskMetricsService);
                    _console.WriteLine("\nTask updated successfully (Shift+Enter).");
                    return;
                }

                switch (key.Key)
                {
                    case ConsoleKey.UpArrow:
                        var previousUp = selectedIndex;
                        selectedIndex = (selectedIndex - 1 + displayItems.Count) % displayItems.Count;
                        _console.UpdateMenuSelection(displayItems, previousUp, selectedIndex, selectorTop);
                        break;
                    case ConsoleKey.DownArrow:
                        var previousDown = selectedIndex;
                        selectedIndex = (selectedIndex + 1) % displayItems.Count;
                        _console.UpdateMenuSelection(displayItems, previousDown, selectedIndex, selectorTop);
                        break;
                    case ConsoleKey.Enter:
                        if (selectedIndex == displayItems.Count - 2) // Save & Exit
                        {
                            service.UpdateTask(currentTask);
                            _console.CursorVisible = true;
                            _snapshotProvider.RefreshActiveListSnapshot(out _);
                            _console.ClearAndRenderDashboard(_snapshotProvider, _taskMetricsService);
                            _console.WriteLine("\nTask updated successfully.");
                            return;
                        }
                        if (selectedIndex == displayItems.Count - 1) // Cancel
                        {
                            _console.CursorVisible = true;
                            _console.WriteLine("\nEdit cancelled.");
                            return;
                        }
                        
                        // Edit selected field
                        _console.CursorVisible = true;
                        _console.WriteLine(string.Empty); // New line for input
                        EditField(service, currentTask, selectedIndex);
                        _console.CursorVisible = false;
                        displayItems = BuildEditMenuItems(currentTask);
                        _console.DrawMenuItems(displayItems, selectedIndex, selectorTop);
                        break;
                    case ConsoleKey.Escape:
                        _console.CursorVisible = true;
                        return;
                }
            }
        }

        private List<string> BuildEditMenuItems(TaskItem currentTask)
        {
            return new List<string>
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
                    var newDate = ConsoleInputHelper.InteractiveDateInput(task.DueDate);
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
                         var newTime = ConsoleInputHelper.InteractiveTimeInput(task.DueDate.Value);
                         task.DueDate = task.DueDate.Value.Date + newTime.TimeOfDay;
                    }
                    else
                    {
                        _console.WriteLine("No due date set. Please set a Due Date first.");
                        _console.ReadKey(true);
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

            _console.ClearAndRenderDashboard(_snapshotProvider, _taskMetricsService);
            _console.WriteLine($"Editing Dependencies for '{task.Title}'");
            _console.WriteLine("-------------------------------------");
            int selectorTop = _console.CursorTop;

            var menuItems = BuildDependencyMenuItems(service, task, tempDependencies);
            _console.DrawMenuItems(menuItems, selectedDepIndex, selectorTop);

            while (true)
            {
                // Check for Shift+Enter to immediately save/confirm
                var key = _console.ReadKey(true);
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
                        var previousUp = selectedDepIndex;
                        selectedDepIndex = (selectedDepIndex - 1 + menuItems.Count) % menuItems.Count;
                        _console.UpdateMenuSelection(menuItems, previousUp, selectedDepIndex, selectorTop);
                        break;
                    case ConsoleKey.DownArrow:
                        var previousDown = selectedDepIndex;
                        selectedDepIndex = (selectedDepIndex + 1) % menuItems.Count;
                        _console.UpdateMenuSelection(menuItems, previousDown, selectedDepIndex, selectorTop);
                        break;
                    case ConsoleKey.Enter:
                        // If selecting an existing dependency (to remove)
                        if (selectedDepIndex < tempDependencies.Count)
                        {
                            _console.WriteLine("\nRemove this dependency? (y/n)");
                            // Simple prompt instead of full boolean helper to keep flow quick
                            var conf = _console.ReadLine();
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
                                _console.Write("\nEnter Task ID to depend on: ");
                                var input = _console.ReadLine();
                                if (int.TryParse(input, out int newId))
                                {
                                    if (newId == task.Id)
                                    {
                                        _console.WriteLine("Cannot depend on self. Press key to continue.");
                                        _console.ReadKey(true);
                                    }
                                    else if (tempDependencies.Contains(newId))
                                    {
                                        _console.WriteLine("Dependency already exists. Press key to continue.");
                                        _console.ReadKey(true);
                                    }
                                    else
                                    {
                                        var exists = service.GetTaskById(newId);
                                        if (exists == null)
                                        {
                                            _console.Write($"Warning: Task {newId} not found. Add anyway? (y/n): ");
                                            var force = _console.ReadLine();
                                            if (string.IsNullOrWhiteSpace(force) || !force.ToLower().StartsWith("y"))
                                                continue;
                                        }
                                        tempDependencies.Add(newId);
                                    }
                                }

                                menuItems = BuildDependencyMenuItems(service, task, tempDependencies);
                                _console.DrawMenuItems(menuItems, selectedDepIndex, selectorTop);
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

        private List<string> BuildDependencyMenuItems(TaskManagerService service, TaskItem task, List<int> tempDependencies)
        {
            var menuItems = new List<string>();

            foreach (var depId in tempDependencies)
            {
                var depTask = service.GetTaskById(depId);
                var depLabel = depTask != null ? $"ID {depId}: {depTask.Title}" : $"ID {depId} (Not Found)";
                menuItems.Add($"[Remove] {depLabel}");
            }

            menuItems.Add("[Add New Dependency]");
            menuItems.Add("[Save & Confirm]");
            menuItems.Add("[Cancel]");

            return menuItems;
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
                    _console.WriteLine("Direct value editing for due date is not supported via CLI argument. Use interactive mode.");
                    break;
                default:
                    _console.WriteLine($"Unknown attribute: {attribute}");
                    break;
            }
        }
    }
}

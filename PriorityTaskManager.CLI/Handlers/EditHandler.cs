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
    public class EditHandler : ICommandHandler, ICommandResultHandler
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
        /// <remarks>
        /// This handler mixes a targeted (non-interactive) update path with a fully interactive
        /// menu-driven edit flow, both of which already own their console rendering via
        /// <see cref="IInteractiveConsoleFacade"/>. This wrapper exists only so <c>edit</c> can
        /// participate in the canonical <see cref="ICommandResultHandler"/> dispatch contract;
        /// no dashboard refresh or message is deferred to <c>Program.cs</c>.
        /// </remarks>
        public CommandResult ExecuteWithResult(TaskManagerService service, string[] args)
        {
            Execute(service, args);
            return new CommandResult();
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
                            ExitInteractiveEdit("Edit cancelled.");
                            return;
                        }
                        
                        // Edit selected field
                        _console.CursorVisible = true;
                        EditField(service, currentTask, selectedIndex, selectorTop);
                        _console.CursorVisible = false;
                        displayItems = BuildEditMenuItems(currentTask);
                        _console.DrawMenuItems(displayItems, selectedIndex, selectorTop);
                        break;
                    case ConsoleKey.Escape:
                        ExitInteractiveEdit("Edit cancelled.");
                        return;
                }
            }
        }

        private void ExitInteractiveEdit(string message)
        {
            _console.CursorVisible = true;
            _snapshotProvider.RefreshActiveListSnapshot(out _);
            _console.ClearAndRenderDashboard(_snapshotProvider, _taskMetricsService);
            _console.WriteLine(message);
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
                $"Dependencies: [{string.Join(", ", currentTask.Dependencies ?? new List<int>())}]",
                "[Save & Exit]",
                "[Cancel]"
            };
        }

        private void EditField(TaskManagerService service, TaskItem task, int index, int selectorTop)
        {
            switch (index)
            {
                case 0: // Title
                    if (_console.TryPromptInlineInput(selectorTop + index, "> Title: ", task.Title ?? string.Empty, out var title) &&
                        !string.IsNullOrWhiteSpace(title))
                    {
                        task.Title = title.Trim();
                    }
                    break;
                case 1: // Description
                    if (_console.TryPromptInlineInput(selectorTop + index, "> Description: ", task.Description ?? string.Empty, out var description))
                    {
                        task.Description = description;
                    }
                    break;
                case 2: // Importance
                    if (_console.TryPromptInlineInput(selectorTop + index, "> Importance (1-10): ", task.Importance.ToString(), out var importanceInput))
                    {
                        if (int.TryParse(importanceInput, out int importance) && importance >= 1 && importance <= 10)
                        {
                            task.Importance = importance;
                        }
                        else
                        {
                            _console.WriteLine("Invalid importance. Enter a whole number from 1 to 10.");
                            _console.ReadKey(true);
                        }
                    }
                    break;
                case 3: // Complexity
                    if (_console.TryPromptInlineInput(selectorTop + index, "> Complexity (1-10): ", task.Complexity.ToString("0.##"), out var complexityInput))
                    {
                        if (double.TryParse(complexityInput, out double complexity) && complexity >= 1 && complexity <= 10)
                        {
                            task.Complexity = complexity;
                        }
                        else
                        {
                            _console.WriteLine("Invalid complexity. Enter a number from 1 to 10.");
                            _console.ReadKey(true);
                        }
                    }
                    break;
                case 4: // Must Schedule
                    task.IsPinned = !task.IsPinned;
                    break;
                case 5: // Duration
                    if (_console.TryPromptInlineInput(selectorTop + index, "> Duration (e.g. 1.5h or 90m): ", task.EstimatedDuration.TotalHours.ToString("0.##") + "h", out var durationInput))
                    {
                        if (TryParseDuration(durationInput, out var duration))
                        {
                            task.EstimatedDuration = duration;
                        }
                        else
                        {
                            _console.WriteLine("Invalid duration. Use formats like 90m, 1.5h, or 2.");
                            _console.ReadKey(true);
                        }
                    }
                    break;
                case 6: // Due Date
                    PrepareForDateTimeInput(task, "Due Date");
                    _console.WriteLine("Adjust Due Date:");
                    var newDate = ConsoleInputHelper.InteractiveDateInput(task.DueDate);
                    if (newDate.HasValue)
                    {
                        // Preserve time if date was already set, otherwise default to end of day.
                        var timePart = task.DueDate.HasValue ? task.DueDate.Value.TimeOfDay : new TimeSpan(23, 59, 59);
                        task.DueDate = newDate.Value.Date + timePart;
                    }
                    else
                    {
                        task.DueDate = null;
                    }
                    break;
                case 7: // Due Time
                    PrepareForDateTimeInput(task, "Due Time");
                    _console.WriteLine("Adjust Due Time:");
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

        private void PrepareForDateTimeInput(TaskItem task, string fieldLabel)
        {
            _snapshotProvider.RefreshActiveListSnapshot(out _);
            _console.ClearAndRenderDashboard(_snapshotProvider, _taskMetricsService);
            _console.WriteLine($"Editing Task: {task.Title} (ID: {task.Id})");
            _console.WriteLine($"Field: {fieldLabel}");
            _console.WriteLine("---------------------------------------------");
        }

        private static bool TryParseDuration(string input, out TimeSpan duration)
        {
            duration = TimeSpan.Zero;

            if (string.IsNullOrWhiteSpace(input))
            {
                return false;
            }

            var normalized = input.Trim().ToLowerInvariant();
            if (normalized.EndsWith("m"))
            {
                if (double.TryParse(normalized.TrimEnd('m'), out var minutes) && minutes > 0)
                {
                    duration = TimeSpan.FromMinutes(minutes);
                    return true;
                }

                return false;
            }

            if (normalized.EndsWith("h"))
            {
                if (double.TryParse(normalized.TrimEnd('h'), out var hoursWithSuffix) && hoursWithSuffix > 0)
                {
                    duration = TimeSpan.FromHours(hoursWithSuffix);
                    return true;
                }

                return false;
            }

            if (double.TryParse(normalized, out var hours) && hours > 0)
            {
                duration = TimeSpan.FromHours(hours);
                return true;
            }

            return false;
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

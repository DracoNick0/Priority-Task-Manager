using PriorityTaskManager.MCP;
using PriorityTaskManager.Models;
using PriorityTaskManager.Services.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PriorityTaskManager.Services.Agents
{
    public class SchedulingAgent : IAgent
    {
        private readonly DependencyGraphHelper _dependencyGraphHelper;

        public SchedulingAgent(DependencyGraphHelper? dependencyGraphHelper = null)
        {
            _dependencyGraphHelper = dependencyGraphHelper ?? new DependencyGraphHelper();
        }

        public MCPContext Act(MCPContext context)
        {
            context.History.Add("Phase 4.1: Sorting tasks and checking dependencies...");

            if (!context.SharedState.TryGetValue("Tasks", out var tasksObj) || tasksObj is not List<TaskItem> tasks)
                return context;

            if (!context.SharedState.TryGetValue("AvailableScheduleWindow", out var scheduleWindowObj) || scheduleWindowObj is not ScheduleWindow scheduleWindow)
                return context;

            var scheduledTasks = new List<TaskItem>();
            var unscheduledTasks = new List<TaskItem>();

            // Create a dictionary for quick lookups of tasks by their ID.
            var taskDictionary = tasks.ToDictionary(t => t.Id);

            // Step 4.1: Master Sort
            var tasksToProcess = tasks
                .OrderBy(t => t.DueDate)
                .ThenByDescending(t => t.Importance)
                .ToList();

            // Make a mutable copy of the available slots to modify during scheduling
            var availableSlots = scheduleWindow.AvailableSlots.OrderBy(s => s.StartTime).ToList();

            var inflexibleTasks = tasksToProcess.Where(t => !t.IsDivisible).ToList();
            var flexibleTasks = tasksToProcess.Where(t => t.IsDivisible).ToList();

            // --- Loop 1: Inflexible Tasks ---
            context.History.Add("Phase 4.2 & 4.3: Processing inflexible tasks with transactional bumping...");
            foreach (var task in inflexibleTasks)
            {
                task.ScheduledParts.Clear(); // Ensure we start fresh
                bool isScheduled = false;

                // Dependency Check
                if (!AreDependenciesMet(task, scheduledTasks, taskDictionary))
                {
                    unscheduledTasks.Add(task);
                    context.History.Add($"  -> Task '{task.Title}' unscheduled: Blocked by an unscheduled dependency.");
                    continue;
                }

                // Step 4.2: Attempt to find an empty best-fit slot
                TimeSlot? bestFitSlot = FindBestFitSlot(task, availableSlots);

                if (bestFitSlot != null)
                {
                    ScheduleInSlot(task, bestFitSlot, scheduledTasks, availableSlots);
                    isScheduled = true;
                    context.History.Add($"  -> Task '{task.Title}' scheduled in empty slot starting at {bestFitSlot.StartTime}.");
                }
                else
                {
                    // Step 4.3: Transactional Bumping Logic
                    context.History.Add($"  -> No empty slot for '{task.Title}'. Attempting to bump a lower-priority task.");
                    var bumpedTasks = new List<TaskItem>();
                    
                    // Find a task to bump
                    var potentialBumpCandidates = scheduledTasks
                        .Where(st => !st.IsDivisible && task.Importance > st.Importance) // Authority Check
                        .OrderBy(st => st.Importance) // Start with the least important candidate
                        .ToList();

                    foreach (var candidateToBump in potentialBumpCandidates)
                    {
                        var slotOccupiedByCandidate = new TimeSlot
                        {
                            StartTime = candidateToBump.ScheduledParts.First().StartTime,
                            EndTime = candidateToBump.ScheduledParts.First().EndTime
                        };

                        if (task.EstimatedDuration <= slotOccupiedByCandidate.Duration)
                        {
                            // Bump the candidate
                            context.History.Add($"    -> Bumping '{candidateToBump.Title}' (Importance: {candidateToBump.Importance}) for '{task.Title}' (Importance: {task.Importance}).");
                            UnscheduleTask(candidateToBump, scheduledTasks, availableSlots);
                            bumpedTasks.Add(candidateToBump);

                            // Schedule the current task in the newly freed slot
                            ScheduleInSlot(task, slotOccupiedByCandidate, scheduledTasks, availableSlots);
                            isScheduled = true;
                            break; // Stop after the first successful bump
                        }
                    }

                    // Attempt to reschedule the bumped tasks immediately
                    if (bumpedTasks.Any())
                    {
                        foreach (var bumpedTask in bumpedTasks.OrderBy(t => t.DueDate).ThenByDescending(t => t.Importance))
                        {
                            TimeSlot? rescheduleSlot = FindBestFitSlot(bumpedTask, availableSlots);
                            if (rescheduleSlot != null)
                            {
                                ScheduleInSlot(bumpedTask, rescheduleSlot, scheduledTasks, availableSlots);
                                context.History.Add($"    -> Successfully rescheduled bumped task '{bumpedTask.Title}'.");
                            }
                            else
                            {
                                unscheduledTasks.Add(bumpedTask);
                                context.History.Add($"    -> Failed to reschedule bumped task '{bumpedTask.Title}'. It is now unscheduled.");
                            }
                        }
                    }
                }

                if (!isScheduled)
                {
                    unscheduledTasks.Add(task);
                    context.History.Add($"  -> Task '{task.Title}' remains unscheduled: No suitable empty or bumpable slot found.");
                }
            }

            // --- Loop 2: Flexible Tasks ---
            context.History.Add("Phase 4.4: Processing flexible tasks...");
            foreach (var task in flexibleTasks)
            {
                // Placeholder for flexible tasks (Step 4.4)
                unscheduledTasks.Add(task);
            }

            // Finalize context
            context.SharedState["Tasks"] = scheduledTasks;
            if (unscheduledTasks.Any())
            {
                context.SharedState["UnschedulableTasks"] = unscheduledTasks;
            }

            return context;
        }

        private bool AreDependenciesMet(TaskItem task, List<TaskItem> scheduledTasks, Dictionary<Guid, TaskItem> taskDictionary)
        {
            foreach (var dependencyId in task.Dependencies)
            {
                if (taskDictionary.ContainsKey(dependencyId) && !scheduledTasks.Any(st => st.Id == dependencyId))
                {
                    return false;
                }
            }
            return true;
        }

        private TimeSlot? FindBestFitSlot(TaskItem task, List<TimeSlot> availableSlots)
        {
            TimeSlot? bestFitSlot = null;
            TimeSpan smallestWastedTime = TimeSpan.MaxValue;

            for (int i = 0; i < availableSlots.Count; i++)
            {
                var slot = availableSlots[i];
                if (slot.EndTime <= task.DueDate && slot.Duration >= task.EstimatedDuration)
                {
                    var wastedTime = slot.Duration - task.EstimatedDuration;
                    if (wastedTime < smallestWastedTime)
                    {
                        smallestWastedTime = wastedTime;
                        bestFitSlot = slot;
                    }
                }
            }
            return bestFitSlot;
        }

        private void ScheduleInSlot(TaskItem task, TimeSlot slot, List<TaskItem> scheduledTasks, List<TimeSlot> availableSlots)
        {
            var newChunk = new ScheduledChunk
            {
                StartTime = slot.StartTime,
                EndTime = slot.StartTime + task.EstimatedDuration
            };
            task.ScheduledParts.Add(newChunk);
            scheduledTasks.Add(task);

            // Update available slots
            availableSlots.Remove(slot);
            var remainingDuration = slot.Duration - task.EstimatedDuration;
            if (remainingDuration > TimeSpan.Zero)
            {
                var remainingSlot = new TimeSlot
                {
                    StartTime = newChunk.EndTime,
                    EndTime = slot.EndTime
                };
                availableSlots.Add(remainingSlot);
                // Re-sort is important
                availableSlots.Sort((s1, s2) => s1.StartTime.CompareTo(s2.StartTime));
            }
        }

        private void UnscheduleTask(TaskItem task, List<TaskItem> scheduledTasks, List<TimeSlot> availableSlots)
        {
            scheduledTasks.Remove(task);
            var freedSlot = new TimeSlot
            {
                StartTime = task.ScheduledParts.First().StartTime,
                EndTime = task.ScheduledParts.First().EndTime
            };
            task.ScheduledParts.Clear();

            // Merge the freed slot back into availableSlots
            availableSlots.Add(freedSlot);
            MergeAdjacentSlots(availableSlots);
        }

        private void MergeAdjacentSlots(List<TimeSlot> slots)
        {
            if (slots.Count < 2) return;
            slots.Sort((s1, s2) => s1.StartTime.CompareTo(s2.StartTime));

            var merged = new List<TimeSlot>();
            var current = slots[0];

            for (int i = 1; i < slots.Count; i++)
            {
                var next = slots[i];
                if (current.EndTime == next.StartTime)
                {
                    current.EndTime = next.EndTime;
                }
                else
                {
                    merged.Add(current);
                    current = next;
                }
            }
            merged.Add(current);

            slots.Clear();
            slots.AddRange(merged);
        }
    }
}

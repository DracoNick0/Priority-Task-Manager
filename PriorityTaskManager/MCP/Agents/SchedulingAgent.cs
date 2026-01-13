using PriorityTaskManager.MCP;
using PriorityTaskManager.Models;
using PriorityTaskManager.Services.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PriorityTaskManager.MCP.Agents
{
    public class SchedulingAgent : IAgent
    {
        private readonly DependencyGraphHelper _dependencyGraphHelper;
        private readonly bool _enableMultiBump = true; // Feature flag for advanced bumping

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

            var tasksToProcess = tasks;

            // Make a mutable copy of the available slots to modify during scheduling
            var availableSlots = scheduleWindow.AvailableSlots.OrderBy(s => s.StartTime).ToList();

            // Ensure all tasks start with a clean slate before any scheduling attempts.
            foreach (var task in tasksToProcess)
            {
                task.ScheduledParts.Clear();
            }

            var inflexibleTasks = tasksToProcess.Where(t => !t.IsDivisible).ToList();
            var flexibleTasks = tasksToProcess.Where(t => t.IsDivisible).ToList();

            // --- Loop 1: Inflexible Tasks ---
            context.History.Add("Phase 4.2 & 4.3: Processing inflexible tasks with transactional bumping...");
            foreach (var task in inflexibleTasks)
            {
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
                    ScheduleInSlot(task, bestFitSlot, scheduledTasks, availableSlots, context);
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
                            ScheduleInSlot(task, slotOccupiedByCandidate, scheduledTasks, availableSlots, context);
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
                                ScheduleInSlot(bumpedTask, rescheduleSlot, scheduledTasks, availableSlots, context);
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
                // Dependency Check
                if (!AreDependenciesMet(task, scheduledTasks, taskDictionary))
                {
                    unscheduledTasks.Add(task);
                    context.History.Add($"  -> Task '{task.Title}' unscheduled: Blocked by an unscheduled dependency.");
                    continue;
                }

                ScheduleFlexibleTask(task, availableSlots, scheduledTasks, unscheduledTasks, context);
            }

            // Step 4.5: Last Chance Appeal for High-Importance Flexible Tasks
            context.History.Add("Phase 4.5: Last Chance Appeal for important flexible tasks...");

            var scheduledInflexibleTasks = scheduledTasks.Where(st => !st.IsDivisible).ToList();

            if (_enableMultiBump)
            {
                context.History.Add("  -> Multi-bump logic enabled.");
                if (!scheduledInflexibleTasks.Any())
                {
                    context.History.Add("  -> No scheduled inflexible tasks to consider for bumping. Skipping appeal process.");
                }
                else
                {
                    var minInflexibleImportance = scheduledInflexibleTasks.Min(st => st.Importance);
                    var appealingFlexibleTasks = unscheduledTasks
                        .Where(t => t.IsDivisible && t.Importance >= minInflexibleImportance)
                        .OrderByDescending(t => t.Importance)
                        .ToList();

                    if (!appealingFlexibleTasks.Any())
                    {
                        context.History.Add("  -> No unscheduled flexible tasks with enough importance to appeal.");
                    }
                    else
                    {
                        foreach (var flexibleTask in appealingFlexibleTasks)
                        {
                            // Step 1: Calculate Slack and Shortfall
                            var slack = availableSlots.Where(s => s.EndTime <= flexibleTask.DueDate).Sum(s => s.Duration.Ticks);
                            var slackTimeSpan = TimeSpan.FromTicks(slack);
                            var shortfall = flexibleTask.EstimatedDuration - slackTimeSpan;

                            context.History.Add($"  -> Evaluating flexible task '{flexibleTask.Title}'. Needed: {flexibleTask.EstimatedDuration}, Slack: {slackTimeSpan}, Shortfall: {shortfall}.");

                            if (shortfall <= TimeSpan.Zero)
                            {
                                // Enough slack has been freed up by other bumps. No need to bump for this task.
                                context.History.Add($"    -> Sufficient slack available. Attempting to schedule without bumping.");
                                unscheduledTasks.Remove(flexibleTask);
                                ScheduleFlexibleTask(flexibleTask, availableSlots, scheduledTasks, unscheduledTasks, context);
                                continue; // Move to the next appealing task
                            }

                            // Step 2 & 3: Gather and Sort Bump Candidates
                            var bumpCandidates = scheduledTasks
                                .Where(st => !st.IsDivisible && flexibleTask.Importance >= st.Importance)
                                .OrderBy(st => st.Importance)
                                .ThenByDescending(st => st.EstimatedDuration)
                                .ToList();

                            if (!bumpCandidates.Any())
                            {
                                context.History.Add($"    -> No suitable inflexible tasks available to bump. Task remains unscheduled.");
                                continue; // Move to the next appealing task
                            }

                            // Step 4 & 5: Aggregate, Simulate, and Commit/Abort
                            var tasksToBump = new List<TaskItem>();
                            var timeFreedUp = TimeSpan.Zero;
                            bool solutionFound = false;

                            foreach (var candidate in bumpCandidates)
                            {
                                tasksToBump.Add(candidate);
                                timeFreedUp += candidate.EstimatedDuration;
                                if (timeFreedUp >= shortfall)
                                {
                                    solutionFound = true;
                                    break;
                                }
                            }

                            if (solutionFound)
                            {
                                context.History.Add($"    -> Solution found: Bumping {tasksToBump.Count} task(s) to free up {timeFreedUp}.");

                                var bumpedTasksToReschedule = new List<TaskItem>();

                                // Commit the bumps
                                foreach (var taskToBump in tasksToBump)
                                {
                                    UnscheduleTask(taskToBump, scheduledTasks, availableSlots);
                                    bumpedTasksToReschedule.Add(taskToBump);
                                }

                                // Schedule the flexible task
                                unscheduledTasks.Remove(flexibleTask);
                                ScheduleFlexibleTask(flexibleTask, availableSlots, scheduledTasks, unscheduledTasks, context);

                                // Attempt to reschedule the bumped tasks
                                foreach (var bumpedTask in bumpedTasksToReschedule.OrderBy(t => t.DueDate).ThenByDescending(t => t.Importance))
                                {
                                    TimeSlot? rescheduleSlot = FindBestFitSlot(bumpedTask, availableSlots);
                                    if (rescheduleSlot != null)
                                    {
                                        ScheduleInSlot(bumpedTask, rescheduleSlot, scheduledTasks, availableSlots, context);
                                        context.History.Add($"      -> Successfully rescheduled bumped task '{bumpedTask.Title}'.");
                                    }
                                    else
                                    {
                                        unscheduledTasks.Add(bumpedTask);
                                        context.History.Add($"      -> Failed to reschedule bumped task '{bumpedTask.Title}'.");
                                    }
                                }
                            }
                            else
                            {
                                context.History.Add($"    -> No combination of bumps could satisfy the shortfall of {shortfall}.");
                            }
                        }
                    }
                }
            }
            else
            {
                // Original single-bump logic
                if (!scheduledInflexibleTasks.Any())
                {
                    context.History.Add("  -> No scheduled inflexible tasks to consider for bumping. Skipping appeal process.");
                }
                else
                {
                    var minInflexibleImportance = scheduledInflexibleTasks.Min(st => st.Importance);

                    var appealingFlexibleTasks = unscheduledTasks
                        .Where(t => t.IsDivisible && t.Importance > minInflexibleImportance)
                        .OrderByDescending(t => t.Importance)
                        .ToList();

                    if (!appealingFlexibleTasks.Any())
                    {
                        context.History.Add("  -> No unscheduled flexible tasks with enough importance to appeal.");
                    }
                    else
                    {
                        foreach (var flexibleTask in appealingFlexibleTasks)
                        {
                            // Find a low-priority, inflexible task to bump
                            var potentialBumpCandidates = scheduledTasks
                                .Where(st => !st.IsDivisible && flexibleTask.Importance > st.Importance) // Authority Check
                                .OrderBy(st => st.Importance) // Start with the least important candidate
                                .ToList();

                            foreach (var candidateToBump in potentialBumpCandidates)
                            {
                                var slotOccupiedByCandidate = new TimeSlot
                                {
                                    StartTime = candidateToBump.ScheduledParts.First().StartTime,
                                    EndTime = candidateToBump.ScheduledParts.First().EndTime
                                };

                                // Fit Check: Can the flexible task fit entirely within this single freed slot?
                                if (flexibleTask.EstimatedDuration <= slotOccupiedByCandidate.Duration)
                                {
                                    context.History.Add($"  -> Last Chance: Bumping inflexible '{candidateToBump.Title}' for flexible '{flexibleTask.Title}'.");

                                    // 1. Bump the candidate
                                    UnscheduleTask(candidateToBump, scheduledTasks, availableSlots);
                                    unscheduledTasks.Add(candidateToBump);

                                    // 2. Schedule the flexible task
                                    unscheduledTasks.Remove(flexibleTask); // It's about to be scheduled
                                    ScheduleFlexibleTask(flexibleTask, availableSlots, scheduledTasks, unscheduledTasks, context);

                                    // 3. Attempt to reschedule the bumped task immediately
                                    unscheduledTasks.Remove(candidateToBump);
                                    TimeSlot? rescheduleSlot = FindBestFitSlot(candidateToBump, availableSlots);
                                    if (rescheduleSlot != null)
                                    {
                                        ScheduleInSlot(candidateToBump, rescheduleSlot, scheduledTasks, availableSlots, context);
                                        context.History.Add($"    -> Successfully rescheduled bumped task '{candidateToBump.Title}'.");
                                    }
                                    else
                                    {
                                        unscheduledTasks.Add(candidateToBump); // Failed again, put it back
                                        context.History.Add($"    -> Failed to reschedule bumped task '{candidateToBump.Title}'.");
                                    }
                                    goto nextFlexibleTask; // Move to the next unscheduled flexible task
                                }
                            }
                            nextFlexibleTask:;
                        }
                    }
                }
            }

            // Finalize context
            context.SharedState["Tasks"] = scheduledTasks;
            context.SharedState["UnschedulableTasks"] = unscheduledTasks;

            return context;
        }

        private void ScheduleFlexibleTask(TaskItem task, List<TimeSlot> availableSlots, List<TaskItem> scheduledTasks, List<TaskItem> unscheduledTasks, MCPContext context)
        {
            var durationToSchedule = task.EstimatedDuration;
            var slotsToRemove = new List<TimeSlot>();
            var slotsToAdd = new List<TimeSlot>();

            // Define the pool of slots to consider. If no due date, all slots are valid.
            var relevantSlots = task.DueDate.HasValue
                ? availableSlots.Where(s => s.EndTime <= task.DueDate.Value)
                : availableSlots;

            // Iterate through available slots, filling them up
            foreach (var slot in relevantSlots.OrderBy(s => s.StartTime))
            {
                if (durationToSchedule <= TimeSpan.Zero) break;

                var slotDuration = slot.Duration;
                var timeToUse = slotDuration < durationToSchedule ? slotDuration : durationToSchedule;

                if (timeToUse > TimeSpan.Zero)
                {
                    var newChunk = new ScheduledChunk
                    {
                        StartTime = slot.StartTime,
                        EndTime = slot.StartTime + timeToUse
                    };
                    task.ScheduledParts.Add(newChunk);
                    context.History.Add($"  -> Scheduled part of '{task.Title}': {newChunk.StartTime} to {newChunk.EndTime}");

                    durationToSchedule -= timeToUse;

                    // Mark the original slot for removal
                    slotsToRemove.Add(slot);

                    // If the slot was only partially used, create a new slot for the remainder
                    if (slotDuration > timeToUse)
                    {
                        var remainingSlot = new TimeSlot
                        {
                            StartTime = newChunk.EndTime,
                            EndTime = slot.EndTime
                        };
                        slotsToAdd.Add(remainingSlot);
                    }
                }
            }

            // Perform the slot updates after iterating
            if (slotsToRemove.Any())
            {
                availableSlots.RemoveAll(s => slotsToRemove.Contains(s));
                availableSlots.AddRange(slotsToAdd);
                availableSlots.Sort((s1, s2) => s1.StartTime.CompareTo(s2.StartTime));
            }

            // Final check if task was fully scheduled
            if (durationToSchedule <= TimeSpan.Zero)
            {
                scheduledTasks.Add(task);
                context.History.Add($"  -> Flexible task '{task.Title}' successfully scheduled in {task.ScheduledParts.Count} chunk(s).");
            }
            else
            {
                task.ScheduledParts.Clear(); // Incomplete, so clear partial chunks
                if (!unscheduledTasks.Contains(task))
                {
                    unscheduledTasks.Add(task);
                }
                context.History.Add($"  -> Flexible task '{task.Title}' could not be fully scheduled. Remaining duration: {durationToSchedule}.");
            }
        }

        private bool AreDependenciesMet(TaskItem task, List<TaskItem> scheduledTasks, Dictionary<int, TaskItem> taskDictionary)
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
                
                // A slot is valid if the task fits and either the task has no due date,
                // or the slot ends before the task's due date.
                bool isSlotValid = slot.Duration >= task.EstimatedDuration &&
                                   (!task.DueDate.HasValue || slot.EndTime <= task.DueDate.Value);

                if (isSlotValid)
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

        private void ScheduleInSlot(TaskItem task, TimeSlot slot, List<TaskItem> scheduledTasks, List<TimeSlot> availableSlots, MCPContext context)
        {
            var newChunk = new ScheduledChunk
            {
                StartTime = slot.StartTime,
                EndTime = slot.StartTime + task.EstimatedDuration
            };
            task.ScheduledParts.Add(newChunk);
            context.History.Add($"  -> Scheduled '{task.Title}': {newChunk.StartTime} to {newChunk.EndTime}");
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

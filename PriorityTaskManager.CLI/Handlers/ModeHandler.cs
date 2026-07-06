using System;
using PriorityTaskManager.CLI.Interfaces;
using PriorityTaskManager.CLI.Utils;
using PriorityTaskManager.Models;
using PriorityTaskManager.Services;

namespace PriorityTaskManager.CLI.Handlers
{
    public class ModeHandler : ICommandHandler
    {
        private readonly ScheduleSnapshotProvider _snapshotProvider;
        private readonly ITaskMetricsService _taskMetricsService;

        public ModeHandler(ScheduleSnapshotProvider snapshotProvider, ITaskMetricsService taskMetricsService)
        {
            _snapshotProvider = snapshotProvider;
            _taskMetricsService = taskMetricsService;
        }

        public void Execute(TaskManagerService service, string[] args)
        {
            var userProfile = service.UserProfile;

            _snapshotProvider.RefreshActiveListSnapshot(out _);
            ConsoleHelper.ClearAndRenderDashboard(_snapshotProvider, _taskMetricsService);
            
            if (args.Length == 0)
            {
                // Just display current mode if no argument provided
                Console.WriteLine($"Current Scheduling Mode: {userProfile.SchedulingMode}");
                if (userProfile.SchedulingMode == SchedulingMode.ConstraintOptimization)
                {
                    Console.WriteLine("(Note: Constraint Solver strategy is currently under development)");
                }
                Console.WriteLine("Use 'mode gold' or 'mode constraint' to change.");
                return;
            }
            else
            {
                var modeArg = args[0].ToLower();
                if (modeArg == "gold" || modeArg == "goldpanning" || modeArg == "multiagent")
                {
                    userProfile.SchedulingMode = SchedulingMode.GoldPanning;
                }
                else if (modeArg == "constraint" || modeArg == "singleagent" || modeArg == "v1")
                {
                    userProfile.SchedulingMode = SchedulingMode.ConstraintOptimization;
                }
                else
                {
                    Console.WriteLine("Unknown mode. Use 'gold' or 'constraint'.");
                    return;
                }
            }

            service.UpdateUserProfile(userProfile);
            Console.WriteLine($"Scheduling Mode set to: {userProfile.SchedulingMode}");
            
            if (userProfile.SchedulingMode == SchedulingMode.ConstraintOptimization)
            {
                Console.WriteLine("(Note: Constraint Solver strategy is currently under development)");
            }
        }
    }
}

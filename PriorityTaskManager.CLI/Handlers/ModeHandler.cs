using System;
using System.Text;
using PriorityTaskManager.CLI.Interfaces;
using PriorityTaskManager.CLI.Utils;
using PriorityTaskManager.Models;
using PriorityTaskManager.Services;

namespace PriorityTaskManager.CLI.Handlers
{
    public class ModeHandler : ICommandHandler, ICommandResultHandler
    {
        public ModeHandler(ScheduleSnapshotProvider snapshotProvider, ITaskMetricsService taskMetricsService)
        {
            // Dependencies intentionally retained in the constructor to avoid breaking current wiring
            // while this handler migrates toward Program-driven dashboard rendering.
        }

        /// <inheritdoc/>
        public void Execute(TaskManagerService service, string[] args)
        {
            var result = ExecuteWithResult(service, args);
            if (!string.IsNullOrWhiteSpace(result.Message))
            {
                Console.WriteLine(result.Message);
            }
        }

        /// <inheritdoc/>
        public CommandResult ExecuteWithResult(TaskManagerService service, string[] args)
        {
            var userProfile = service.UserProfile;
            var messageBuilder = new StringBuilder();

            if (args.Length == 0)
            {
                messageBuilder.AppendLine($"Current Scheduling Mode: {userProfile.SchedulingMode}");
                if (userProfile.SchedulingMode == SchedulingMode.ConstraintOptimization)
                {
                    messageBuilder.AppendLine("(Note: Constraint Solver strategy is currently under development)");
                }
                messageBuilder.Append("Use 'mode gold' or 'mode constraint' to change.");

                return new CommandResult
                {
                    Status = CommandResultStatus.Info,
                    Message = messageBuilder.ToString(),
                    ShouldRefreshDashboard = false
                };
            }

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
                return new CommandResult
                {
                    Status = CommandResultStatus.Warning,
                    Message = "Unknown mode. Use 'gold' or 'constraint'.",
                    ShouldRefreshDashboard = false
                };
            }

            service.UpdateUserProfile(userProfile);
            messageBuilder.Append($"Scheduling Mode set to: {userProfile.SchedulingMode}");

            if (userProfile.SchedulingMode == SchedulingMode.ConstraintOptimization)
            {
                messageBuilder.AppendLine();
                messageBuilder.Append("(Note: Constraint Solver strategy is currently under development)");
            }

            return new CommandResult
            {
                Status = CommandResultStatus.Success,
                Message = messageBuilder.ToString(),
                ShouldRefreshDashboard = true
            };
        }
    }
}

using System;
using System.Collections.Generic;

namespace PriorityTaskManager.Scheduling.GoldPanning
{
    /// <summary>
    /// The Gold Panning pipeline coordinator.
    /// This static class is responsible for executing a chain of stages in sequence,
    /// passing a shared context between them. It forms the backbone of the agent-based
    //  scheduling pipeline, allowing for flexible and composable strategies.
    /// </summary>
    public static class PipelineCoordinator
    {
        /// <summary>
        /// Coordinates the execution of a list of stages.
        /// Each stage acts on the context, and the final, transformed context is returned.
        /// </summary>
        /// <param name="stages">The ordered list of stages to execute.</param>
        /// <param name="initialContext">The initial state before the pipeline begins.</param>
        /// <returns>The final context after all stages have been processed.</returns>
        public static SchedulingContext Coordinate(List<ISchedulingStage> stages, SchedulingContext initialContext)
        {
            SchedulingContext currentContext = initialContext;

            foreach (var stage in stages)
            {
                currentContext = stage.Act(currentContext);

                // Allows a stage to prematurely halt the pipeline if a critical error
                // occurs or if a definitive result is reached early.
                if (currentContext.ShouldTerminate)
                {
                    break;
                }
            }

            return currentContext;
        }
    }
}
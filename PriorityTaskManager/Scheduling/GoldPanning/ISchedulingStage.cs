using System;

namespace PriorityTaskManager.Scheduling.GoldPanning
{
    /// <summary>
    /// Defines the contract for a scheduling stage within the Gold Panning pipeline.
    /// A stage is a modular component responsible for a specific, atomic transformation
    /// of the scheduling context. Stages are chained together to form a processing pipeline.
    /// </summary>
    public interface ISchedulingStage
    {
        /// <summary>
        /// Executes the stage's logic on the given context.
        /// </summary>
        /// <param name="context">The current state of the scheduling process.</param>
        /// <returns>The modified context after the stage has acted.</returns>
        SchedulingContext Act(SchedulingContext context);
    }
}
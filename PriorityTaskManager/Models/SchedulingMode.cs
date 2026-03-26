namespace PriorityTaskManager.Models
{
    /// <summary>
    /// Defines the available scheduling algorithms that the user can choose from.
    /// </summary>
    public enum SchedulingMode
    {
        /// <summary>
        /// The "Gold Panning" strategy, which uses the Master Control Program (MCP)
        /// agent pipeline to flow tasks through time based on urgency and importance.
        /// </summary>
        GoldPanning,

        /// <summary>
        /// The "Constraint Solver" strategy.
        /// It uses an optimization engine to find the best schedule that satisfies a set of rules and constraints.
        /// </summary>
        ConstraintOptimization
    }
}
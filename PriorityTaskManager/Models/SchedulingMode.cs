namespace PriorityTaskManager.Models
{
    public enum SchedulingMode
    {
        /// <summary>
        /// The legacy "Gold Panning" strategy (Multi-Agent Coordination Pattern).
        /// Flows tasks through time based on gravity (Urgency/Importance).
        /// </summary>
        GoldPanning,

        /// <summary>
        /// The new V1 strategy (Constraint Solver).
        /// Uses a solver to optimize the schedule against a set of constraints.
        /// </summary>
        ConstraintOptimization
    }
}
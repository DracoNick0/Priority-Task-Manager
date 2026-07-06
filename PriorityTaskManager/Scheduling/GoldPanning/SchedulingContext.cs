using System;
using System.Collections.Generic;

namespace PriorityTaskManager.Scheduling.GoldPanning
{
    /// <summary>
    /// Represents the shared state and history that is passed between stages in the Gold Panning pipeline.
    /// It acts as a "blackboard" where stages can read data from previous steps and write
    /// results for subsequent steps.
    /// </summary>
    public class SchedulingContext
    {
        /// <summary>
        /// A dictionary holding the shared data. Stages use string keys to access and
        /// modify objects like the list of tasks, user profile, or calculated weights.
        /// </summary>
        public Dictionary<string, object> SharedState { get; set; }

        /// <summary>
        /// A running log of the pipeline's execution. Each stage should add entries
        /// to this list to provide a traceable history of the scheduling process.
        /// </summary>
        public List<string> History { get; set; }

        /// <summary>
        /// If a stage encounters a non-fatal but significant error, it can be stored here.
        /// </summary>
        public Exception? LastError { get; set; }

        /// <summary>
        /// A flag that a stage can set to true to stop the execution of the stage chain.
        /// </summary>
        public bool ShouldTerminate { get; set; } = false;

        public SchedulingContext()
        {
            SharedState = new Dictionary<string, object>();
            History = new List<string>();
        }
    }
}
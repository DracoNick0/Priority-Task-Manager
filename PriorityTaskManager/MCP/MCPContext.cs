using System;
using System.Collections.Generic;

namespace PriorityTaskManager.MCP
{
    /// <summary>
    /// Represents the shared state and history that is passed between agents in the MCP pipeline.
    /// It acts as a "blackboard" where agents can read data from previous steps and write
    /// results for subsequent steps.
    /// </summary>
    public class MCPContext
    {
        /// <summary>
        /// A dictionary holding the shared data. Agents use string keys to access and
        /// modify objects like the list of tasks, user profile, or calculated weights.
        /// </summary>
        public Dictionary<string, object> SharedState { get; set; }

        /// <summary>
        /// A running log of the pipeline's execution. Each agent should add entries
        /// to this list to provide a traceable history of the scheduling process.
        /// </summary>
        public List<string> History { get; set; }

        /// <summary>
        /// If an agent encounters a non-fatal but significant error, it can be stored here.
        /// </summary>
        public Exception? LastError { get; set; }

        /// <summary>
        /// A flag that an agent can set to true to stop the execution of the agent chain.
        /// </summary>
        public bool ShouldTerminate { get; set; } = false;

        public MCPContext()
        {
            SharedState = new Dictionary<string, object>();
            History = new List<string>();
        }
    }
}
using System;

namespace PriorityTaskManager.MCP
{
    /// <summary>
    /// Defines the contract for an "Agent" within the Master Control Program (MCP) framework.
    /// An agent is a modular component responsible for a specific, atomic transformation
    /// of the scheduling context. Agents are chained together to form a processing pipeline.
    /// </summary>
    public interface IAgent
    {
        /// <summary>
        /// Executes the agent's logic on the given context.
        /// </summary>
        /// <param name="context">The current state of the scheduling process.</param>
        /// <returns>The modified context after the agent has acted.</returns>
        MCPContext Act(MCPContext context);
    }
}
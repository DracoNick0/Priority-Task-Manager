using System;
using System.Collections.Generic;

namespace PriorityTaskManager.MCP
{
    /// <summary>
    /// The Master Control Program (MCP) orchestrator.
    /// This static class is responsible for executing a chain of agents in sequence,
    /// passing a shared context between them. It forms the backbone of the agent-based
    //  scheduling pipeline, allowing for flexible and composable strategies.
    /// </summary>
    public static class MCP
    {
        /// <summary>
        /// Coordinates the execution of a list of agents.
        /// Each agent acts on the context, and the final, transformed context is returned.
        /// </summary>
        /// <param name="agents">The ordered list of agents to execute.</param>
        /// <param name="initialContext">The initial state before the pipeline begins.</param>
        /// <returns>The final context after all agents have been processed.</returns>
        public static MCPContext Coordinate(List<IAgent> agents, MCPContext initialContext)
        {
            MCPContext currentContext = initialContext;

            foreach (var agent in agents)
            {
                currentContext = agent.Act(currentContext);

                // Allows an agent to prematurely halt the pipeline if a critical error
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
using System;
using System.Collections.Generic;

namespace PriorityTaskManager.MCP
{
    public static class MCP
    {
        public static MCPContext Coordinate(List<IAgent> agents, MCPContext initialContext)
        {
            MCPContext currentContext = initialContext;

            foreach (var agent in agents)
            {
                currentContext = agent.Act(currentContext);

                if (currentContext.ShouldTerminate)
                {
                    break;
                }
            }

            return currentContext;
        }
    }
}
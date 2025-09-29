using System;

namespace PriorityTaskManager.MCP
{
    public interface IAgent
    {
        MCPContext Act(MCPContext context);
    }
}
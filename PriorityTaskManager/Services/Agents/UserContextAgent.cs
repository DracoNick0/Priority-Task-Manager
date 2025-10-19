using PriorityTaskManager.MCP;

namespace PriorityTaskManager.Services.Agents
{
    public class UserContextAgent : IAgent
    {
        public MCPContext Act(MCPContext context)
        {
            context.History.Add("UserContextAgent execution started (not yet implemented).");
            // TODO: Implement user context logic in Week 3.
            return context;
        }
    }
}

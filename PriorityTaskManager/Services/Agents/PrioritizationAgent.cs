using PriorityTaskManager.MCP;

namespace PriorityTaskManager.Services.Agents
{
    public class PrioritizationAgent : IAgent
    {
        public MCPContext Act(MCPContext context)
        {
            context.History.Add("PrioritizationAgent execution started (not yet implemented).");
            // TODO: Implement prioritization logic in Week 3.
            return context;
        }
    }
}

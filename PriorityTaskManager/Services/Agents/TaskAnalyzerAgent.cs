using PriorityTaskManager.MCP;

namespace PriorityTaskManager.Services.Agents
{
    public class TaskAnalyzerAgent : IAgent
    {
        public MCPContext Act(MCPContext context)
        {
            context.History.Add("TaskAnalyzerAgent execution started (not yet implemented).");
            // TODO: Implement analysis logic in Week 3.
            return context;
        }
    }
}

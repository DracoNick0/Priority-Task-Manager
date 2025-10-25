using PriorityTaskManager.MCP;

namespace PriorityTaskManager.Services.Agents
{
    /// <summary>
    /// Agent responsible for spreading scheduled tasks to avoid conflicts and optimize distribution.
    /// </summary>
    public class ScheduleSpreaderAgent : IAgent
    {
        /// <inheritdoc />
        public MCPContext Act(MCPContext context)
        {
            // Placeholder: No-op for now
            return context;
        }
    }
}

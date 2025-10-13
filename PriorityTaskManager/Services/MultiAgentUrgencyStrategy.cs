using System.Collections.Generic;
using PriorityTaskManager.Models;

namespace PriorityTaskManager.Services
{
    public class MultiAgentUrgencyStrategy : IUrgencyStrategy
    {
        public List<TaskItem> CalculateUrgency(List<TaskItem> tasks)
        {
            // TODO: Implement multi-agent MCP logic here in a future sprint.
            return tasks;
        }
    }
}
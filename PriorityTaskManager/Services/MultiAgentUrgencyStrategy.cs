using System.Collections.Generic;
using PriorityTaskManager.Models;

namespace PriorityTaskManager.Services
{
    public class MultiAgentUrgencyStrategy : IUrgencyStrategy
    {
        private readonly UserProfile _userProfile;

        public MultiAgentUrgencyStrategy(UserProfile userProfile)
        {
            _userProfile = userProfile;
        }

        public List<TaskItem> CalculateUrgency(List<TaskItem> tasks)
        {
            // TODO: Implement multi-agent MCP logic here in a future sprint.
            // Example: context.SharedState["UserProfile"] = _userProfile;
            return tasks;
        }
    }
}
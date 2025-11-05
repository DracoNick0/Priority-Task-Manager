using System.Collections.Generic;

namespace PriorityTaskManager.Models
{
    public class PrioritizationResult
    {
        public List<TaskItem> Tasks { get; set; } = new List<TaskItem>();
        public List<string> History { get; set; } = new List<string>();
    }
}

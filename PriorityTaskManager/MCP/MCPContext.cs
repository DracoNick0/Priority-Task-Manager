using System;
using System.Collections.Generic;

namespace PriorityTaskManager.MCP
{
    public class MCPContext
    {
        public Dictionary<string, object> SharedState { get; set; }
        public List<string> History { get; set; }
        public Exception? LastError { get; set; }
        public bool ShouldTerminate { get; set; } = false;

        public MCPContext()
        {
            SharedState = new Dictionary<string, object>();
            History = new List<string>();
        }
    }
}
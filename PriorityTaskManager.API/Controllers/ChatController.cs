using Microsoft.AspNetCore.Mvc;
using PriorityTaskManager.Models;
using PriorityTaskManager.Services;

namespace PriorityTaskManager.API.Controllers
{
    public class ChatRequest
    {
        public string Message { get; set; } = string.Empty;
    }

    public class ChatResponse
    {
        public string Response { get; set; } = string.Empty;
        public bool Success { get; set; }
    }

    [ApiController]
    [Route("api/[controller]")]
    public class ChatController : ControllerBase
    {
        private readonly TaskManagerService _taskManager;

        public ChatController(TaskManagerService taskManager)
        {
            _taskManager = taskManager;
        }

        [HttpPost]
        public async Task<ActionResult<ChatResponse>> SendMessage([FromBody] ChatRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Message))
            {
                return BadRequest("Message cannot be empty.");
            }

            // TODO: Integrate actual LLM (OpenAI/Gemini) here.
            // Flow:
            // 1. Send 'request.Message' + System Prompt (defining tools) to LLM.
            // 2. LLM returns Function Call (e.g., AddTask).
            // 3. Execute Function against _taskManager.
            // 4. Return success message.

            // MOCK IMPLEMENTATION for testing Android App
            string lowerMsg = request.Message.ToLower();
            if (lowerMsg.StartsWith("add task"))
            {
                var title = request.Message.Substring(8).Trim();
                if (string.IsNullOrWhiteSpace(title)) title = "New Task";
                
                _taskManager.AddTask(new TaskItem 
                { 
                    Title = title,
                    ListId = _taskManager.GetActiveListId(),
                    DueDate = DateTime.Now.AddDays(1),
                    Importance = 5,
                    Complexity = 1.0
                });

                return Ok(new ChatResponse 
                { 
                    Success = true, 
                    Response = $"I've added the task '{title}' for you." 
                });
            }
            else if (lowerMsg.Contains("list"))
            {
                var count = _taskManager.GetTaskCount();
                return Ok(new ChatResponse 
                { 
                    Success = true, 
                    Response = $"You have {count} tasks in your list." 
                });
            }

            return Ok(new ChatResponse 
            { 
                Success = false, 
                Response = "I'm sorry, I didn't understand that. Currently I only support 'add task [name]' and 'list'." 
            });
        }
    }
}

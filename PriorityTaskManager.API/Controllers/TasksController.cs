using Microsoft.AspNetCore.Mvc;
using PriorityTaskManager.Models;
using PriorityTaskManager.Services;

namespace PriorityTaskManager.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TasksController : ControllerBase
    {
        private readonly TaskManagerService _taskManager;
        private readonly ITimeService _timeService;

        public TasksController(TaskManagerService taskManager, ITimeService timeService)
        {
            _taskManager = taskManager;
            _timeService = timeService;
        }

        // GET: api/tasks?listId=1
        [HttpGet]
        public ActionResult<IEnumerable<TaskItem>> GetAllTasks([FromQuery] int listId = 0)
        {
            try 
            {
                if (listId > 0)
                {
                     return Ok(_taskManager.GetAllTasks(listId));
                }
                return Ok(_taskManager.GetAllTasks());
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        // GET: api/tasks/prioritized?listId=1
        [HttpGet("prioritized")]
        public ActionResult<PrioritizationResult> GetPrioritizedTasks([FromQuery] int listId = 1)
        {
            try
            {
                var result = _taskManager.GetPrioritizedTasks(listId, _timeService);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error prioritizing tasks: {ex.Message}");
            }
        }

        // GET: api/tasks/{id}
        [HttpGet("{id}")]
        public ActionResult<TaskItem> GetTaskById(int id)
        {
            var task = _taskManager.GetTaskById(id);
            if (task == null)
            {
                return NotFound($"Task with ID {id} not found.");
            }
            return Ok(task);
        }

        // POST: api/tasks
        [HttpPost]
        public ActionResult<TaskItem> CreateTask([FromBody] TaskItem newTask)
        {
            if (newTask == null)
            {
                return BadRequest("Task data is null.");
            }

            try
            {
                // Ensure ID is not set by client (or reset it)
                newTask.Id = 0; // TaskManagerService handles ID generation
                
                // Set default list if not provided
                if (newTask.ListId <= 0)
                {
                    newTask.ListId = _taskManager.GetActiveListId();
                }

                _taskManager.AddTask(newTask);
                return CreatedAtAction(nameof(GetTaskById), new { id = newTask.Id }, newTask);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error creating task: {ex.Message}");
            }
        }

        // PUT: api/tasks/{id}
        [HttpPut("{id}")]
        public IActionResult UpdateTask(int id, [FromBody] TaskItem updatedTask)
        {
            if (updatedTask == null || id != updatedTask.Id)
            {
                return BadRequest("Task data is invalid or ID mismatch.");
            }

            try
            {
                var result = _taskManager.UpdateTask(updatedTask);
                if (!result)
                {
                    return NotFound($"Task with ID {id} not found.");
                }
                return NoContent();
            }
            catch (InvalidOperationException ex) // Circular dependency
            {
                return Conflict(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error updating task: {ex.Message}");
            }
        }

        // DELETE: api/tasks/{id}
        [HttpDelete("{id}")]
        public IActionResult DeleteTask(int id)
        {
            try
            {
                var result = _taskManager.DeleteTask(id);
                if (!result)
                {
                    return NotFound($"Task with ID {id} not found.");
                }
                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error deleting task: {ex.Message}");
            }
        }

        // PUT: api/tasks/{id}/complete
        [HttpPut("{id}/complete")]
        public IActionResult MarkComplete(int id)
        {
            var task = _taskManager.GetTaskById(id);
            if (task == null) return NotFound();

            task.IsCompleted = true;
            _taskManager.UpdateTask(task);
            return NoContent();
        }

        // PUT: api/tasks/{id}/uncomplete
        [HttpPut("{id}/uncomplete")]
        public IActionResult MarkUncomplete(int id)
        {
            var task = _taskManager.GetTaskById(id);
            if (task == null) return NotFound();

            task.IsCompleted = false;
            _taskManager.UpdateTask(task);
            return NoContent();
        }
    }
}

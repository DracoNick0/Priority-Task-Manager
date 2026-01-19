using Microsoft.AspNetCore.Mvc;
using PriorityTaskManager.Models;
using PriorityTaskManager.Services;

namespace PriorityTaskManager.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class EventsController : ControllerBase
    {
        private readonly TaskManagerService _taskManager;

        public EventsController(TaskManagerService taskManager)
        {
            _taskManager = taskManager;
        }

        // GET: api/events
        [HttpGet]
        public ActionResult<IEnumerable<Event>> GetAllEvents()
        {
            try
            {
                return Ok(_taskManager.GetAllEvents());
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        // GET: api/events/{id}
        [HttpGet("{id}")]
        public ActionResult<Event> GetEventById(int id)
        {
            var evt = _taskManager.GetEvent(id);
            if (evt == null)
            {
                return NotFound($"Event with ID {id} not found.");
            }
            return Ok(evt);
        }

        // POST: api/events
        [HttpPost]
        public ActionResult<Event> CreateEvent([FromBody] Event newEvent)
        {
            if (newEvent == null)
            {
                return BadRequest("Event data is null.");
            }

            try
            {
                // ID handled by service
                newEvent.Id = 0; 
                _taskManager.AddEvent(newEvent);
                return CreatedAtAction(nameof(GetEventById), new { id = newEvent.Id }, newEvent);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error creating event: {ex.Message}");
            }
        }

        // PUT: api/events/{id}
        [HttpPut("{id}")]
        public IActionResult UpdateEvent(int id, [FromBody] Event updatedEvent)
        {
            if (updatedEvent == null || id != updatedEvent.Id)
            {
                return BadRequest("Event data is invalid or ID mismatch.");
            }

            try
            {
                var result = _taskManager.UpdateEvent(updatedEvent);
                if (!result)
                {
                    return NotFound($"Event with ID {id} not found.");
                }
                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error updating event: {ex.Message}");
            }
        }

        // DELETE: api/events/{id}
        [HttpDelete("{id}")]
        public IActionResult DeleteEvent(int id)
        {
            try
            {
                var result = _taskManager.DeleteEvent(id);
                if (!result)
                {
                    return NotFound($"Event with ID {id} not found.");
                }
                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error deleting event: {ex.Message}");
            }
        }
    }
}

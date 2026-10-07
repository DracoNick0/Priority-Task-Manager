using PriorityTaskManager.API.Tasks;
using PriorityTaskManager.Models;

namespace PriorityTaskManager.Tests.API
{
    public class TaskDtoExtensionsTests
    {
        [Fact]
        public void ToNewTaskItem_MapsLinkFromRequest()
        {
            var request = new TaskRequest(
                "Review proposal",
                "Review notes",
                Guid.NewGuid(),
                5,
                null,
                null,
                TimeSpan.FromHours(1),
                new List<Guid>(),
                false,
                1,
                0,
                null,
                null,
                Link: "https://example.com/reference");

            var task = request.ToNewTaskItem();

            Assert.Equal("https://example.com/reference", task.Link);
            Assert.Equal(task.Link, task.ToResponse().Link);
        }
    }
}

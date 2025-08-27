using PriorityTaskManager.Services;

namespace PriorityTaskManager.CLI.Interfaces
{
    public interface ICommandHandler
    {

        void Execute(TaskManagerService service, string[] args);

    }
}

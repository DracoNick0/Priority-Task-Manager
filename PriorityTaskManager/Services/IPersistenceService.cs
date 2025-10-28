using PriorityTaskManager.Models;

namespace PriorityTaskManager.Services
{
    /// <summary>
    /// Defines methods for loading and saving persistent application data.
    /// </summary>
    public interface IPersistenceService
    {
        DataContainer LoadData();
        void SaveData(DataContainer data);
    }
}

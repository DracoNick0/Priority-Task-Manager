using PriorityTaskManager.Models;

namespace PriorityTaskManager.Services
{
    /// <summary>
    /// Defines storage operations for <see cref="Account"/> records. Kept separate from
    /// <see cref="IPersistenceService"/> because accounts are the tenant boundary itself, not
    /// tenant-scoped task/list/event data; front ends that do not support multiple accounts (e.g. the
    /// single-local-user CLI) do not need an implementation of this interface.
    /// </summary>
    public interface IAccountRepository
    {
        /// <summary>
        /// Finds an account by its normalized email address, or <c>null</c> if none exists.
        /// </summary>
        Account? FindByEmail(string normalizedEmail);

        /// <summary>
        /// Finds an account by its id, or <c>null</c> if none exists.
        /// </summary>
        Account? FindById(Guid id);

        /// <summary>
        /// Persists a newly created account.
        /// </summary>
        void Add(Account account);
    }
}

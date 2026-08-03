namespace PriorityTaskManager.Models
{
    /// <summary>
    /// Represents an authenticated account (MVP email + password model, see docs/ARCHITECTURE_DATA.md).
    /// Accounts are the tenant boundary for the API's persisted data: every task, list, event, and
    /// user profile document is scoped to exactly one account.
    /// </summary>
    public class Account
    {
        /// <summary>
        /// Gets or sets the globally unique identifier for this account, assigned at creation time.
        /// </summary>
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>
        /// Gets or sets the account's normalized (lowercased, trimmed) email address. Used as the
        /// unique login identifier.
        /// </summary>
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the hashed password credential. Never stores a plaintext password; hashing is
        /// performed by <see cref="AccountService"/> using a vetted password hasher.
        /// </summary>
        public string PasswordHash { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the UTC timestamp the account was created.
        /// </summary>
        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    }
}

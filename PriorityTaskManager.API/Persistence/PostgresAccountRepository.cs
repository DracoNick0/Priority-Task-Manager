using Npgsql;
using PriorityTaskManager.Models;
using PriorityTaskManager.Services;

namespace PriorityTaskManager.API.Persistence
{
	/// <summary>
	/// Backs <see cref="IAccountRepository"/> with a dedicated Postgres <c>accounts</c> table, separate
	/// from the <c>persisted_documents</c> table used for tenant-scoped task/list/event data (see
	/// <see cref="PostgresPersistenceService"/>), because accounts are the tenant boundary itself.
	/// </summary>
	public class PostgresAccountRepository : IAccountRepository
	{
		private readonly string _connectionString;

		public PostgresAccountRepository(string connectionString)
		{
			_connectionString = connectionString;
			EnsureSchema();
		}

		/// <summary>
		/// Creates the backing table if it does not already exist. Safe to call on every startup.
		/// </summary>
		private void EnsureSchema()
		{
			using var connection = OpenConnection();
			using var command = connection.CreateCommand();
			command.CommandText = @"
				CREATE TABLE IF NOT EXISTS accounts (
					id uuid PRIMARY KEY,
					email text NOT NULL UNIQUE,
					password_hash text NOT NULL,
					created_at timestamptz NOT NULL DEFAULT now(),
					subscription_tier text NOT NULL DEFAULT 'Free'
				);
				ALTER TABLE accounts ADD COLUMN IF NOT EXISTS subscription_tier text NOT NULL DEFAULT 'Free';";
			command.ExecuteNonQuery();
		}

		private NpgsqlConnection OpenConnection()
		{
			var connection = new NpgsqlConnection(_connectionString);
			connection.Open();
			return connection;
		}

		public Account? FindByEmail(string normalizedEmail)
		{
			using var connection = OpenConnection();
			using var command = connection.CreateCommand();
			command.CommandText = "SELECT id, email, password_hash, created_at, subscription_tier FROM accounts WHERE email = @email;";
			command.Parameters.AddWithValue("email", normalizedEmail);

			using var reader = command.ExecuteReader();
			return reader.Read() ? ReadAccount(reader) : null;
		}

		public Account? FindById(Guid id)
		{
			using var connection = OpenConnection();
			using var command = connection.CreateCommand();
			command.CommandText = "SELECT id, email, password_hash, created_at, subscription_tier FROM accounts WHERE id = @id;";
			command.Parameters.AddWithValue("id", id);

			using var reader = command.ExecuteReader();
			return reader.Read() ? ReadAccount(reader) : null;
		}

		public void Add(Account account)
		{
			using var connection = OpenConnection();
			using var command = connection.CreateCommand();
			command.CommandText = @"
				INSERT INTO accounts (id, email, password_hash, created_at, subscription_tier)
				VALUES (@id, @email, @passwordHash, @createdAt, @subscriptionTier);";
			command.Parameters.AddWithValue("id", account.Id);
			command.Parameters.AddWithValue("email", account.Email);
			command.Parameters.AddWithValue("passwordHash", account.PasswordHash);
			command.Parameters.AddWithValue("createdAt", account.CreatedAtUtc);
			command.Parameters.AddWithValue("subscriptionTier", account.SubscriptionTier.ToString());
			command.ExecuteNonQuery();
		}

		private static Account ReadAccount(NpgsqlDataReader reader) => new()
		{
			Id = reader.GetGuid(0),
			Email = reader.GetString(1),
			PasswordHash = reader.GetString(2),
			CreatedAtUtc = reader.GetDateTime(3),
			SubscriptionTier = Enum.Parse<SubscriptionTier>(reader.GetString(4))
		};
	}
}

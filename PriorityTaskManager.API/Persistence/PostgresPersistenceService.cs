using System.Text.Json;
using Npgsql;
using PriorityTaskManager.Models;
using PriorityTaskManager.Services;

namespace PriorityTaskManager.API.Persistence
{
	/// <summary>
	/// Backs the API's server-side <see cref="DataContainer"/> with Postgres instead of local JSON
	/// files, per issue #31 ("Back persisted state with Postgres instead of local JSON files for the
	/// API's server-side data"). Each front end may still keep a separate local offline cache; that is
	/// out of scope here and tracked by the sync sub-issue.
	///
	/// Mirrors the shape of <see cref="PersistenceService"/>'s per-document JSON files (tasks, lists,
	/// events, user profile), but stores each document as a JSONB row in a single table instead of a
	/// file on disk. Every row is scoped by <c>account_id</c> (issue #35's MVP account model), so each
	/// instance of this class is bound to a single account and only ever reads/writes that account's
	/// documents; the API composes one instance per authenticated request (see
	/// <c>PriorityTaskManager.API/Program.cs</c>) rather than sharing one across all accounts.	/// No client authenticates against this yet - that integration is tracked by issue #44 (V1).	/// </summary>
	public class PostgresPersistenceService : IPersistenceService
	{
		private const string TasksDocumentId = "tasks";
		private const string ListsDocumentId = "lists";
		private const string EventsDocumentId = "events";
		private const string UserProfileDocumentId = "user_profile";
		private const string ArchiveDocumentId = "archive";

		private readonly string _connectionString;
		private readonly Guid _accountId;

		/// <summary>
		/// Creates a persistence service scoped to a single account's documents.
		/// </summary>
		/// <param name="connectionString">The Postgres connection string.</param>
		/// <param name="accountId">The authenticated account whose documents this instance reads/writes.</param>
		public PostgresPersistenceService(string connectionString, Guid accountId)
		{
			_connectionString = connectionString;
			_accountId = accountId;
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
				CREATE TABLE IF NOT EXISTS persisted_documents (
					account_id uuid NOT NULL,
					id text NOT NULL,
					data jsonb NOT NULL,
					updated_at timestamptz NOT NULL DEFAULT now(),
					PRIMARY KEY (account_id, id)
				);";
			command.ExecuteNonQuery();
		}

		private NpgsqlConnection OpenConnection()
		{
			var connection = new NpgsqlConnection(_connectionString);
			connection.Open();
			return connection;
		}

		public DataContainer LoadData()
		{
			var data = new DataContainer();

			using var connection = OpenConnection();

			var tasksDocument = ReadDocument(connection, _accountId, TasksDocumentId);
			if (tasksDocument != null)
			{
				try
				{
					var dict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(tasksDocument);
					if (dict != null && dict.TryGetValue("Tasks", out var tasksElement))
					{
						data.Tasks = JsonSerializer.Deserialize<List<TaskItem>>(tasksElement.GetRawText()) ?? new List<TaskItem>();
					}
					if (dict != null && dict.TryGetValue("NextDisplayId", out var nextIdElement))
					{
						data.NextDisplayId = nextIdElement.GetInt32();
					}
				}
				catch (Exception ex)
				{
					data.LoadWarnings.Add($"Could not load the '{TasksDocumentId}' document from Postgres ({ex.GetType().Name}: {ex.Message}). Tasks were reset to an empty default.");
				}
			}

			var listsDocument = ReadDocument(connection, _accountId, ListsDocumentId);
			if (listsDocument != null)
			{
				try
				{
					var dict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(listsDocument);
					if (dict != null && dict.TryGetValue("Lists", out var listsElement))
					{
						data.Lists = JsonSerializer.Deserialize<List<TaskList>>(listsElement.GetRawText()) ?? new List<TaskList>();
					}
				}
				catch (Exception ex)
				{
					data.LoadWarnings.Add($"Could not load the '{ListsDocumentId}' document from Postgres ({ex.GetType().Name}: {ex.Message}). Lists were reset to an empty default.");
				}
			}

			var eventsDocument = ReadDocument(connection, _accountId, EventsDocumentId);
			if (eventsDocument != null)
			{
				try
				{
					var dict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(eventsDocument);
					if (dict != null && dict.TryGetValue("Events", out var eventsElement))
					{
						data.Events = JsonSerializer.Deserialize<List<Event>>(eventsElement.GetRawText()) ?? new List<Event>();
					}
				}
				catch (Exception ex)
				{
					data.LoadWarnings.Add($"Could not load the '{EventsDocumentId}' document from Postgres ({ex.GetType().Name}: {ex.Message}). Events were reset to an empty default.");
				}
			}

			var userProfileDocument = ReadDocument(connection, _accountId, UserProfileDocumentId);
			if (userProfileDocument != null)
			{
				try
				{
					data.UserProfile = JsonSerializer.Deserialize<UserProfile>(userProfileDocument) ?? new UserProfile();
				}
				catch (Exception ex)
				{
					data.UserProfile = new UserProfile();
					data.LoadWarnings.Add($"Could not load the '{UserProfileDocumentId}' document from Postgres ({ex.GetType().Name}: {ex.Message}). User profile was reset to defaults.");
				}
			}
			else
			{
				data.UserProfile = new UserProfile();
			}

			return data;
		}

		public void SaveData(DataContainer data)
		{
			using var connection = OpenConnection();

			var tasksData = new { Tasks = data.Tasks, NextDisplayId = data.NextDisplayId };
			WriteDocument(connection, _accountId, TasksDocumentId, JsonSerializer.Serialize(tasksData));

			var listsData = new { Lists = data.Lists };
			WriteDocument(connection, _accountId, ListsDocumentId, JsonSerializer.Serialize(listsData));

			var eventsData = new { Events = data.Events };
			WriteDocument(connection, _accountId, EventsDocumentId, JsonSerializer.Serialize(eventsData));

			WriteDocument(connection, _accountId, UserProfileDocumentId, JsonSerializer.Serialize(data.UserProfile));
		}

		public void ArchiveTasks(IEnumerable<TaskItem> tasksToArchive)
		{
			using var connection = OpenConnection();

			var archivedTasks = new List<TaskItem>();
			var archiveDocument = ReadDocument(connection, _accountId, ArchiveDocumentId);
			if (archiveDocument != null)
			{
				archivedTasks = JsonSerializer.Deserialize<List<TaskItem>>(archiveDocument) ?? new List<TaskItem>();
			}

			archivedTasks.AddRange(tasksToArchive);
			WriteDocument(connection, _accountId, ArchiveDocumentId, JsonSerializer.Serialize(archivedTasks));
		}

		private static string? ReadDocument(NpgsqlConnection connection, Guid accountId, string documentId)
		{
			using var command = connection.CreateCommand();
			command.CommandText = "SELECT data FROM persisted_documents WHERE account_id = @accountId AND id = @id;";
			command.Parameters.AddWithValue("accountId", accountId);
			command.Parameters.AddWithValue("id", documentId);

			var result = command.ExecuteScalar();
			return result as string;
		}

		private static void WriteDocument(NpgsqlConnection connection, Guid accountId, string documentId, string json)
		{
			using var command = connection.CreateCommand();
			command.CommandText = @"
				INSERT INTO persisted_documents (account_id, id, data, updated_at)
				VALUES (@accountId, @id, @data::jsonb, now())
				ON CONFLICT (account_id, id) DO UPDATE SET data = @data::jsonb, updated_at = now();";
			command.Parameters.AddWithValue("accountId", accountId);
			command.Parameters.AddWithValue("id", documentId);
			command.Parameters.AddWithValue("data", json);
			command.ExecuteNonQuery();
		}
	}
}

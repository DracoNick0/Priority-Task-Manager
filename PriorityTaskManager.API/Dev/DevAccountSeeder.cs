using PriorityTaskManager.Models;
using PriorityTaskManager.Services;

namespace PriorityTaskManager.API.Dev
{
	/// <summary>
	/// Seeds two fixed, known-credential accounts (one per <see cref="SubscriptionTier"/>) into the dev
	/// database on startup, so a developer can log in as either tier without registering an account by
	/// hand. Only ever called when <c>IsDevelopment()</c> (see <c>Program.cs</c>); never runs against a
	/// real deployment. Idempotent: safe to call on every startup, leaves an already-seeded account as-is.
	/// </summary>
	public static class DevAccountSeeder
	{
		public const string FreeAccountEmail = "free@dev.local";
		public const string SubscriptionAccountEmail = "subscriber@dev.local";
		public const string DevAccountPassword = "DevPassword123!";

		public static void Seed(AccountService accountService)
		{
			TrySeed(accountService, FreeAccountEmail, SubscriptionTier.Free);
			TrySeed(accountService, SubscriptionAccountEmail, SubscriptionTier.Subscription);
		}

		private static void TrySeed(AccountService accountService, string email, SubscriptionTier tier)
		{
			try
			{
				accountService.Register(email, DevAccountPassword, tier);
			}
			catch (InvalidOperationException)
			{
				// Already seeded from a previous run; leave the existing account as-is.
			}
		}
	}
}

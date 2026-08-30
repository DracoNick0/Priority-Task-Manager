namespace PriorityTaskManager.API.Auth
{
	/// <summary>
	/// JWT claim type and authorization policy name used to gate Subscription-only endpoints (online
	/// scheduling, cross-device sync) behind the authenticated account's membership tier. See
	/// docs/ARCHITECTURE_INTEGRATIONS.md's "Planned Direction" section.
	/// </summary>
	public static class SubscriptionPolicy
	{
		/// <summary>JWT claim type carrying the account's <see cref="PriorityTaskManager.Models.SubscriptionTier"/> name.</summary>
		public const string ClaimType = "subscription_tier";

		/// <summary>Authorization policy name requiring <see cref="PriorityTaskManager.Models.SubscriptionTier.Subscription"/>.</summary>
		public const string PolicyName = "SubscriptionRequired";
	}
}

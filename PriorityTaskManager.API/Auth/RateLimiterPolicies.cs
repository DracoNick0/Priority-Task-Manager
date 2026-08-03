namespace PriorityTaskManager.API.Auth
{
	/// <summary>Names of rate limiting policies configured in <c>Program.cs</c>.</summary>
	public static class RateLimiterPolicies
	{
		/// <summary>Applied to login/register endpoints to blunt credential-stuffing/brute-force attempts.</summary>
		public const string Auth = "auth";
	}
}

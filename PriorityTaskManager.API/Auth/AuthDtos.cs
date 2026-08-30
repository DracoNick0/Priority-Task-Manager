namespace PriorityTaskManager.API.Auth
{
	/// <summary>Request body for <c>POST /api/auth/register</c>.</summary>
	public record RegisterRequest(string Email, string Password);

	/// <summary>Request body for <c>POST /api/auth/login</c>.</summary>
	public record LoginRequest(string? Email, string? Password);

	/// <summary>
	/// Response body for successful registration/login, carrying the bearer token to send as
	/// <c>Authorization: Bearer &lt;token&gt;</c>. <paramref name="BetaGracePeriodNotice"/> is non-null only
	/// while issue #50's beta grace period is active, so clients can surface an honest "this full access is
	/// a temporary free beta preview" message (see docs/VISION.md).
	/// </summary>
	public record AuthResponse(string Token, DateTime ExpiresAtUtc, string? BetaGracePeriodNotice = null);
}

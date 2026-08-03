namespace PriorityTaskManager.API.Auth
{
	/// <summary>Request body for <c>POST /api/auth/register</c>.</summary>
	public record RegisterRequest(string Email, string Password);

	/// <summary>Request body for <c>POST /api/auth/login</c>.</summary>
	public record LoginRequest(string? Email, string? Password);

	/// <summary>Response body for successful registration/login, carrying the bearer token to send as <c>Authorization: Bearer &lt;token&gt;</c>.</summary>
	public record AuthResponse(string Token, DateTime ExpiresAtUtc);
}

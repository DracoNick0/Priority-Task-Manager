using PriorityTaskManager.Services;

namespace PriorityTaskManager.API.Auth
{
	/// <summary>
	/// Maps the account registration/login endpoints (issue #35's MVP account model).
	/// </summary>
	public static class AuthEndpoints
	{
		private const int MinimumPasswordLength = 8;

		public static void MapAuthEndpoints(this WebApplication app)
		{
			app.MapPost("/api/auth/register", (RegisterRequest request, AccountService accountService, JwtTokenService tokenService) =>
			{
				if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
				{
					return Results.BadRequest(new { error = "Email and password are required." });
				}
				if (request.Password.Length < MinimumPasswordLength)
				{
					return Results.BadRequest(new { error = $"Password must be at least {MinimumPasswordLength} characters." });
				}

				try
				{
					var account = accountService.Register(request.Email, request.Password);
					var (token, expiresAtUtc) = tokenService.CreateToken(account);
					return Results.Ok(new AuthResponse(token, expiresAtUtc));
				}
				catch (InvalidOperationException ex)
				{
					return Results.Conflict(new { error = ex.Message });
				}
			}).RequireRateLimiting(RateLimiterPolicies.Auth);

			app.MapPost("/api/auth/login", (LoginRequest request, AccountService accountService, JwtTokenService tokenService) =>
			{
				var account = accountService.ValidateCredentials(request.Email ?? string.Empty, request.Password ?? string.Empty);
				if (account == null)
				{
					// Deliberately generic (no "unknown email" vs. "wrong password" distinction) to avoid
					// leaking which emails are registered.
					return Results.Unauthorized();
				}

				var (token, expiresAtUtc) = tokenService.CreateToken(account);
				return Results.Ok(new AuthResponse(token, expiresAtUtc));
			}).RequireRateLimiting(RateLimiterPolicies.Auth);
		}
	}
}

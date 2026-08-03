using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using PriorityTaskManager.Models;

namespace PriorityTaskManager.API.Auth
{
	/// <summary>
	/// JWT signing/validation configuration read from configuration (see <c>Jwt:*</c> settings).
	/// </summary>
	public class JwtOptions
	{
		public required string Key { get; init; }
		public required string Issuer { get; init; }
		public required string Audience { get; init; }
		public int ExpiryMinutes { get; init; } = 60;
	}

	/// <summary>
	/// Issues short-lived JWT bearer tokens for authenticated accounts (issue #35's MVP account model).
	/// The token's <c>sub</c>/<see cref="ClaimTypes.NameIdentifier"/> claim carries the account id that
	/// <c>PriorityTaskManager.API/Program.cs</c> uses to build per-account scoped core services.
	/// </summary>
	public class JwtTokenService
	{
		private readonly JwtOptions _options;

		public JwtTokenService(JwtOptions options)
		{
			_options = options;
		}

		/// <summary>
		/// Creates a signed JWT for the given account and returns it alongside its UTC expiry.
		/// </summary>
		public (string Token, DateTime ExpiresAtUtc) CreateToken(Account account)
		{
			var expiresAtUtc = DateTime.UtcNow.AddMinutes(_options.ExpiryMinutes);

			var claims = new[]
			{
				new Claim(JwtRegisteredClaimNames.Sub, account.Id.ToString()),
				new Claim(ClaimTypes.NameIdentifier, account.Id.ToString()),
				new Claim(ClaimTypes.Email, account.Email),
				new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
			};

			var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.Key));
			var signingCredentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

			var token = new JwtSecurityToken(
				issuer: _options.Issuer,
				audience: _options.Audience,
				claims: claims,
				expires: expiresAtUtc,
				signingCredentials: signingCredentials);

			return (new JwtSecurityTokenHandler().WriteToken(token), expiresAtUtc);
		}
	}
}

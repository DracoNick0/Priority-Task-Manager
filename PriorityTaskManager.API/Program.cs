using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using PriorityTaskManager.API.Auth;
using PriorityTaskManager.API.Events;
using PriorityTaskManager.API.Lists;
using PriorityTaskManager.API.Persistence;
using PriorityTaskManager.API.Tasks;
using PriorityTaskManager.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHttpContextAccessor();

var connectionString = builder.Configuration.GetConnectionString("Postgres")
	?? throw new InvalidOperationException("Missing required 'ConnectionStrings:Postgres' configuration value.");

// Server-side only so far; no client logs in with this yet (client integration is issue #44, V1).
var jwtOptions = new JwtOptions
{
	Key = builder.Configuration["Jwt:Key"]
		?? throw new InvalidOperationException("Missing required 'Jwt:Key' configuration value."),
	Issuer = builder.Configuration["Jwt:Issuer"] ?? "PriorityTaskManager",
	Audience = builder.Configuration["Jwt:Audience"] ?? "PriorityTaskManagerClients",
	ExpiryMinutes = int.TryParse(builder.Configuration["Jwt:ExpiryMinutes"], out var expiryMinutes) ? expiryMinutes : 60
};
builder.Services.AddSingleton(jwtOptions);
builder.Services.AddSingleton<JwtTokenService>();

builder.Services.AddSingleton<IAccountRepository>(new PostgresAccountRepository(connectionString));
builder.Services.AddSingleton<AccountService>();

// Persisted task/list/event data is scoped per authenticated account (issue #35), so the core service
// graph (TaskManagerService, IPersistenceService, etc.) is built per request instead of once at
// startup, using the account id resolved from the request's JWT claims.
builder.Services.AddScoped(sp =>
{
	var httpContextAccessor = sp.GetRequiredService<IHttpContextAccessor>();
	var accountIdClaim = httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier)
		?? throw new InvalidOperationException("Cannot build account-scoped services without an authenticated account.");

	IPersistenceService persistenceService = new PostgresPersistenceService(connectionString, Guid.Parse(accountIdClaim));
	return ServiceComposer.Compose(persistenceService);
});
builder.Services.AddScoped(sp => sp.GetRequiredService<ComposedServices>().TaskManagerService);
builder.Services.AddScoped(sp => sp.GetRequiredService<ComposedServices>().TaskMetricsService);
builder.Services.AddScoped(sp => sp.GetRequiredService<ComposedServices>().TimeService);
builder.Services.AddScoped(sp => sp.GetRequiredService<ComposedServices>().PersistenceService);

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
	.AddJwtBearer(options =>
	{
		options.TokenValidationParameters = new TokenValidationParameters
		{
			ValidateIssuer = true,
			ValidIssuer = jwtOptions.Issuer,
			ValidateAudience = true,
			ValidAudience = jwtOptions.Audience,
			ValidateIssuerSigningKey = true,
			IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.Key)),
			ValidateLifetime = true,
			ClockSkew = TimeSpan.FromSeconds(30)
		};
	});
builder.Services.AddAuthorization();

// Blunt brute-force/credential-stuffing attempts against login/register (baseline security hygiene).
builder.Services.AddRateLimiter(options =>
{
	options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
	options.AddFixedWindowLimiter(RateLimiterPolicies.Auth, limiterOptions =>
	{
		limiterOptions.PermitLimit = 5;
		limiterOptions.Window = TimeSpan.FromMinutes(1);
		limiterOptions.QueueLimit = 0;
	});
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapAuthEndpoints();
app.MapAccountEndpoints();
app.MapTaskEndpoints();
app.MapListEndpoints();
app.MapEventEndpoints();

app.Run();


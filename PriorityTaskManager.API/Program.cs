using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using PriorityTaskManager.API.Auth;
using PriorityTaskManager.API.Dev;
using PriorityTaskManager.API.Events;
using PriorityTaskManager.API.Local;
using PriorityTaskManager.API.Lists;
using PriorityTaskManager.API.Persistence;
using PriorityTaskManager.API.Schedule;
using PriorityTaskManager.API.Tasks;
using PriorityTaskManager.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHttpContextAccessor();

// Serialize enums (e.g. SchedulingMode, DayOfWeek) as their readable names, not raw numbers,
// for request/response bodies across all endpoints including the local schedule-compute route.
builder.Services.ConfigureHttpJsonOptions(options =>
{
	options.SerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
});

// The cloud/multi-tenant routes (auth, accounts, tasks/lists/events) need Postgres; the unauthenticated
// local schedule-compute route (docs/VISION.md MVP scope) does not. A fully offline client (e.g. the
// Flutter app's local sidecar) launches this process with LocalOnly=true so it never attempts cloud
// wiring even though appsettings.json ships a default (dev-convenience) Postgres connection string.
var connectionString = builder.Configuration.GetConnectionString("Postgres");
var localOnly = builder.Configuration.GetValue<bool>("LocalOnly");
var cloudModeEnabled = !localOnly && !string.IsNullOrWhiteSpace(connectionString);

// Issue #50's MVP/beta grace period flag: while true, real self-registration (POST /api/auth/register)
// grants Subscription tier instead of Free, since no real payment integration exists yet. Flip to
// false in config at V1 once real payment/entitlement enforcement lands.
var betaGracePeriodEnabled = builder.Configuration.GetValue<bool>("BetaGracePeriod:DefaultNewAccountsToSubscription");

if (cloudModeEnabled)
{
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

	builder.Services.AddSingleton<IAccountRepository>(new PostgresAccountRepository(connectionString!));
	builder.Services.AddSingleton(sp => new AccountService(sp.GetRequiredService<IAccountRepository>(), betaGracePeriodEnabled));

	// Persisted task/list/event data is scoped per authenticated account (issue #35), so the core service
	// graph (TaskManagerService, IPersistenceService, etc.) is built per request instead of once at
	// startup, using the account id resolved from the request's JWT claims.
	builder.Services.AddScoped(sp =>
	{
		var httpContextAccessor = sp.GetRequiredService<IHttpContextAccessor>();
		var accountIdClaim = httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier)
			?? throw new InvalidOperationException("Cannot build account-scoped services without an authenticated account.");

		IPersistenceService persistenceService = new PostgresPersistenceService(connectionString!, Guid.Parse(accountIdClaim));
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
	builder.Services.AddAuthorization(options =>
	{
		// Gates online scheduling (and, later, sync) behind the Subscription tier (docs/VISION.md).
		options.AddPolicy(SubscriptionPolicy.PolicyName, policy =>
			policy.RequireClaim(SubscriptionPolicy.ClaimType, PriorityTaskManager.Models.SubscriptionTier.Subscription.ToString()));
	});

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
}

var app = builder.Build();

// Dev-only convenience: seed fixed, known-credential Free/Subscription test accounts so a developer can
// log in as either tier without registering by hand (see PriorityTaskManager.API/Dev/DevAccountSeeder.cs).
// Never runs outside Development, and is a no-op unless Postgres/cloud mode is configured.
if (cloudModeEnabled && app.Environment.IsDevelopment())
{
	DevAccountSeeder.Seed(app.Services.GetRequiredService<AccountService>());
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

if (cloudModeEnabled)
{
	app.UseRateLimiter();
	app.UseAuthentication();
	app.UseAuthorization();

	app.MapAuthEndpoints(betaGracePeriodEnabled);
	app.MapAccountEndpoints();
	app.MapTaskEndpoints();
	app.MapListEndpoints();
	app.MapEventEndpoints();
	app.MapScheduleEndpoints();
}

// Always available, even without Postgres configured: unauthenticated, stateless schedule compute for
// fully offline/local clients (see PriorityTaskManager.API/Local/LocalScheduleEndpoints.cs).
app.MapLocalScheduleEndpoints();

app.Run();


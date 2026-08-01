using PriorityTaskManager.API.Persistence;
using PriorityTaskManager.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Back the API's server-side data with Postgres instead of local JSON files (docs issue #31).
// Each client may still keep its own local offline cache separately; that is handled by the sync sub-issue.
var connectionString = builder.Configuration.GetConnectionString("Postgres")
	?? throw new InvalidOperationException("Missing required 'ConnectionStrings:Postgres' configuration value.");
IPersistenceService persistenceService = new PostgresPersistenceService(connectionString);

// Build the shared core service graph (TaskManagerService, IPersistenceService, ITimeService, etc.)
// the same way the CLI does, per docs/ARCHITECTURE_INTEGRATIONS.md.
var composedServices = ServiceComposer.Compose(persistenceService);
builder.Services.AddSingleton(composedServices);
builder.Services.AddSingleton(composedServices.TaskManagerService);
builder.Services.AddSingleton(composedServices.TaskMetricsService);
builder.Services.AddSingleton(composedServices.TimeService);
builder.Services.AddSingleton(composedServices.PersistenceService);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.Run();

using PriorityTaskManager.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Build the shared core service graph (TaskManagerService, IPersistenceService, ITimeService, etc.)
// the same way the CLI does, per docs/ARCHITECTURE_INTEGRATIONS.md.
var dataDirectory = Path.Combine(AppContext.BaseDirectory, "Data");
var composedServices = ServiceComposer.Compose(dataDirectory);
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

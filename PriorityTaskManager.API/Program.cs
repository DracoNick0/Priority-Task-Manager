using PriorityTaskManager.Services;
using PriorityTaskManager.Models;
using PriorityTaskManager.MCP;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

// 1. Time Service
builder.Services.AddSingleton<ITimeService, TimeService>();

// 2. Persistence Service
// Ensure Data directory exists relative to the executable
var dataDirectory = Path.Combine(AppContext.BaseDirectory, "Data");
if (!Directory.Exists(dataDirectory))
{
    Directory.CreateDirectory(dataDirectory);
}

builder.Services.AddSingleton<IPersistenceService>(sp => new PersistenceService(dataDirectory));

// 3. DataContainer (Load once on startup)
builder.Services.AddSingleton<DataContainer>(sp => {
    var persistence = sp.GetRequiredService<IPersistenceService>();
    return persistence.LoadData();
});

// 4. Urgency Strategy
// Note: This injects the initial UserProfile and Events references. 
// If these references are swapped out entirely in DataContainer, this strategy instance might become stale.
// However, this matches the current CLI implementation.
builder.Services.AddSingleton<IUrgencyStrategy>(sp => {
    var data = sp.GetRequiredService<DataContainer>();
    var time = sp.GetRequiredService<ITimeService>();
    return new MultiAgentUrgencyStrategy(data.UserProfile, data.Events, time);
});

// 5. TaskManagerService
builder.Services.AddSingleton<TaskManagerService>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();

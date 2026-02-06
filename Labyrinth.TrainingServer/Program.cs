using Labyrinth.TrainingServer.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = null; // Keep original casing
    });

// Register the labyrinth service as singleton (shared state)
builder.Services.AddSingleton<LabyrinthService>();

// Add OpenAPI (built-in .NET 10)
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapControllers();

// Log startup
app.Logger.LogInformation("Labyrinth Training Server started on {Urls}", string.Join(", ", app.Urls));

app.Run();

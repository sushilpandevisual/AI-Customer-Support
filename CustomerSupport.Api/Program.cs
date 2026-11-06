using Npgsql;
var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
// Read connection string from Codespaces secret
var connectionString =
    builder.Configuration["SUPABASE_DB_CONNECTION"]
    ?? throw new InvalidOperationException(
        "SUPABASE_DB_CONNECTION is missing.");

// Register NpgsqlDataSource with dependency injection
builder.Services.AddSingleton(
    new NpgsqlDataSourceBuilder(connectionString).Build());

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

//app.UseHttpsRedirection();

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/health", () => Results.Ok(new
{
    status = "Healthy"
}));

app.MapGet("/weatherforecast", () =>
{
    var forecast =  Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
})
.WithName("GetWeatherForecast");

// Development-only database check
if (app.Environment.IsDevelopment())
{
    app.MapGet("/api/dev/database-check",
        async (NpgsqlDataSource dataSource) =>
    {
        await using var connection =
            await dataSource.OpenConnectionAsync();

        await using var command =
            new NpgsqlCommand(
                "SELECT COUNT(*) FROM public.support_tickets",
                connection);

        var count = Convert.ToInt64(
            await command.ExecuteScalarAsync());

        return Results.Ok(new
        {
            databaseConnected = true,
            ticketCount = count
        });
    });
}

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}

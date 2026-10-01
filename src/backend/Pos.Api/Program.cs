using Npgsql;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("Postgres");
if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException("Connection string 'Postgres' is not configured.");
}

builder.Services.AddSingleton(_ => NpgsqlDataSource.Create(connectionString));

var app = builder.Build();

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

app.MapGet("/health/db", async (
    NpgsqlDataSource dataSource,
    CancellationToken cancellationToken) =>
{
    try
    {
        await using var command = dataSource.CreateCommand("SELECT 1");
        await command.ExecuteScalarAsync(cancellationToken);

        return Results.Ok(new { status = "ok" });
    }
    catch (NpgsqlException)
    {
        return Results.Json(
            new { status = "unavailable" },
            statusCode: StatusCodes.Status503ServiceUnavailable);
    }
});

app.Run();

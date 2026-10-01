using Npgsql;

const string RequestIdHeaderName = "X-Request-Id";

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("Postgres");
if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException("Connection string 'Postgres' is not configured.");
}

builder.Services.AddSingleton(_ => NpgsqlDataSource.Create(connectionString));

var app = builder.Build();

app.Use(async (context, next) =>
{
    var hasValidRequestId =
        context.Request.Headers.TryGetValue(RequestIdHeaderName, out var requestIdValues) &&
        requestIdValues.Count == 1 &&
        IsValidRequestId(requestIdValues[0]);

    var requestId = hasValidRequestId
        ? requestIdValues[0]!
        : Guid.NewGuid().ToString("D");

    context.TraceIdentifier = requestId;
    context.Response.Headers[RequestIdHeaderName] = requestId;

    await next(context);
});

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

static bool IsValidRequestId(string? value)
{
    if (value is null or { Length: < 1 or > 128 })
    {
        return false;
    }

    foreach (var character in value)
    {
        if (character is < '!' or > '~')
        {
            return false;
        }
    }

    return true;
}

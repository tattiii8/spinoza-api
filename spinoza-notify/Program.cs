using Npgsql;
using Spinoza.Data;
using Spinoza.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen();

var connectionString =
    builder.Configuration.GetConnectionString(
        "DefaultConnection")
    ?? "Host=localhost;Port=5432;Database=spinoza;Username=postgres;Password=postgres";

builder.Services.AddSingleton(
    new DbConnectionFactory(connectionString));

builder.Services.AddSingleton<EmailDeliveryService>();

builder.Services.AddSingleton<EventDispatcher>();

builder.Services.AddHostedService(sp =>
    sp.GetRequiredService<EventDispatcher>());

var app = builder.Build();

app.UseSwagger();

app.UseSwaggerUI(options =>
{
    options.RoutePrefix =
        "api/notify/swagger";

    options.SwaggerEndpoint(
        "/swagger/v1/swagger.json",
        "Spinoza API v1");
});

// Intentionally NO authentication/authorization middleware.
app.MapControllers();

app.MapGet(
    "/health",
    () => Results.Ok(
        new
        {
            status = "ok",
            service = "spinoza"
        }));

await DatabaseInitializer.InitializeAsync(
    app.Services);

app.Run();
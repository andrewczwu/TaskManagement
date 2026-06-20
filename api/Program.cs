var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// Endpoints, auth, and data access are wired up in later commits.
app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

app.Run();

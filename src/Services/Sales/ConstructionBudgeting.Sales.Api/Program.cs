var builder = WebApplication.CreateBuilder(args);

builder.Services.AddProblemDetails();
builder.Services.AddHealthChecks();

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();

// Liveness only: dependency readiness is added alongside persistence and messaging.
app.MapHealthChecks("/health");

app.Run();

public partial class Program { }

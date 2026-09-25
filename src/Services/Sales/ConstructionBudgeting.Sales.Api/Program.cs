var builder = WebApplication.CreateBuilder(args);

builder.Services.AddProblemDetails();
builder.Services.AddHealthChecks();

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();

// 当前仅检查进程存活；数据库和消息服务的就绪检查随相应功能接入。
app.MapHealthChecks("/health");

app.Run();

public partial class Program { }

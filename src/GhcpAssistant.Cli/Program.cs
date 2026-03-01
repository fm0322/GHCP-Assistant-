using GhcpAssistant.Channels;
using GhcpAssistant.Core.Channels;
using GhcpAssistant.Core.History;
using GhcpAssistant.Core.Sessions;
using GhcpAssistant.Data;
using GhcpAssistant.Sdk;
using GhcpAssistant.Tools;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = Host.CreateApplicationBuilder(args);

// Bind configuration
var sessionOptions = builder.Configuration.GetSection("Session").Get<SessionOptions>() ?? new SessionOptions();
var allowedCommands = builder.Configuration.GetSection("Shell:AllowedCommands").Get<string[]>() ?? ["dotnet", "git", "ls"];
var haBaseUrl = builder.Configuration["HomeAssistant:BaseUrl"] ?? "";
var haAccessToken = builder.Configuration["HomeAssistant:AccessToken"] ?? "";

// Register services
builder.Services.AddSingleton(sessionOptions);
builder.Services.AddSingleton<IInputChannel, ConsoleInputChannel>();
builder.Services.AddSingleton<ICopilotClientFactory, CopilotSdkClientFactory>();
builder.Services.AddSingleton(sp =>
{
    var registry = new ToolRegistry();
    registry.Register(new FileSystemTool(Directory.GetCurrentDirectory()));
    registry.Register(new ShellTool(allowedCommands));
    registry.Register(new GitTool(Directory.GetCurrentDirectory()));
    registry.Register(new WebSearchTool());
    registry.Register(new GitHubTool());
    if (!string.IsNullOrWhiteSpace(haBaseUrl))
        registry.Register(new HomeAssistantTool(new HttpClient(), haBaseUrl, haAccessToken));
    return registry;
});
builder.Services.AddScoped<SessionManager>();

// Register persistent conversation history (SQLite via EF Core)
var connectionString = builder.Configuration.GetConnectionString("AssistantDb")
    ?? "Data Source=ghcpassistant.db";
builder.Services.AddDbContext<AssistantDbContext>(options =>
    options.UseSqlite(connectionString));
builder.Services.AddScoped<IConversationHistoryService, SqliteConversationHistoryService>();

var host = builder.Build();

// Ensure the database is created and run the assistant
Console.WriteLine("GHCP Assistant started. Type 'exit' to quit.\n");

using (var scope = host.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AssistantDbContext>();
    await db.Database.EnsureCreatedAsync();

    var sessionManager = scope.ServiceProvider.GetRequiredService<SessionManager>();
    await sessionManager.RunAsync();
}

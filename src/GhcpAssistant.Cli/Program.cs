using GhcpAssistant.Channels;
using GhcpAssistant.Core.Channels;
using GhcpAssistant.Core.Sessions;
using GhcpAssistant.Sdk;
using GhcpAssistant.Tools;
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
builder.Services.AddSingleton<SessionManager>();

var host = builder.Build();

// Run the assistant
Console.WriteLine("GHCP Assistant started. Type 'exit' to quit.\n");

var sessionManager = host.Services.GetRequiredService<SessionManager>();
await sessionManager.RunAsync();

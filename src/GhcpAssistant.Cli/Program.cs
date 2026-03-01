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

// Register services
builder.Services.AddSingleton(sessionOptions);
builder.Services.AddSingleton<IInputChannel, ConsoleInputChannel>();
builder.Services.AddSingleton(sp =>
{
    var registry = new ToolRegistry();
    registry.Register(new FileSystemTool(Directory.GetCurrentDirectory()));
    registry.Register(new ShellTool(allowedCommands));
    registry.Register(new GitTool(Directory.GetCurrentDirectory()));
    registry.Register(new WebSearchTool());
    registry.Register(new GitHubTool());
    return registry;
});

var host = builder.Build();

// Run the assistant
Console.WriteLine("GHCP Assistant started. Type 'exit' to quit.\n");

var inputChannel = host.Services.GetRequiredService<IInputChannel>();
var toolRegistry = host.Services.GetRequiredService<ToolRegistry>();
var options = host.Services.GetRequiredService<SessionOptions>();

// Note: CopilotClientFactory is not yet implemented with the real SDK.
// For now, print a message. Replace with SessionManager.RunAsync() once Phase 4 SDK integration is complete.
Console.WriteLine("⚠ CopilotClient integration pending. Tools are registered and ready:");
foreach (var tool in toolRegistry.GetRegisteredTools())
{
    Console.WriteLine($"  • {tool.Name}: {tool.Description}");
}
Console.WriteLine("\nWaiting for GitHub.Copilot.SDK availability to enable full agent loop.");

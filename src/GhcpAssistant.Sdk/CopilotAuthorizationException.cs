namespace GhcpAssistant.Sdk;

/// <summary>
/// Thrown when the Copilot SDK reports an authorization failure.
/// The user should run <c>/login</c> to authenticate before retrying.
/// </summary>
public sealed class CopilotAuthorizationException : Exception
{
    public CopilotAuthorizationException(string message) : base(message) { }
}

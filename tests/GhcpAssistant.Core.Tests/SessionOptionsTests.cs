using GhcpAssistant.Core.Sessions;

namespace GhcpAssistant.Core.Tests;

public class SessionOptionsTests
{
    [Fact]
    public void DefaultModel_ShouldBeGpt4o()
    {
        var options = new SessionOptions();
        Assert.Equal("gpt-4o", options.Model);
    }

    [Fact]
    public void DefaultMaxTurns_ShouldBe50()
    {
        var options = new SessionOptions();
        Assert.Equal(50, options.MaxTurns);
    }

    [Fact]
    public void DefaultSystemPrompt_ShouldBeNull()
    {
        var options = new SessionOptions();
        Assert.Null(options.SystemPrompt);
    }
}

using System.Net;
using System.Text.Json;

namespace GhcpAssistant.Tools.Tests;

public class HomeAssistantToolTests
{
    private static HomeAssistantTool CreateTool(HttpClient httpClient) =>
        new(httpClient, "http://localhost:8123", "test-token");

    [Fact]
    public void Name_IsHomeAssistant()
    {
        var tool = CreateTool(new HttpClient());
        Assert.Equal("home_assistant", tool.Name);
    }

    [Fact]
    public void Description_IsNotEmpty()
    {
        var tool = CreateTool(new HttpClient());
        Assert.False(string.IsNullOrWhiteSpace(tool.Description));
    }

    [Fact]
    public void Constructor_ThrowsOnNullHttpClient()
    {
        Assert.Throws<ArgumentNullException>(() => new HomeAssistantTool(null!, "http://localhost:8123", "token"));
    }

    [Fact]
    public void Constructor_ThrowsOnEmptyBaseUrl()
    {
        Assert.Throws<ArgumentException>(() => new HomeAssistantTool(new HttpClient(), "", "token"));
    }

    [Fact]
    public async Task UnknownAction_ThrowsArgumentException()
    {
        var tool = CreateTool(new HttpClient());
        var parameters = JsonDocument.Parse("""{"action":"unknown"}""").RootElement;

        await Assert.ThrowsAsync<ArgumentException>(
            () => tool.ExecuteAsync(parameters, CancellationToken.None));
    }

    [Fact]
    public async Task GetStates_ReturnsEntityList()
    {
        var handler = new FakeHandler(HttpStatusCode.OK,
            """[{"entity_id":"light.living_room","state":"on","attributes":{"friendly_name":"Living Room"}}]""");
        var tool = CreateTool(new HttpClient(handler));

        var parameters = JsonDocument.Parse("""{"action":"get_states"}""").RootElement;
        var result = await tool.ExecuteAsync(parameters, CancellationToken.None);

        Assert.Contains("light.living_room", result);
        Assert.Contains("on", result);
        Assert.Contains("Living Room", result);
    }

    [Fact]
    public async Task GetState_ReturnsSingleEntity()
    {
        var handler = new FakeHandler(HttpStatusCode.OK,
            """{"entity_id":"switch.garage","state":"off","attributes":{"friendly_name":"Garage Switch"}}""");
        var tool = CreateTool(new HttpClient(handler));

        var parameters = JsonDocument.Parse("""{"action":"get_state","entity_id":"switch.garage"}""").RootElement;
        var result = await tool.ExecuteAsync(parameters, CancellationToken.None);

        Assert.Contains("switch.garage", result);
        Assert.Contains("off", result);
        Assert.Contains("Garage Switch", result);
    }

    [Fact]
    public async Task CallService_ReturnsSuccessMessage()
    {
        var handler = new FakeHandler(HttpStatusCode.OK, "[]");
        var tool = CreateTool(new HttpClient(handler));

        var parameters = JsonDocument.Parse(
            """{"action":"call_service","domain":"light","service":"turn_on","service_data":{"entity_id":"light.living_room"}}""")
            .RootElement;
        var result = await tool.ExecuteAsync(parameters, CancellationToken.None);

        Assert.Contains("light.turn_on", result);
        Assert.Contains("successfully", result);
    }

    [Fact]
    public async Task CallService_WithoutServiceData_Succeeds()
    {
        var handler = new FakeHandler(HttpStatusCode.OK, "[]");
        var tool = CreateTool(new HttpClient(handler));

        var parameters = JsonDocument.Parse(
            """{"action":"call_service","domain":"switch","service":"toggle"}""")
            .RootElement;
        var result = await tool.ExecuteAsync(parameters, CancellationToken.None);

        Assert.Contains("switch.toggle", result);
        Assert.Contains("successfully", result);
    }

    [Fact]
    public async Task GetServices_ReturnsDomainList()
    {
        var handler = new FakeHandler(HttpStatusCode.OK,
            """[{"domain":"light","services":{"turn_on":{},"turn_off":{}}}]""");
        var tool = CreateTool(new HttpClient(handler));

        var parameters = JsonDocument.Parse("""{"action":"get_services"}""").RootElement;
        var result = await tool.ExecuteAsync(parameters, CancellationToken.None);

        Assert.Contains("light", result);
        Assert.Contains("turn_on", result);
        Assert.Contains("turn_off", result);
    }

    /// <summary>Minimal HttpMessageHandler stub for unit tests.</summary>
    private sealed class FakeHandler(HttpStatusCode statusCode, string content) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var response = new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(content, System.Text.Encoding.UTF8, "application/json")
            };
            return Task.FromResult(response);
        }
    }
}

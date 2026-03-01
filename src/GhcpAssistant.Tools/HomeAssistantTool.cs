using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using GhcpAssistant.Core.Tools;

namespace GhcpAssistant.Tools;

public sealed class HomeAssistantTool : IAssistantTool
{
    private readonly HttpClient _httpClient;

    public HomeAssistantTool(HttpClient httpClient, string baseUrl, string accessToken)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));

        if (string.IsNullOrWhiteSpace(baseUrl))
            throw new ArgumentException("Home Assistant base URL is required.", nameof(baseUrl));

        _httpClient.BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/api/");
        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", accessToken);
    }

    public string Name => "home_assistant";
    public string Description =>
        "Interact with Home Assistant: get entity states, call services (e.g., turn on/off lights, locks, switches).";

    public async Task<string> ExecuteAsync(JsonElement parameters, CancellationToken ct)
    {
        var action = parameters.GetProperty("action").GetString()!;

        return action.ToLowerInvariant() switch
        {
            "get_states" => await GetStatesAsync(ct),
            "get_state" => await GetStateAsync(
                parameters.GetProperty("entity_id").GetString()!, ct),
            "call_service" => await CallServiceAsync(
                parameters.GetProperty("domain").GetString()!,
                parameters.GetProperty("service").GetString()!,
                parameters.TryGetProperty("service_data", out var data) ? data : default,
                ct),
            "get_services" => await GetServicesAsync(ct),
            _ => throw new ArgumentException($"Unknown Home Assistant action '{action}'.")
        };
    }

    private async Task<string> GetStatesAsync(CancellationToken ct)
    {
        var response = await _httpClient.GetAsync("states", ct);
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync(ct);

        using var doc = JsonDocument.Parse(json);
        var entities = doc.RootElement.EnumerateArray()
            .Take(50)
            .Select(e =>
            {
                var entityId = e.GetProperty("entity_id").GetString();
                var state = e.GetProperty("state").GetString();
                var friendlyName = e.TryGetProperty("attributes", out var attrs) &&
                                   attrs.TryGetProperty("friendly_name", out var fn)
                    ? fn.GetString()
                    : entityId;
                return $"{entityId}: {state} ({friendlyName})";
            });

        return string.Join('\n', entities);
    }

    private async Task<string> GetStateAsync(string entityId, CancellationToken ct)
    {
        var response = await _httpClient.GetAsync($"states/{entityId}", ct);
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync(ct);

        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        var state = root.GetProperty("state").GetString();
        var friendlyName = root.TryGetProperty("attributes", out var attrs) &&
                           attrs.TryGetProperty("friendly_name", out var fn)
            ? fn.GetString()
            : entityId;

        return $"Entity: {entityId}\nState: {state}\nFriendly Name: {friendlyName}";
    }

    private async Task<string> CallServiceAsync(
        string domain, string service, JsonElement serviceData, CancellationToken ct)
    {
        var payload = serviceData.ValueKind != JsonValueKind.Undefined
            ? serviceData.GetRawText()
            : "{}";

        var content = new StringContent(payload, Encoding.UTF8, "application/json");
        var response = await _httpClient.PostAsync($"services/{domain}/{service}", content, ct);
        response.EnsureSuccessStatusCode();

        return $"Service {domain}.{service} called successfully.";
    }

    private async Task<string> GetServicesAsync(CancellationToken ct)
    {
        var response = await _httpClient.GetAsync("services", ct);
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync(ct);

        using var doc = JsonDocument.Parse(json);
        var services = doc.RootElement.EnumerateArray()
            .Take(20)
            .Select(d =>
            {
                var domain = d.GetProperty("domain").GetString();
                var svcNames = d.TryGetProperty("services", out var svcs)
                    ? string.Join(", ", svcs.EnumerateObject().Take(5).Select(s => s.Name))
                    : "";
                return $"{domain}: {svcNames}";
            });

        return string.Join('\n', services);
    }
}

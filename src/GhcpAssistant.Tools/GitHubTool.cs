using System.Text.Json;
using GhcpAssistant.Core.Tools;
using Octokit;

namespace GhcpAssistant.Tools;

public sealed class GitHubTool : IAssistantTool
{
    private readonly GitHubClient _client;

    public GitHubTool(string? token = null)
    {
        _client = new GitHubClient(new ProductHeaderValue("GhcpAssistant"));
        if (!string.IsNullOrEmpty(token))
            _client.Credentials = new Credentials(token);
    }

    public string Name => "github";
    public string Description => "Query GitHub REST API: get repo info, list issues, list PRs.";

    public async Task<string> ExecuteAsync(JsonElement parameters, CancellationToken ct)
    {
        var action = parameters.GetProperty("action").GetString()!;
        var owner = parameters.GetProperty("owner").GetString()!;
        var repo = parameters.GetProperty("repo").GetString()!;

        return action.ToLowerInvariant() switch
        {
            "get_repo" => await GetRepoAsync(owner, repo),
            "list_issues" => await ListIssuesAsync(owner, repo),
            "list_prs" => await ListPullRequestsAsync(owner, repo),
            _ => throw new ArgumentException($"Unknown GitHub action '{action}'.")
        };
    }

    private async Task<string> GetRepoAsync(string owner, string repo)
    {
        var r = await _client.Repository.Get(owner, repo);
        return $"Name: {r.FullName}\nDescription: {r.Description}\nStars: {r.StargazersCount}\nLanguage: {r.Language}";
    }

    private async Task<string> ListIssuesAsync(string owner, string repo)
    {
        var issues = await _client.Issue.GetAllForRepository(owner, repo,
            new RepositoryIssueRequest { State = ItemStateFilter.Open });
        return string.Join('\n', issues.Take(10).Select(i => $"#{i.Number} {i.Title}"));
    }

    private async Task<string> ListPullRequestsAsync(string owner, string repo)
    {
        var prs = await _client.PullRequest.GetAllForRepository(owner, repo);
        return string.Join('\n', prs.Take(10).Select(p => $"#{p.Number} {p.Title} ({p.State})"));
    }
}

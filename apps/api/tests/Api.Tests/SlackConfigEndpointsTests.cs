using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using Api.Infrastructure;
using Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Api.Tests;

/// <summary>
/// BDD-style integration tests for /admin/slack endpoints managing project Slack configurations.
/// </summary>
[Collection("Admin Endpoints")]
public class SlackConfigEndpointsTests : AdminTestFixture
{
    public SlackConfigEndpointsTests(WebApplicationFactoryFixture factory) : base(factory)
    {
    }

    [Fact]
    public async Task PostAdminSlack_WhenAdminCreatesConfig_PersistsEncryptedWebhook()
    {
        await ClearSlackConfigsAsync();

        using var adminClient = Factory.CreateHttpsClient();
        var adminEmail = $"admin-slack-create-{Guid.NewGuid():N}@example.com";
        var cookies = await RegisterAndAuthenticateAsync(adminClient, adminEmail, "Admin");
        var adminUserId = await GetUserIdByEmailAsync(adminEmail);

        var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/admin/slack")
        {
            Content = JsonContent.Create(new CreateSlackConfigRequest(
                "MeepleAI",
                "Assistente AI",
                "https://meeple.ai/",
                "https://docs.meeple.ai/",
                "team@meeple.ai",
                "https://meeple.slack.com/",
                "#meeple-project",
                "https://hooks.slack.com/services/test"))
        };
        AddCookies(request, cookies);

        var response = await adminClient.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var dto = await response.Content.ReadFromJsonAsync<SlackConfigDto>(JsonOptions);
        Assert.NotNull(dto);
        Assert.Equal("MeepleAI", dto!.ProjectName);
        Assert.Equal("https://meeple.ai", dto.ProjectUrl);
        Assert.Equal("#meeple-project", dto.Channel);

        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MeepleAiDbContext>();
        var entity = await db.SlackConfigs.SingleAsync(c => c.Id == dto.Id);
        Assert.Equal(adminUserId, entity.CreatedByUserId);
        Assert.Equal("MeepleAI", entity.ProjectName);
        Assert.NotEqual("https://hooks.slack.com/services/test", entity.WebhookUrlEncrypted);
    }

    [Fact]
    public async Task GetAdminSlackById_WhenConfigExists_ReturnsDto()
    {
        await ClearSlackConfigsAsync();

        using var adminClient = Factory.CreateHttpsClient();
        var adminEmail = $"admin-slack-get-{Guid.NewGuid():N}@example.com";
        var cookies = await RegisterAndAuthenticateAsync(adminClient, adminEmail, "Admin");
        var adminUserId = await GetUserIdByEmailAsync(adminEmail);

        var existing = await CreateSlackConfigAsync(adminUserId, "Existing Project");

        var request = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/admin/slack/{existing.Id}");
        AddCookies(request, cookies);

        var response = await adminClient.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var dto = await response.Content.ReadFromJsonAsync<SlackConfigDto>(JsonOptions);
        Assert.NotNull(dto);
        Assert.Equal(existing.Id, dto!.Id);
        Assert.Equal("Existing Project", dto.ProjectName);
    }

    [Fact]
    public async Task GetAdminSlackMessage_WhenConfigExists_ReturnsFormattedMessage()
    {
        await ClearSlackConfigsAsync();

        using var adminClient = Factory.CreateHttpsClient();
        var adminEmail = $"admin-slack-message-{Guid.NewGuid():N}@example.com";
        var cookies = await RegisterAndAuthenticateAsync(adminClient, adminEmail, "Admin");
        var adminUserId = await GetUserIdByEmailAsync(adminEmail);

        var config = await CreateSlackConfigAsync(adminUserId, "Slack Message Project");

        var request = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/admin/slack/{config.Id}/message");
        AddCookies(request, cookies);

        var response = await adminClient.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = document.RootElement;
        Assert.Equal(config.Channel, root.GetProperty("channel").GetString());
        var text = root.GetProperty("text").GetString();
        Assert.NotNull(text);
        Assert.Contains("Slack Message Project", text!, StringComparison.Ordinal);
        Assert.Contains(config.ProjectUrl, text!, StringComparison.Ordinal);
    }
}

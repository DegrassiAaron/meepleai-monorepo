using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Api.Infrastructure;
using Api.Infrastructure.Entities;
using Api.Models;
using Api.Services;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Api.Tests;

/// <summary>
/// Base class for admin endpoint integration tests providing shared helpers and utilities.
/// Implements common authentication, seeding, and HTTP client management functionality.
/// </summary>
public abstract class AdminTestFixture : IClassFixture<WebApplicationFactoryFixture>
{
    protected static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    protected readonly WebApplicationFactoryFixture Factory;

    protected AdminTestFixture(WebApplicationFactoryFixture factory)
    {
        Factory = factory;
    }

    // ===== HTTP Client Helpers =====

    protected HttpClient CreateClientWithoutCookies()
    {
        return Factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = false
        });
    }

    protected static void AddCookies(HttpRequestMessage request, IEnumerable<string> cookies)
    {
        var cookieList = cookies
            .Where(cookie => !string.IsNullOrWhiteSpace(cookie))
            .ToList();

        if (cookieList.Count == 0)
        {
            return;
        }

        request.Headers.Remove("Cookie");
        request.Headers.TryAddWithoutValidation("Cookie", string.Join("; ", cookieList));
    }

    protected static List<string> ExtractCookies(HttpResponseMessage response)
    {
        if (!response.Headers.TryGetValues("Set-Cookie", out var values))
        {
            return new List<string>();
        }

        return values
            .Select(value => value.Split(';')[0])
            .ToList();
    }

    // ===== Authentication Helpers =====

    protected async Task<List<string>> RegisterAndAuthenticateAsync(HttpClient client, string email, string role)
    {
        var payload = new RegisterPayload(email, "Password123!", "Integration Tester", null);
        var response = await client.PostAsJsonAsync("/auth/register", payload);
        response.EnsureSuccessStatusCode();

        if (!string.Equals(role, UserRole.User.ToString(), StringComparison.OrdinalIgnoreCase))
        {
            var parsedRole = Enum.Parse<UserRole>(role, true);
            await PromoteUserAsync(email, parsedRole);
        }

        return ExtractCookies(response);
    }

    protected async Task<string> GetUserIdByEmailAsync(string email)
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MeepleAiDbContext>();
        var user = await db.Users.SingleAsync(u => u.Email == email);
        return user.Id;
    }

    protected async Task PromoteUserAsync(string email, UserRole role)
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MeepleAiDbContext>();
        var user = await db.Users.SingleAsync(u => u.Email == email);
        user.Role = role;
        await db.SaveChangesAsync();
    }

    // ===== Dashboard/Stats Data Seeding =====

    protected async Task<DashboardSeedContext> SeedDashboardDataAsync(string adminUserId, string otherUserId)
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MeepleAiDbContext>();

        db.AiRequestLogs.RemoveRange(db.AiRequestLogs);
        db.AgentFeedbacks.RemoveRange(db.AgentFeedbacks);
        await db.SaveChangesAsync();

        var baseTime = DateTime.UtcNow;

        var adminSuccess = new AiRequestLogEntity
        {
            UserId = adminUserId,
            GameId = "game-1",
            Endpoint = "qa",
            Query = "How to score points?",
            ResponseSnippet = "Prioritize engine building.",
            LatencyMs = 120,
            TokenCount = 50,
            PromptTokens = 30,
            CompletionTokens = 20,
            Confidence = 0.9,
            Status = "Success",
            IpAddress = "127.0.0.1",
            UserAgent = "integration-test/1.0",
            Model = "gpt-4",
            FinishReason = "stop",
            CreatedAt = baseTime.AddMinutes(-30)
        };

        var adminError = new AiRequestLogEntity
        {
            UserId = adminUserId,
            GameId = "game-1",
            Endpoint = "setup",
            Query = "How do I set up?",
            ResponseSnippet = "Unable to answer.",
            LatencyMs = 80,
            TokenCount = 30,
            PromptTokens = 18,
            CompletionTokens = 12,
            Confidence = 0.5,
            Status = "Error",
            ErrorMessage = "timeout",
            IpAddress = "127.0.0.1",
            UserAgent = "integration-test/2.0",
            Model = "gpt-4",
            FinishReason = "length",
            CreatedAt = baseTime.AddMinutes(-20)
        };

        var otherLog = new AiRequestLogEntity
        {
            UserId = otherUserId,
            GameId = "game-2",
            Endpoint = "qa",
            Query = "Unrelated question",
            ResponseSnippet = "Different game response.",
            LatencyMs = 60,
            TokenCount = 20,
            PromptTokens = 10,
            CompletionTokens = 10,
            Confidence = 0.8,
            Status = "Success",
            IpAddress = "192.168.1.10",
            UserAgent = "integration-test/3.0",
            Model = "gpt-3.5",
            FinishReason = "stop",
            CreatedAt = baseTime.AddMinutes(-10)
        };

        db.AiRequestLogs.AddRange(adminSuccess, adminError, otherLog);

        var helpfulFeedback = new AgentFeedbackEntity
        {
            MessageId = "msg-helpful",
            Endpoint = "qa",
            GameId = "game-1",
            UserId = adminUserId,
            Outcome = "helpful",
            CreatedAt = baseTime.AddMinutes(-25),
            UpdatedAt = baseTime.AddMinutes(-25)
        };

        var notHelpfulFeedback = new AgentFeedbackEntity
        {
            MessageId = "msg-not-helpful",
            Endpoint = "setup",
            GameId = "game-1",
            UserId = adminUserId,
            Outcome = "not-helpful",
            CreatedAt = baseTime.AddMinutes(-15),
            UpdatedAt = baseTime.AddMinutes(-15)
        };

        var otherFeedback = new AgentFeedbackEntity
        {
            MessageId = "msg-other",
            Endpoint = "qa",
            GameId = "game-2",
            UserId = otherUserId,
            Outcome = "helpful",
            CreatedAt = baseTime.AddMinutes(-5),
            UpdatedAt = baseTime.AddMinutes(-5)
        };

        db.AgentFeedbacks.AddRange(helpfulFeedback, notHelpfulFeedback, otherFeedback);

        await db.SaveChangesAsync();

        return new DashboardSeedContext(
            baseTime.AddMinutes(-35),
            baseTime.AddMinutes(-15));
    }

    protected sealed record DashboardSeedContext(DateTime StartDate, DateTime EndDate);

    // ===== N8n Configuration Helpers =====

    protected async Task ClearN8nConfigsAsync()
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MeepleAiDbContext>();
        db.N8nConfigs.RemoveRange(db.N8nConfigs);
        await db.SaveChangesAsync();
    }

    protected async Task<N8nConfigDto> CreateN8nConfigAsync(string userId, string name)
    {
        using var scope = Factory.Services.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<N8nConfigService>();
        return await service.CreateConfigAsync(
            userId,
            new CreateN8nConfigRequest(
                name,
                "https://n8n.seed/",
                "seed-api-key",
                "https://n8n.seed/webhook"),
            default);
    }
}

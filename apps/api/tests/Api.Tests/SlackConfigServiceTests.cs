using System;
using Api.Infrastructure;
using Api.Infrastructure.Entities;
using Api.Models;
using Api.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Api.Tests;

public class SlackConfigServiceTests
{
    private static MeepleAiDbContext CreateInMemoryContext()
    {
        var connection = new SqliteConnection("Filename=:memory:");
        connection.Open();

        var options = new DbContextOptionsBuilder<MeepleAiDbContext>()
            .UseSqlite(connection)
            .Options;

        var context = new MeepleAiDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }

    private sealed class FakeSecretProtector : ISecretProtector
    {
        public string Protect(string secret) => $"protected::{secret.Trim()}";
        public string Unprotect(string protectedSecret) => protectedSecret.Replace("protected::", string.Empty, StringComparison.Ordinal);
    }

    private sealed class FakeSecretProtectorFactory : ISecretProtectorFactory
    {
        private readonly ISecretProtector _protector = new FakeSecretProtector();

        public ISecretProtector Create(string encryptionKeyConfigName, string encryptionKeyPlaceholder)
        {
            return _protector;
        }
    }

    private static SlackConfigService CreateService(
        MeepleAiDbContext dbContext,
        ISecretProtectorFactory? protectorFactory = null,
        ISlackMessageBuilder? messageBuilder = null)
    {
        protectorFactory ??= new FakeSecretProtectorFactory();
        messageBuilder ??= new SlackMessageBuilder();
        var loggerMock = new Mock<ILogger<SlackConfigService>>();

        return new SlackConfigService(dbContext, protectorFactory, messageBuilder, loggerMock.Object);
    }

    [Fact]
    public async Task CreateConfigAsync_PersistsEncryptedWebhookAndProjectInfo()
    {
        await using var dbContext = CreateInMemoryContext();
        var service = CreateService(dbContext);

        var dto = await service.CreateConfigAsync(
            "admin-1",
            new CreateSlackConfigRequest(
                ProjectName: "MeepleAI",
                ProjectDescription: "Assistente AI per giochi da tavolo",
                ProjectUrl: "https://meeple.ai",
                DocumentationUrl: "https://docs.meeple.ai",
                ContactEmail: "support@meeple.ai",
                WorkspaceUrl: "https://meeple.slack.com",
                Channel: "#meepleai-project",
                WebhookUrl: "https://hooks.slack.com/services/T/T/T"),
            CancellationToken.None);

        Assert.Equal("MeepleAI", dto.ProjectName);
        Assert.Equal("https://meeple.ai", dto.ProjectUrl);
        Assert.Equal("#meepleai-project", dto.Channel);

        var entity = await dbContext.SlackConfigs.SingleAsync();
        Assert.Equal("MeepleAI", entity.ProjectName);
        Assert.Equal("assistente ai per giochi da tavolo", entity.ProjectDescription?.ToLowerInvariant());
        Assert.Equal("admin-1", entity.CreatedByUserId);
        Assert.Equal("#meepleai-project", entity.Channel);
        Assert.StartsWith("protected::", entity.WebhookUrlEncrypted, StringComparison.Ordinal);
        Assert.NotEqual("https://hooks.slack.com/services/T/T/T", entity.WebhookUrlEncrypted);
    }

    [Fact]
    public async Task UpdateConfigAsync_ModifiesProjectFields()
    {
        await using var dbContext = CreateInMemoryContext();
        var service = CreateService(dbContext);

        var created = await service.CreateConfigAsync(
            "creator",
            new CreateSlackConfigRequest(
                "Project One",
                "Descrizione",
                "https://project.one",
                null,
                null,
                null,
                "#project-one",
                "https://hooks.slack.com/services/old"),
            CancellationToken.None);

        var updated = await service.UpdateConfigAsync(
            created.Id,
            new UpdateSlackConfigRequest(
                ProjectName: "Project Renamed",
                ProjectDescription: "Nuova descrizione",
                ProjectUrl: "https://project.renamed/",
                DocumentationUrl: "https://docs.renamed/",
                ContactEmail: "hello@project.ai",
                WorkspaceUrl: "https://renamed.slack.com/",
                Channel: "#renamed",
                WebhookUrl: "https://hooks.slack.com/services/new",
                IsActive: false),
            CancellationToken.None);

        Assert.Equal("Project Renamed", updated.ProjectName);
        Assert.Equal("https://project.renamed", updated.ProjectUrl);
        Assert.Equal("https://docs.renamed", updated.DocumentationUrl);
        Assert.Equal("hello@project.ai", updated.ContactEmail);
        Assert.Equal("https://renamed.slack.com", updated.WorkspaceUrl);
        Assert.Equal("#renamed", updated.Channel);
        Assert.False(updated.IsActive);

        var entity = await dbContext.SlackConfigs.SingleAsync(c => c.Id == created.Id);
        Assert.Equal("Project Renamed", entity.ProjectName);
        Assert.Equal("https://project.renamed", entity.ProjectUrl);
        Assert.Equal("https://docs.renamed", entity.DocumentationUrl);
        Assert.Equal("hello@project.ai", entity.ContactEmail);
        Assert.Equal("https://renamed.slack.com", entity.WorkspaceUrl);
        Assert.Equal("#renamed", entity.Channel);
        Assert.False(entity.IsActive);
        Assert.StartsWith("protected::", entity.WebhookUrlEncrypted, StringComparison.Ordinal);
    }

    [Fact]
    public async Task BuildProjectInfoMessageAsync_ReturnsFormattedMessage()
    {
        await using var dbContext = CreateInMemoryContext();
        var service = CreateService(dbContext);

        var created = await service.CreateConfigAsync(
            "creator",
            new CreateSlackConfigRequest(
                "MeepleAI",
                "Assistente AI",
                "https://meeple.ai",
                "https://docs.meeple.ai",
                "team@meeple.ai",
                "https://meeple.slack.com",
                "#project",
                "https://hooks.slack.com/services/abc"),
            CancellationToken.None);

        var message = await service.BuildProjectInfoMessageAsync(created.Id, CancellationToken.None);

        Assert.Equal("#project", message.Channel);
        Assert.Contains("MeepleAI", message.Text, StringComparison.Ordinal);
        Assert.Contains("https://meeple.ai", message.Text, StringComparison.Ordinal);
        Assert.Contains("https://docs.meeple.ai", message.Text, StringComparison.Ordinal);
        Assert.Contains("team@meeple.ai", message.Text, StringComparison.Ordinal);
    }

    [Fact]
    public async Task DeleteConfigAsync_RemovesEntity()
    {
        await using var dbContext = CreateInMemoryContext();
        var service = CreateService(dbContext);

        var created = await service.CreateConfigAsync(
            "creator",
            new CreateSlackConfigRequest(
                "Project",
                null,
                "https://project.io",
                null,
                null,
                null,
                "#general",
                "https://hooks.slack.com/services/delete"),
            CancellationToken.None);

        var deleted = await service.DeleteConfigAsync(created.Id, CancellationToken.None);

        Assert.True(deleted);
        Assert.Empty(dbContext.SlackConfigs);
    }

    [Fact]
    public async Task GetConfigAsync_WhenMissing_ReturnsNull()
    {
        await using var dbContext = CreateInMemoryContext();
        var service = CreateService(dbContext);

        var config = await service.GetConfigAsync("missing", CancellationToken.None);

        Assert.Null(config);
    }
}

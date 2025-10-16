using System;
using System.Linq;
using Api.Infrastructure;
using Api.Infrastructure.Entities;
using Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Api.Services;

public class SlackConfigService
{
    private const string EncryptionKeyConfigName = "SLACK_ENCRYPTION_KEY";
    private const string EncryptionKeyPlaceholder = "changeme-slack-32-byte-key-in-production";

    private readonly MeepleAiDbContext _dbContext;
    private readonly ISecretProtector _secretProtector;
    private readonly ISlackMessageBuilder _messageBuilder;
    private readonly ILogger<SlackConfigService> _logger;

    public SlackConfigService(
        MeepleAiDbContext dbContext,
        ISecretProtectorFactory secretProtectorFactory,
        ISlackMessageBuilder messageBuilder,
        ILogger<SlackConfigService> logger)
    {
        _dbContext = dbContext;
        _secretProtector = secretProtectorFactory.Create(EncryptionKeyConfigName, EncryptionKeyPlaceholder);
        _messageBuilder = messageBuilder;
        _logger = logger;
    }

    public async Task<List<SlackConfigDto>> GetConfigsAsync(CancellationToken ct)
    {
        var configs = await _dbContext.SlackConfigs
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync(ct);

        return configs.Select(ToDto).ToList();
    }

    public async Task<SlackConfigDto?> GetConfigAsync(string configId, CancellationToken ct)
    {
        var config = await _dbContext.SlackConfigs
            .FirstOrDefaultAsync(c => c.Id == configId, ct);

        return config is null ? null : ToDto(config);
    }

    public async Task<SlackConfigDto> CreateConfigAsync(string userId, CreateSlackConfigRequest request, CancellationToken ct)
    {
        var projectName = RequireValue(request.ProjectName, nameof(request.ProjectName));
        var projectUrl = NormalizeUrl(RequireValue(request.ProjectUrl, nameof(request.ProjectUrl)));
        var channel = NormalizeChannel(RequireValue(request.Channel, nameof(request.Channel)));
        var webhookUrl = RequireValue(request.WebhookUrl, nameof(request.WebhookUrl));

        var existing = await _dbContext.SlackConfigs
            .AnyAsync(c => c.ProjectName == projectName && c.Channel == channel, ct);

        if (existing)
        {
            throw new InvalidOperationException($"Slack configuration for project '{projectName}' and channel '{channel}' already exists");
        }

        var now = DateTime.UtcNow;

        var entity = new SlackConfigEntity
        {
            Id = Guid.NewGuid().ToString(),
            ProjectName = projectName,
            ProjectDescription = request.ProjectDescription?.Trim(),
            ProjectUrl = projectUrl,
            DocumentationUrl = NormalizeOptionalUrl(request.DocumentationUrl),
            ContactEmail = request.ContactEmail?.Trim(),
            WorkspaceUrl = NormalizeOptionalUrl(request.WorkspaceUrl),
            Channel = channel,
            WebhookUrlEncrypted = _secretProtector.Protect(webhookUrl),
            IsActive = true,
            CreatedByUserId = userId,
            CreatedAt = now,
            UpdatedAt = now
        };

        _dbContext.SlackConfigs.Add(entity);
        await _dbContext.SaveChangesAsync(ct);

        _logger.LogInformation("Slack config {ConfigId} created for project {ProjectName}", entity.Id, projectName);

        return ToDto(entity);
    }

    public async Task<SlackConfigDto> UpdateConfigAsync(string configId, UpdateSlackConfigRequest request, CancellationToken ct)
    {
        var entity = await _dbContext.SlackConfigs
            .FirstOrDefaultAsync(c => c.Id == configId, ct)
            ?? throw new InvalidOperationException("Configuration not found");

        if (!string.IsNullOrWhiteSpace(request.ProjectName) && request.ProjectName.Trim() != entity.ProjectName)
        {
            var newName = request.ProjectName.Trim();
            var channelToCompare = request.Channel is not null ? NormalizeChannel(request.Channel) : entity.Channel;
            var conflict = await _dbContext.SlackConfigs
                .AnyAsync(c => c.Id != configId && c.ProjectName == newName && c.Channel == channelToCompare, ct);

            if (conflict)
            {
                throw new InvalidOperationException($"Slack configuration for project '{newName}' and channel '{channelToCompare}' already exists");
            }

            entity.ProjectName = newName;
        }

        if (request.ProjectDescription != null)
        {
            entity.ProjectDescription = request.ProjectDescription.Trim();
        }

        if (request.ProjectUrl != null)
        {
            entity.ProjectUrl = NormalizeUrl(request.ProjectUrl);
        }

        if (request.DocumentationUrl != null)
        {
            entity.DocumentationUrl = NormalizeOptionalUrl(request.DocumentationUrl);
        }

        if (request.ContactEmail != null)
        {
            entity.ContactEmail = string.IsNullOrWhiteSpace(request.ContactEmail)
                ? null
                : request.ContactEmail.Trim();
        }

        if (request.WorkspaceUrl != null)
        {
            entity.WorkspaceUrl = NormalizeOptionalUrl(request.WorkspaceUrl);
        }

        if (request.Channel != null)
        {
            var newChannel = NormalizeChannel(request.Channel);

            if (!string.Equals(newChannel, entity.Channel, StringComparison.Ordinal))
            {
                var nameToCompare = entity.ProjectName;
                if (request.ProjectName != null)
                {
                    nameToCompare = request.ProjectName.Trim();
                }

                var conflict = await _dbContext.SlackConfigs
                    .AnyAsync(c => c.Id != configId && c.ProjectName == nameToCompare && c.Channel == newChannel, ct);

                if (conflict)
                {
                    throw new InvalidOperationException($"Slack configuration for project '{nameToCompare}' and channel '{newChannel}' already exists");
                }

                entity.Channel = newChannel;
            }
        }

        if (request.WebhookUrl != null)
        {
            entity.WebhookUrlEncrypted = _secretProtector.Protect(request.WebhookUrl);
        }

        if (request.IsActive.HasValue)
        {
            entity.IsActive = request.IsActive.Value;
        }

        entity.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(ct);

        _logger.LogInformation("Slack config {ConfigId} updated", entity.Id);

        return ToDto(entity);
    }

    public async Task<bool> DeleteConfigAsync(string configId, CancellationToken ct)
    {
        var entity = await _dbContext.SlackConfigs
            .FirstOrDefaultAsync(c => c.Id == configId, ct);

        if (entity == null)
        {
            return false;
        }

        _dbContext.SlackConfigs.Remove(entity);
        await _dbContext.SaveChangesAsync(ct);

        _logger.LogInformation("Slack config {ConfigId} deleted", configId);

        return true;
    }

    public async Task<SlackMessageDto> BuildProjectInfoMessageAsync(string configId, CancellationToken ct)
    {
        var entity = await _dbContext.SlackConfigs
            .FirstOrDefaultAsync(c => c.Id == configId, ct)
            ?? throw new InvalidOperationException("Configuration not found");

        var dto = ToDto(entity);
        return _messageBuilder.BuildProjectSummary(dto);
    }

    private static SlackConfigDto ToDto(SlackConfigEntity entity)
    {
        return new SlackConfigDto(
            entity.Id,
            entity.ProjectName,
            entity.ProjectDescription,
            entity.ProjectUrl,
            entity.DocumentationUrl,
            entity.ContactEmail,
            entity.WorkspaceUrl,
            entity.Channel,
            entity.IsActive,
            entity.CreatedAt,
            entity.UpdatedAt);
    }

    private static string RequireValue(string value, string propertyName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException($"{propertyName} is required");
        }

        return value.Trim();
    }

    private static string NormalizeChannel(string channel)
    {
        var trimmed = channel.Trim();
        if (string.IsNullOrWhiteSpace(trimmed))
        {
            throw new InvalidOperationException("Channel is required");
        }

        return trimmed.StartsWith('#') ? trimmed : $"#{trimmed}";
    }

    private static string NormalizeUrl(string url)
    {
        var trimmed = url.Trim();
        return trimmed.TrimEnd('/');
    }

    private static string? NormalizeOptionalUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return null;
        }

        return NormalizeUrl(url);
    }
}

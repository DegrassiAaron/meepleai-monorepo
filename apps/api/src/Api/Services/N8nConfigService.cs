using System;
using Api.Infrastructure;
using Api.Infrastructure.Entities;
using Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Api.Services;

public class N8nConfigService
{
    private readonly MeepleAiDbContext _db;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ISecretProtector _secretProtector;
    private readonly ILogger<N8nConfigService> _logger;
    public N8nConfigService(
        MeepleAiDbContext db,
        IHttpClientFactory httpClientFactory,
        ISecretProtectorFactory secretProtectorFactory,
        ILogger<N8nConfigService> logger)
    {
        _db = db;
        _httpClientFactory = httpClientFactory;
        _secretProtector = secretProtectorFactory.Create(EncryptionKeyConfigName, EncryptionKeyPlaceholder);
        _logger = logger;
    }

    public async Task<List<N8nConfigDto>> GetConfigsAsync(CancellationToken ct)
    {
        var configs = await _db.N8nConfigs
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync(ct);

        return configs.Select(c => new N8nConfigDto(
            c.Id,
            c.Name,
            c.BaseUrl,
            c.WebhookUrl,
            c.IsActive,
            c.LastTestedAt,
            c.LastTestResult,
            c.CreatedAt,
            c.UpdatedAt
        )).ToList();
    }

    public async Task<N8nConfigDto?> GetConfigAsync(string configId, CancellationToken ct)
    {
        var config = await _db.N8nConfigs
            .FirstOrDefaultAsync(c => c.Id == configId, ct);

        if (config == null)
        {
            return null;
        }

        return new N8nConfigDto(
            config.Id,
            config.Name,
            config.BaseUrl,
            config.WebhookUrl,
            config.IsActive,
            config.LastTestedAt,
            config.LastTestResult,
            config.CreatedAt,
            config.UpdatedAt
        );
    }

    public async Task<N8nConfigDto> CreateConfigAsync(
        string userId,
        CreateN8nConfigRequest request,
        CancellationToken ct)
    {
        var existingConfig = await _db.N8nConfigs
            .FirstOrDefaultAsync(c => c.Name == request.Name, ct);

        if (existingConfig != null)
        {
            throw new InvalidOperationException($"Configuration with name '{request.Name}' already exists");
        }

        var config = new N8nConfigEntity
        {
            Id = Guid.NewGuid().ToString(),
            Name = request.Name,
            BaseUrl = request.BaseUrl.TrimEnd('/'),
            ApiKeyEncrypted = _secretProtector.Protect(request.ApiKey),
            WebhookUrl = request.WebhookUrl?.TrimEnd('/'),
            IsActive = true,
            CreatedByUserId = userId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _db.N8nConfigs.Add(config);
        await _db.SaveChangesAsync(ct);

        return new N8nConfigDto(
            config.Id,
            config.Name,
            config.BaseUrl,
            config.WebhookUrl,
            config.IsActive,
            config.LastTestedAt,
            config.LastTestResult,
            config.CreatedAt,
            config.UpdatedAt
        );
    }

    public async Task<N8nConfigDto> UpdateConfigAsync(
        string configId,
        UpdateN8nConfigRequest request,
        CancellationToken ct)
    {
        var config = await _db.N8nConfigs
            .FirstOrDefaultAsync(c => c.Id == configId, ct);

        if (config == null)
        {
            throw new InvalidOperationException("Configuration not found");
        }

        if (request.Name != null && request.Name != config.Name)
        {
            var existingConfig = await _db.N8nConfigs
                .FirstOrDefaultAsync(c => c.Name == request.Name && c.Id != configId, ct);

            if (existingConfig != null)
            {
                throw new InvalidOperationException($"Configuration with name '{request.Name}' already exists");
            }

            config.Name = request.Name;
        }

        if (request.BaseUrl != null)
        {
            config.BaseUrl = request.BaseUrl.TrimEnd('/');
        }

        if (request.ApiKey != null)
        {
            config.ApiKeyEncrypted = _secretProtector.Protect(request.ApiKey);
        }

        if (request.WebhookUrl != null)
        {
            config.WebhookUrl = request.WebhookUrl.TrimEnd('/');
        }

        if (request.IsActive.HasValue)
        {
            config.IsActive = request.IsActive.Value;
        }

        config.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);

        return new N8nConfigDto(
            config.Id,
            config.Name,
            config.BaseUrl,
            config.WebhookUrl,
            config.IsActive,
            config.LastTestedAt,
            config.LastTestResult,
            config.CreatedAt,
            config.UpdatedAt
        );
    }

    public async Task<bool> DeleteConfigAsync(string configId, CancellationToken ct)
    {
        var config = await _db.N8nConfigs
            .FirstOrDefaultAsync(c => c.Id == configId, ct);

        if (config == null)
        {
            return false;
        }

        _db.N8nConfigs.Remove(config);
        await _db.SaveChangesAsync(ct);

        return true;
    }

    public async Task<N8nTestResult> TestConnectionAsync(string configId, CancellationToken ct)
    {
        var config = await _db.N8nConfigs
            .FirstOrDefaultAsync(c => c.Id == configId, ct);

        if (config == null)
        {
            throw new InvalidOperationException("Configuration not found");
        }

        var apiKey = _secretProtector.Unprotect(config.ApiKeyEncrypted);
        var httpClient = _httpClientFactory.CreateClient();

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, $"{config.BaseUrl}/api/v1/workflows");
            request.Headers.Add("X-N8N-API-KEY", apiKey);

            var startTime = DateTime.UtcNow;
            using var response = await httpClient.SendAsync(request, ct);
            var latency = (int)(DateTime.UtcNow - startTime).TotalMilliseconds;

            var success = response.IsSuccessStatusCode;
            var message = success
                ? $"Connection successful ({latency}ms)"
                : $"Connection failed: {response.StatusCode}";

            config.LastTestedAt = DateTime.UtcNow;
            config.LastTestResult = message;
            await _db.SaveChangesAsync(ct);

            return new N8nTestResult(success, message, latency);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to test n8n connection for config {ConfigId}", configId);

            var message = $"Connection failed: {ex.Message}";
            config.LastTestedAt = DateTime.UtcNow;
            config.LastTestResult = message;
            await _db.SaveChangesAsync(ct);

            return new N8nTestResult(false, message, null);
        }
    }

    private const string EncryptionKeyConfigName = "N8N_ENCRYPTION_KEY";
    private const string EncryptionKeyPlaceholder = "changeme-replace-with-32-byte-key-in-production";
}

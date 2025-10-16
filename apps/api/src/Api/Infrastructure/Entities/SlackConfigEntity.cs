using System;

namespace Api.Infrastructure.Entities;

public class SlackConfigEntity
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string ProjectName { get; set; } = string.Empty;
    public string? ProjectDescription { get; set; }
        = null;
    public string ProjectUrl { get; set; } = string.Empty;
    public string? DocumentationUrl { get; set; }
        = null;
    public string? ContactEmail { get; set; }
        = null;
    public string? WorkspaceUrl { get; set; }
        = null;
    public string Channel { get; set; } = string.Empty;
    public string WebhookUrlEncrypted { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public string CreatedByUserId { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public UserEntity? CreatedBy { get; set; }
        = null;
}

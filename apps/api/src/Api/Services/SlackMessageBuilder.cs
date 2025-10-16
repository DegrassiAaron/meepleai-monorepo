using System;
using System.Collections.Generic;
using System.Linq;
using Api.Models;

namespace Api.Services;

public interface ISlackMessageBuilder
{
    SlackMessageDto BuildProjectSummary(SlackConfigDto config);
}

public class SlackMessageBuilder : ISlackMessageBuilder
{
    public SlackMessageDto BuildProjectSummary(SlackConfigDto config)
    {
        var lines = new List<string>
        {
            $"*{config.ProjectName.Trim()}*"
        };

        if (!string.IsNullOrWhiteSpace(config.ProjectDescription))
        {
            lines.Add(config.ProjectDescription.Trim());
        }

        var bulletLines = new List<string>
        {
            $"• Repository: {config.ProjectUrl}"
        };

        if (!string.IsNullOrWhiteSpace(config.DocumentationUrl))
        {
            bulletLines.Add($"• Documentazione: {config.DocumentationUrl}");
        }

        if (!string.IsNullOrWhiteSpace(config.WorkspaceUrl))
        {
            bulletLines.Add($"• Workspace: {config.WorkspaceUrl}");
        }

        if (!string.IsNullOrWhiteSpace(config.ContactEmail))
        {
            bulletLines.Add($"• Contatto: {config.ContactEmail}");
        }

        if (bulletLines.Count > 0)
        {
            lines.Add(string.Empty);
            lines.AddRange(bulletLines);
        }

        var text = string.Join("\n", lines.Where(line => line != null));

        return new SlackMessageDto(config.Channel, text);
    }
}

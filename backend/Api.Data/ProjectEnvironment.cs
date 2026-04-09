using System.ComponentModel.DataAnnotations;
using Api.Abstractions;
using Api.Data.Extensions;

namespace Api.Data;

public class ProjectEnvironment : BaseEntityWithId<ProjectEnvironmentId>
{
    public required ProjectId ProjectId { get; set; }
    public required List<string> Branches { get; set; }
    public required bool AutoDeploy { get; set; } = true;

    [MaxLength(255)]
    public string? Name { get; set; }

    public EnvironmentType Type { get; set; } = EnvironmentType.Production;

    /// <summary>
    /// For preview environments created from a pull request.
    /// </summary>
    public int? PullRequestNumber { get; set; }

    /// <summary>
    /// The generated subdomain for preview environments (e.g., "pr-42-myproject").
    /// </summary>
    [MaxLength(255)]
    public string? Subdomain { get; set; }

    public ServerTierId? ServerTierId { get; set; }
    public string? StripeSubscriptionId { get; set; }
    public string? StripeSubscriptionItemId { get; set; }
    public EnvironmentBillingStatus BillingStatus { get; set; } = EnvironmentBillingStatus.Active;

    public virtual Project? Project { get; set; }
    public virtual ICollection<ProjectEnvironmentVariable>? ProjectEnvironmentVariables { get; set; }
    public virtual ICollection<Deployment>? Deployments { get; set; }

    /// <summary>
    /// Returns the environment's URL. For preview environments, returns the
    /// environment-specific internal domain. For production, returns the
    /// project's primary domain (external custom domain or internal frontend).
    /// </summary>
    public string? GetUrl()
    {
        if (Type != EnvironmentType.Production)
        {
            var envDomain = Project?.Domains?
                .FirstOrDefault(d => d.ProjectEnvironmentId == Id && d.Type == DomainType.Internal);
            if (envDomain != null) return envDomain.GetValue;
        }

        return Project?.Domains?.GetPrimary()?.GetValue;
    }

    public static string FormatInvoiceDescription(string? projectName, string? environmentName, ServerTierId tierId)
        => $"{projectName ?? "Project"} — {environmentName ?? "Environment"} ({tierId.Value})";
}
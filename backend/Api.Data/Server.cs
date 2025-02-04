namespace Api.Data;

public class Server : BaseEntityWithId<ServerId>
{
    public LocationId? LocationId { get; set; }
    public SecretId SecretId { get; set; } = null!;
    public string MachineId { get; set; } = string.Empty; // cat /etc/machine-id
    public string Ram { get; set; } = string.Empty;
    public string VCpu { get; set; } = string.Empty;
    public string Ip { get; set; } = string.Empty;

    public virtual Location? Location { get; set; }
    public virtual Secret? Secret { get; set; }
    public virtual ICollection<Deployment>? Deployments { get; set; }
}
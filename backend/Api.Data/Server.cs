namespace Api.Data;

public class Server : BaseEntityWithId<ServerId>
{
    public LocationId? LocationId { get; set; }
    public SecretId SecretId { get; set; } = null!;
    public required string MachineId { get; set; } // cat /etc/machine-id
    public required string Ram { get; set; }
    public required string VCpu { get; set; }
    public required string Ip { get; set; }

    public virtual Location? Location { get; set; }
    public virtual Secret? Secret { get; set; }
    public virtual ICollection<Deployment>? Deployments { get; set; }
}
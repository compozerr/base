namespace Api.Data;

public class Server : BaseEntityWithId<ServerId>
{
    public LocationId? LocationId { get; set; }
    public SecretId SecretId { get; set; } = null!;
    public string MachineId { get; set; } = string.Empty; // cat /etc/machine-id
    public string Ram { get; set; } = string.Empty; // cat /proc/meminfo | grep "MemAvailable:" and get ram and convert to gb
    public string VCpu { get; set; } = string.Empty; // lscpu | grep -E "Core\(s\) per socket:|Thread\(s\) per core:" and get core count and get thread per core times core count e.g. 8c/16t
    public string Ip { get; set; } = string.Empty; // curl api.ipify.org

    public virtual Location? Location { get; set; }
    public virtual Secret? Secret { get; set; }
    public virtual ICollection<Deployment>? Deployments { get; set; }
}
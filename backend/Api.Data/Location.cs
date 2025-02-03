namespace Api.Data;

public class Location : BaseEntityWithId<LocationId>
{
    public required string IsoCountryCode { get; set; } //ISO 3166-1 alpha-3
    public virtual ICollection<Server>? Servers { get; set; }
}
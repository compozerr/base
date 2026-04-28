using System.Text.Json.Serialization;

namespace Organizations.Models;

[JsonConverter(typeof(JsonStringEnumConverter<OrganizationRole>))]
public enum OrganizationRole
{
    Admin = 0,
    Member = 1
}

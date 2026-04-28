using System.Text.Json.Serialization;

namespace Organizations.Models;

[JsonConverter(typeof(JsonStringEnumConverter<InvitationStatus>))]
public enum InvitationStatus
{
    Pending = 0,
    Accepted = 1,
    Declined = 2,
    Expired = 3
}

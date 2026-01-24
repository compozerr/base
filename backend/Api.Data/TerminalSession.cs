using System.ComponentModel.DataAnnotations;
using Api.Abstractions;
using Auth.Abstractions;
using Database.Data;

namespace Api.Data;

public class TerminalSession : BaseEntityWithId<TerminalSessionId>
{
    public required ProjectId ProjectId { get; set; }
    public required UserId UserId { get; set; }
    public required DateTime StartedAtUtc { get; set; }
    public DateTime? EndedAtUtc { get; set; }

    [MaxLength(500)]
    public string? DisconnectReason { get; set; }

    public virtual Project? Project { get; set; }
}

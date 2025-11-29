using Api.Data;
using Auth.Abstractions;
using Core.Abstractions;

namespace Cli.Abstractions;

public sealed record ProjectAllocatedToUserEvent(
    Project Entity, UserId UserId) : IEvent;

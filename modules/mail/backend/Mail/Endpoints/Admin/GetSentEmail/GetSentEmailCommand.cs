using Core.MediatR;

namespace Mail.Endpoints.Admin.GetSentEmail;

public sealed record GetSentEmailCommand(SentEmailId Id) : ICommand<GetSentEmailResponse?>;

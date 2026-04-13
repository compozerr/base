using Core.MediatR;

namespace Stripe.Endpoints.PaymentMethods.CloneFromPersonal;

public sealed record CloneFromPersonalCommand : ICommand<CloneFromPersonalResponse>;

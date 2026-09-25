using MediatR;

namespace IID.Application.Features.Activities.Commands.MarkActivityRead;

public sealed record MarkActivityReadCommand(List<string>? ActivityIds, bool? All) : IRequest<Result>;

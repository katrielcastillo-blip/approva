using Approva.Application.Requests.Dtos;
using MediatR;

namespace Approva.Application.Requests.Queries.GetRequestById;

public record GetRequestByIdQuery(Guid RequestId) : IRequest<RequestDetailDto>;

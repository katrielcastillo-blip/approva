using Approva.Application.Common.Exceptions;
using Approva.Application.Common.Interfaces;
using Approva.Application.Workflows.Dtos;
using Approva.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Approva.Application.Workflows.Queries.GetWorkflowDefinitionById;

public class GetWorkflowDefinitionByIdQueryHandler : IRequestHandler<GetWorkflowDefinitionByIdQuery, WorkflowDefinitionDto>
{
    private readonly IApprovaDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public GetWorkflowDefinitionByIdQueryHandler(IApprovaDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<WorkflowDefinitionDto> Handle(GetWorkflowDefinitionByIdQuery query, CancellationToken cancellationToken)
    {
        var definition = await _db.WorkflowDefinitions
            .Include(d => d.Steps).ThenInclude(s => s.Conditions)
            .FirstOrDefaultAsync(d => d.Id == query.WorkflowDefinitionId && d.TenantId == _currentUser.TenantId, cancellationToken)
            ?? throw new NotFoundException(nameof(WorkflowDefinition), query.WorkflowDefinitionId);

        return new WorkflowDefinitionDto(
            definition.Id,
            definition.Name,
            definition.EntityType,
            definition.Version,
            definition.IsActive,
            definition.Steps.OrderBy(s => s.Order).Select(s => new WorkflowStepDto(
                s.Id,
                s.Order,
                s.Name,
                s.ApproverType.ToString(),
                s.ApproverRef,
                s.SlaHours,
                s.EscalationPolicy.ToString(),
                s.Conditions.Select(c => new WorkflowConditionDto(c.Id, c.Field, c.Operator.ToString(), c.Value)).ToList()))
                .ToList());
    }
}

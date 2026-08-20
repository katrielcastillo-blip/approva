using Approva.Application.Common.Interfaces;
using Approva.Application.Workflows.Dtos;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Approva.Application.Workflows.Queries.ListWorkflowDefinitions;

public class ListWorkflowDefinitionsQueryHandler
    : IRequestHandler<ListWorkflowDefinitionsQuery, IReadOnlyCollection<WorkflowDefinitionSummaryDto>>
{
    private readonly IApprovaDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public ListWorkflowDefinitionsQueryHandler(IApprovaDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<IReadOnlyCollection<WorkflowDefinitionSummaryDto>> Handle(
        ListWorkflowDefinitionsQuery query, CancellationToken cancellationToken)
    {
        return await _db.WorkflowDefinitions
            .Where(d => d.TenantId == _currentUser.TenantId)
            .Select(d => new WorkflowDefinitionSummaryDto(d.Id, d.Name, d.EntityType, d.Version, d.IsActive, d.Steps.Count))
            .ToListAsync(cancellationToken);
    }
}

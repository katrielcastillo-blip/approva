using Approva.Application.Common.Exceptions;
using Approva.Application.Common.Interfaces;
using Approva.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Approva.Application.Workflows.Commands.SetWorkflowDefinitionActive;

public class SetWorkflowDefinitionActiveCommandHandler : IRequestHandler<SetWorkflowDefinitionActiveCommand>
{
    private readonly IApprovaDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public SetWorkflowDefinitionActiveCommandHandler(IApprovaDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task Handle(SetWorkflowDefinitionActiveCommand cmd, CancellationToken cancellationToken)
    {
        var definition = await _db.WorkflowDefinitions
            .FirstOrDefaultAsync(d => d.Id == cmd.WorkflowDefinitionId && d.TenantId == _currentUser.TenantId, cancellationToken)
            ?? throw new NotFoundException(nameof(WorkflowDefinition), cmd.WorkflowDefinitionId);

        if (cmd.IsActive)
            definition.Activate();
        else
            definition.Deactivate();

        await _db.SaveChangesAsync(cancellationToken);
    }
}

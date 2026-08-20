using System.Text.Json;
using Approva.Application.Common.Exceptions;
using Approva.Application.Common.Interfaces;
using Approva.Domain.Entities;
using Approva.Domain.Enums;
using Approva.Domain.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Approva.Application.Requests.Commands.CreateRequest;

public class CreateRequestCommandHandler : IRequestHandler<CreateRequestCommand, Guid>
{
    private readonly IApprovaDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public CreateRequestCommandHandler(IApprovaDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<Guid> Handle(CreateRequestCommand cmd, CancellationToken cancellationToken)
    {
        var tenantId = _currentUser.TenantId;

        var definition = await _db.WorkflowDefinitions
            .Include(d => d.Steps).ThenInclude(s => s.Conditions)
            .FirstOrDefaultAsync(d => d.Id == cmd.WorkflowDefinitionId && d.TenantId == tenantId, cancellationToken)
            ?? throw new NotFoundException(nameof(WorkflowDefinition), cmd.WorkflowDefinitionId);

        if (!definition.IsActive)
            throw new ConflictException("El flujo de aprobación no está activo.");

        var requester = await _db.Users
            .FirstOrDefaultAsync(u => u.Id == _currentUser.UserId && u.TenantId == tenantId, cancellationToken)
            ?? throw new NotFoundException(nameof(User), _currentUser.UserId);

        var request = Request.Create(tenantId, definition.Id, requester.Id, cmd.Title, cmd.Amount, cmd.Currency, cmd.PayloadJson);
        _db.Requests.Add(request);

        _db.AuditEvents.Add(AuditEvent.Create(tenantId, request.Id, requester.Id, AuditEventType.RequestCreated,
            JsonSerializer.Serialize(new { request.Title, request.Amount, request.Currency })));

        var firstStep = WorkflowEngine.DetermineNextStep(definition, request, currentStepId: null);
        request.Submit(firstStep?.Id);

        _db.AuditEvents.Add(AuditEvent.Create(tenantId, request.Id, requester.Id, AuditEventType.RequestSubmitted));

        if (firstStep is null)
        {
            _db.AuditEvents.Add(AuditEvent.Create(tenantId, request.Id, requester.Id, AuditEventType.RequestApproved,
                JsonSerializer.Serialize(new { reason = "no hay pasos aplicables" })));
        }
        else
        {
            var tenantUsers = await _db.Users.Where(u => u.TenantId == tenantId).ToListAsync(cancellationToken);
            var assigneeId = WorkflowEngine.ResolveApprover(firstStep, requester, tenantUsers);
            var assignee = tenantUsers.First(u => u.Id == assigneeId);

            var task = ApprovalTask.Create(request.Id, firstStep.Id, assignee.EffectiveAssigneeId, firstStep.SlaHours);
            _db.ApprovalTasks.Add(task);

            _db.AuditEvents.Add(AuditEvent.Create(tenantId, request.Id, requester.Id, AuditEventType.TaskAssigned,
                JsonSerializer.Serialize(new { step = firstStep.Name, assignedTo = assignee.Name })));
        }

        await _db.SaveChangesAsync(cancellationToken);

        return request.Id;
    }
}

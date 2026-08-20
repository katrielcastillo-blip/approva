using System.Text.Json;
using Approva.Application.Common.Exceptions;
using Approva.Application.Common.Interfaces;
using Approva.Domain.Entities;
using Approva.Domain.Enums;
using Approva.Domain.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Approva.Application.Requests.Commands.DecideApprovalTask;

public class DecideApprovalTaskCommandHandler : IRequestHandler<DecideApprovalTaskCommand, DecideApprovalTaskResult>
{
    private readonly IApprovaDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public DecideApprovalTaskCommandHandler(IApprovaDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<DecideApprovalTaskResult> Handle(DecideApprovalTaskCommand cmd, CancellationToken cancellationToken)
    {
        var tenantId = _currentUser.TenantId;

        var request = await _db.Requests.FirstOrDefaultAsync(r => r.Id == cmd.RequestId && r.TenantId == tenantId, cancellationToken)
            ?? throw new NotFoundException(nameof(Request), cmd.RequestId);

        var task = await _db.ApprovalTasks
            .Where(t => t.RequestId == request.Id && t.Status == ApprovalTaskStatus.Pending)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException("Tarea pendiente para la solicitud", cmd.RequestId);

        if (task.AssignedToUserId != _currentUser.UserId)
            throw new ForbiddenException("Solo el usuario asignado puede decidir esta tarea.");

        var definition = await _db.WorkflowDefinitions
            .Include(d => d.Steps).ThenInclude(s => s.Conditions)
            .FirstOrDefaultAsync(d => d.Id == request.WorkflowDefinitionId, cancellationToken)
            ?? throw new NotFoundException(nameof(WorkflowDefinition), request.WorkflowDefinitionId);

        var eventType = cmd.Decision == ApprovalDecision.Approve
            ? AuditEventType.TaskApproved
            : AuditEventType.TaskRejected;

        if (cmd.Decision == ApprovalDecision.Approve)
            task.Approve(_currentUser.UserId, cmd.Comment);
        else
            task.Reject(_currentUser.UserId, cmd.Comment);

        _db.AuditEvents.Add(AuditEvent.Create(tenantId, request.Id, _currentUser.UserId, eventType,
            JsonSerializer.Serialize(new { comment = cmd.Comment })));

        if (cmd.Decision == ApprovalDecision.Reject)
        {
            request.Reject();
            _db.AuditEvents.Add(AuditEvent.Create(tenantId, request.Id, _currentUser.UserId, AuditEventType.RequestRejected));
        }
        else
        {
            var nextStep = WorkflowEngine.DetermineNextStep(definition, request, task.StepId);
            request.AdvanceTo(nextStep?.Id);

            if (nextStep is null)
            {
                _db.AuditEvents.Add(AuditEvent.Create(tenantId, request.Id, _currentUser.UserId, AuditEventType.RequestApproved));
            }
            else
            {
                var requester = await _db.Users.FirstAsync(u => u.Id == request.RequesterId, cancellationToken);
                var tenantUsers = await _db.Users.Where(u => u.TenantId == tenantId).ToListAsync(cancellationToken);
                var assigneeId = WorkflowEngine.ResolveApprover(nextStep, requester, tenantUsers);
                var assignee = tenantUsers.First(u => u.Id == assigneeId);

                var nextTask = ApprovalTask.Create(request.Id, nextStep.Id, assignee.EffectiveAssigneeId, nextStep.SlaHours);
                _db.ApprovalTasks.Add(nextTask);

                _db.AuditEvents.Add(AuditEvent.Create(tenantId, request.Id, _currentUser.UserId, AuditEventType.TaskAssigned,
                    JsonSerializer.Serialize(new { step = nextStep.Name, assignedTo = assignee.Name })));
            }
        }

        try
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConflictException(
                "Esta tarea ya fue decidida por otro usuario o cambió mientras la procesabas. Recarga y vuelve a intentar.");
        }

        return new DecideApprovalTaskResult(request.Id, request.Status.ToString());
    }
}

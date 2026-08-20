using Approva.Application.Common.Exceptions;
using Approva.Application.Common.Interfaces;
using Approva.Application.Requests.Dtos;
using Approva.Domain.Entities;
using Approva.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Approva.Application.Requests.Queries.GetRequestById;

public class GetRequestByIdQueryHandler : IRequestHandler<GetRequestByIdQuery, RequestDetailDto>
{
    private readonly IApprovaDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public GetRequestByIdQueryHandler(IApprovaDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<RequestDetailDto> Handle(GetRequestByIdQuery query, CancellationToken cancellationToken)
    {
        var tenantId = _currentUser.TenantId;

        var request = await _db.Requests
            .FirstOrDefaultAsync(r => r.Id == query.RequestId && r.TenantId == tenantId, cancellationToken)
            ?? throw new NotFoundException(nameof(Request), query.RequestId);

        var isOwnerOrAdmin = request.RequesterId == _currentUser.UserId || _currentUser.Role == UserRole.Admin;

        var tasks = await _db.ApprovalTasks.Where(t => t.RequestId == request.Id).ToListAsync(cancellationToken);
        var isAssignedApprover = tasks.Any(t => t.AssignedToUserId == _currentUser.UserId);

        if (!isOwnerOrAdmin && !isAssignedApprover)
            throw new ForbiddenException("No tienes acceso a esta solicitud.");

        var definition = await _db.WorkflowDefinitions
            .FirstOrDefaultAsync(d => d.Id == request.WorkflowDefinitionId, cancellationToken)
            ?? throw new NotFoundException(nameof(WorkflowDefinition), request.WorkflowDefinitionId);

        var requester = await _db.Users.FirstAsync(u => u.Id == request.RequesterId, cancellationToken);

        var stepIds = tasks.Select(t => t.StepId).Distinct().ToList();
        var steps = await _db.WorkflowSteps.Where(s => stepIds.Contains(s.Id))
            .ToDictionaryAsync(s => s.Id, cancellationToken);

        var userIds = tasks.Select(t => t.AssignedToUserId).Distinct().ToList();
        var users = await _db.Users.Where(u => userIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u.Name, cancellationToken);

        var taskDtos = tasks
            .OrderBy(t => steps.TryGetValue(t.StepId, out var s) ? s.Order : int.MaxValue)
            .Select(t => new ApprovalTaskDto(
                t.Id,
                steps.TryGetValue(t.StepId, out var step) ? step.Name : "—",
                t.AssignedToUserId,
                users.GetValueOrDefault(t.AssignedToUserId, "—"),
                t.Status.ToString(),
                t.AssignedAt,
                t.DueAt,
                t.DecidedAt,
                t.Comment))
            .ToList();

        var auditEvents = await _db.AuditEvents
            .Where(e => e.RequestId == request.Id)
            .OrderBy(e => e.OccurredAt)
            .ToListAsync(cancellationToken);

        var actorIds = auditEvents.Select(e => e.ActorId).Distinct().ToList();
        var actorNames = await _db.Users.Where(u => actorIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u.Name, cancellationToken);

        var auditDtos = auditEvents.Select(e => new AuditEventDto(
            e.Id,
            e.EventType.ToString(),
            e.ActorId,
            actorNames.GetValueOrDefault(e.ActorId, "—"),
            e.PayloadJson,
            e.OccurredAt)).ToList();

        return new RequestDetailDto(
            request.Id,
            request.Title,
            request.Amount,
            request.Currency,
            request.PayloadJson,
            request.Status.ToString(),
            request.RequesterId,
            requester.Name,
            definition.Id,
            definition.Name,
            request.CreatedAt,
            request.CompletedAt,
            taskDtos,
            auditDtos);
    }
}

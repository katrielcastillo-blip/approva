using Approva.Application.Common.Exceptions;
using Approva.Application.Common.Interfaces;
using Approva.Domain.Entities;
using Approva.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Approva.Application.Requests.Commands.CancelRequest;

public class CancelRequestCommandHandler : IRequestHandler<CancelRequestCommand>
{
    private readonly IApprovaDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public CancelRequestCommandHandler(IApprovaDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task Handle(CancelRequestCommand cmd, CancellationToken cancellationToken)
    {
        var tenantId = _currentUser.TenantId;

        var request = await _db.Requests
            .FirstOrDefaultAsync(r => r.Id == cmd.RequestId && r.TenantId == tenantId, cancellationToken)
            ?? throw new NotFoundException(nameof(Request), cmd.RequestId);

        if (request.RequesterId != _currentUser.UserId && _currentUser.Role != UserRole.Admin)
            throw new ForbiddenException("Solo el solicitante o un administrador pueden cancelar esta solicitud.");

        request.Cancel();

        var pendingTasks = await _db.ApprovalTasks
            .Where(t => t.RequestId == request.Id && t.Status == ApprovalTaskStatus.Pending)
            .ToListAsync(cancellationToken);
        foreach (var task in pendingTasks)
            task.Skip();

        _db.AuditEvents.Add(AuditEvent.Create(tenantId, request.Id, _currentUser.UserId, AuditEventType.RequestCancelled));

        await _db.SaveChangesAsync(cancellationToken);
    }
}

using Application.Common.Interfaces;
using Application.Common.Mediator;
using Domain.Common;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Application.Users.Commands;

public sealed class CreateTenantUserCommandHandler(IIdentityService identityService, IAppDbContext db)
    : IRequestHandler<CreateTenantUserCommand, Result<CreatedUserResponse>>
{
    public async Task<Result<CreatedUserResponse>> Handle(
        CreateTenantUserCommand request,
        CancellationToken cancellationToken)
    {
        // Tenant is globally visible (no query filter) and the caller is SuperAdmin,
        // so this sees the target tenant regardless of tenant context.
        var tenant = await db.Tenants
            .FirstOrDefaultAsync(t => t.Id == request.TenantId, cancellationToken);

        if (tenant is null)
            return Error.NotFound(nameof(Tenant), request.TenantId);

        // CreateAsync and AddToRoleAsync are two writes on the shared context; wrap
        // them so a failed role assignment cannot leave a roleless orphaned user.
        await using var transaction = await db.BeginTransactionAsync(cancellationToken);

        var result = await identityService.CreateUserAsync(
            request.Email, request.TenantId, request.Role, cancellationToken);

        if (result.IsFailure)
        {
            await transaction.RollbackAsync(cancellationToken);
            return result.Error;
        }

        await transaction.CommitAsync(cancellationToken);

        var created = result.Value!;
        return new CreatedUserResponse(created.UserId, request.Email, request.Role, created.TemporaryPassword);
    }
}

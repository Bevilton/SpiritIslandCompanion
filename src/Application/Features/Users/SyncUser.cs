using Application.Abstractions;
using Application.Behaviour;
using Application.Data;
using Domain.Errors;
using Domain.Models.User;
using Domain.Results;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Users;

/// <summary>
/// Syncs the OIDC user to the local database by email, creating the account on first
/// sign-in. Returns the local UserId and whether the account has chosen a name yet.
/// <para>
/// The nickname is deliberately not touched here. It belongs to the user, not to the
/// identity provider — writing the provider's claim back on every sign-in would quietly
/// undo whatever they set on the account.
/// </para>
/// </summary>
public sealed record SyncUserCommand(string Email) : ICommand<SyncUserResponse>;

/// <param name="NeedsNickname">
/// True while the account has no name of its own — the sign-in flow prompts for one.
/// </param>
public sealed record SyncUserResponse(Guid UserId, bool NeedsNickname);

internal sealed class SyncUserValidator : AbstractValidator<SyncUserCommand>
{
    public SyncUserValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithDomainError(DomainErrors.User.EmailRequired)
            .MaximumTrimmedLength(Email.MaxLength).WithDomainError(DomainErrors.User.EmailTooLong)
            .EmailAddress().WithDomainError(DomainErrors.User.EmailInvalid);
    }
}

internal sealed class SyncUserHandler(IAppDbContext db, TimeProvider clock) : ICommandHandler<SyncUserCommand, SyncUserResponse>
{
    public async Task<Result<SyncUserResponse>> Handle(SyncUserCommand request, CancellationToken cancellationToken)
    {
        var email = Email.Create(request.Email);
        if (email.IsFailure)
            return Result.Failure<SyncUserResponse>(email.Error);

        // Matched in the address's canonical form (see Email.Create): the identity provider's
        // spelling of the address may differ in case from one sign-in to the next.
        var address = email.Value;
        var user = await db.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Email == address, cancellationToken);

        if (user is not null)
            return new SyncUserResponse(user.Id.Value, user.Nickname is null);

        // First login — create the account unnamed, and let the app ask.
        var newUser = User.Create(
            new UserId(Guid.NewGuid()),
            email.Value,
            nickname: null,
            clock.GetUtcNow());

        db.Users.Add(newUser);
        return new SyncUserResponse(newUser.Id.Value, NeedsNickname: true);
    }
}

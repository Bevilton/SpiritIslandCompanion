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
/// Syncs the OIDC user to the local database by email. If the user doesn't exist, creates them.
/// If they do exist, updates the nickname from the latest OIDC claims.
/// Returns the local UserId.
/// </summary>
public sealed record SyncUserCommand(
    string Email,
    string Nickname) : IQuery<SyncUserResponse>;

public sealed record SyncUserResponse(Guid UserId);

internal sealed class SyncUserValidator : AbstractValidator<SyncUserCommand>
{
    public SyncUserValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithDomainError(DomainErrors.User.EmailRequired)
            .EmailAddress().WithDomainError(DomainErrors.User.EmailInvalid);
        RuleFor(x => x.Nickname)
            .NotEmpty().WithDomainError(DomainErrors.User.NicknameRequired)
            .MaximumLength(Nickname.MaxLength).WithDomainError(DomainErrors.User.NicknameTooLong);
    }
}

internal sealed class SyncUserHandler(IAppDbContext db) : IQueryHandler<SyncUserCommand, SyncUserResponse>
{
    public async Task<Result<SyncUserResponse>> Handle(SyncUserCommand request, CancellationToken cancellationToken)
    {
        var emailResult = Email.Create(request.Email);
        if (emailResult.IsFailure) return Result.Failure<SyncUserResponse>(emailResult.Error);

        var nicknameResult = Nickname.Create(request.Nickname);
        if (nicknameResult.IsFailure) return Result.Failure<SyncUserResponse>(nicknameResult.Error);

        var user = await db.Users
            .Include(u => u.UserSettings)
            .FirstOrDefaultAsync(u => u.Email.Value == request.Email, cancellationToken);

        if (user is not null)
        {
            user.UpdateProfile(nicknameResult.Value);
            return new SyncUserResponse(user.Id.Value);
        }

        // First login — create user
        var settings = UserSettings.Create(new UserSettingsId(Guid.NewGuid()), []);

        var newUser = User.Create(
            new UserId(Guid.NewGuid()),
            emailResult.Value,
            nicknameResult.Value,
            settings,
            DateTimeOffset.UtcNow);

        db.Users.Add(newUser);
        return new SyncUserResponse(newUser.Id.Value);
    }
}

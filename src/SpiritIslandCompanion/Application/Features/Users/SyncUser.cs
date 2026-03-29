using Application.Abstractions;
using Application.Data;
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
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Nickname).NotEmpty().MaximumLength(50);
    }
}

internal sealed class SyncUserHandler(IAppDbContext db) : IQueryHandler<SyncUserCommand, SyncUserResponse>
{
    public async Task<Result<SyncUserResponse>> Handle(SyncUserCommand request, CancellationToken cancellationToken)
    {
        var email = Email.Create(request.Email);

        var user = await db.Users
            .Include(u => u.UserSettings)
            .FirstOrDefaultAsync(u => u.Email == email, cancellationToken);

        if (user is not null)
        {
            user.UpdateProfile(Nickname.Create(request.Nickname));
            return new SyncUserResponse(user.Id.Value);
        }

        // First login — create user
        var settings = UserSettings.Create(new UserSettingsId(Guid.NewGuid()), []);

        var newUser = User.Create(
            new UserId(Guid.NewGuid()),
            email,
            Nickname.Create(request.Nickname),
            settings,
            DateTimeOffset.UtcNow);

        db.Users.Add(newUser);
        return new SyncUserResponse(newUser.Id.Value);
    }
}

using Application.Abstractions;
using Application.Data;
using Domain.Models.Static;
using Domain.Models.User;
using Domain.Results;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Users;

/// <summary>
/// Registers a new user manually. Typically not needed when using OIDC
/// (SyncUserCommand handles auto-registration), but available for admin/seeding scenarios.
/// </summary>
public sealed record RegisterUserCommand(
    string Email,
    string Nickname,
    List<string>? OwnedExpansionIds) : ICommand;

internal sealed class RegisterUserValidator : AbstractValidator<RegisterUserCommand>
{
    public RegisterUserValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Nickname).NotEmpty().MaximumLength(50);
    }
}

internal sealed class RegisterUserHandler(IAppDbContext db) : ICommandHandler<RegisterUserCommand>
{
    public async Task<Result> Handle(RegisterUserCommand request, CancellationToken cancellationToken)
    {
        var email = Email.Create(request.Email);

        var existingUser = await db.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Email == email, cancellationToken);

        if (existingUser is not null)
            return Result.Failure(Error.Conflict("User.AlreadyExists", "A user with this email already exists."));

        var expansions = request.OwnedExpansionIds?
            .Select(id => new ExpansionId(id)).ToList() ?? [];

        var settings = UserSettings.Create(new UserSettingsId(Guid.NewGuid()), expansions);

        var user = User.Create(
            new UserId(Guid.NewGuid()),
            email,
            Nickname.Create(request.Nickname),
            settings,
            DateTimeOffset.UtcNow);

        db.Users.Add(user);
        return Result.Success();
    }
}

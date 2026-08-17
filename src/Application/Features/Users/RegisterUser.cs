using Application.Abstractions;
using Application.Behaviour;
using Application.Data;
using Domain.Errors;
using Domain.Models.Static;
using Domain.Models.Static.Data;
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
        RuleFor(x => x.Email)
            .NotEmpty().WithDomainError(DomainErrors.User.EmailRequired)
            .EmailAddress().WithDomainError(DomainErrors.User.EmailInvalid);
        RuleFor(x => x.Nickname)
            .NotEmpty().WithDomainError(DomainErrors.User.NicknameRequired)
            .MaximumLength(Nickname.MaxLength).WithDomainError(DomainErrors.User.NicknameTooLong);
    }
}

internal sealed class RegisterUserHandler(IAppDbContext db) : ICommandHandler<RegisterUserCommand>
{
    public async Task<Result> Handle(RegisterUserCommand request, CancellationToken cancellationToken)
    {
        var emailResult = Email.Create(request.Email);
        if (emailResult.IsFailure) return Result.Failure(emailResult.Error);

        var nicknameResult = Nickname.Create(request.Nickname);
        if (nicknameResult.IsFailure) return Result.Failure(nicknameResult.Error);

        if (request.OwnedExpansionIds is not null &&
            request.OwnedExpansionIds.Any(id => GameData.Expansions.All(e => e.Id.Value != id)))
            return Result.Failure(DomainErrors.User.UnknownExpansion);

        var existingUser = await db.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Email.Value == request.Email, cancellationToken);

        if (existingUser is not null)
            return Result.Failure(Error.Conflict("User.AlreadyExists", "A user with this email already exists."));

        var expansions = request.OwnedExpansionIds?
            .Select(id => new ExpansionId(id)).ToList() ?? [];

        var settings = UserSettings.Create(new UserSettingsId(Guid.NewGuid()), expansions);

        var user = User.Create(
            new UserId(Guid.NewGuid()),
            emailResult.Value,
            nicknameResult.Value,
            settings,
            DateTimeOffset.UtcNow);

        db.Users.Add(user);
        return Result.Success();
    }
}

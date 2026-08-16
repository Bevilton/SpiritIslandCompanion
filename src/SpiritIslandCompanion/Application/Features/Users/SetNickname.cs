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
/// Names the account. Sent from the first-login prompt and from Settings afterwards —
/// the same command either way, because renaming later is the same act as naming.
/// </summary>
public sealed record SetNicknameCommand(Guid UserId, string Nickname) : ICommand;

internal sealed class SetNicknameValidator : AbstractValidator<SetNicknameCommand>
{
    public SetNicknameValidator()
    {
        RuleFor(x => x.Nickname)
            .NotEmpty().WithDomainError(DomainErrors.User.NicknameRequired)
            .MaximumLength(Nickname.MaxLength).WithDomainError(DomainErrors.User.NicknameTooLong);
    }
}

internal sealed class SetNicknameHandler(IAppDbContext db) : ICommandHandler<SetNicknameCommand>
{
    public async Task<Result> Handle(SetNicknameCommand request, CancellationToken cancellationToken)
    {
        var nicknameResult = Nickname.Create(request.Nickname.Trim());
        if (nicknameResult.IsFailure)
            return Result.Failure(nicknameResult.Error);

        var user = await db.Users
            .FirstOrDefaultAsync(u => u.Id == new UserId(request.UserId), cancellationToken);

        if (user is null)
            return Result.Failure(DomainErrors.User.NotFound);

        user.UpdateProfile(nicknameResult.Value);
        return Result.Success();
    }
}

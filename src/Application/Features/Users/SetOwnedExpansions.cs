using Application.Abstractions;
using Application.Data;
using Domain.Errors;
using Domain.Models.Static;
using Domain.Models.User;
using Domain.Results;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Users;

/// <summary>
/// Replaces the account's shelf of expansions — what every picker in the app is narrowed to.
/// Sent from Settings, and named for the one thing it does, like <see cref="SetNicknameCommand"/>
/// beside it and <see cref="User.SetOwnedExpansions"/> underneath it.
/// </summary>
public sealed record SetOwnedExpansionsCommand(Guid UserId, List<string> OwnedExpansionIds) : ICommand;

internal sealed class SetOwnedExpansionsHandler(IAppDbContext db) : ICommandHandler<SetOwnedExpansionsCommand>
{
    public async Task<Result> Handle(SetOwnedExpansionsCommand request, CancellationToken cancellationToken)
    {
        var user = await db.Users
            .FirstOrDefaultAsync(u => u.Id == new UserId(request.UserId), cancellationToken);

        if (user is null)
            return Result.Failure(DomainErrors.User.NotFound);

        // Whether every id names a catalogue entry is the user's own rule.
        return user.SetOwnedExpansions(request.OwnedExpansionIds.Select(id => new ExpansionId(id)));
    }
}

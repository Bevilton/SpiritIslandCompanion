using Application.Abstractions;
using Application.Data;
using Domain.Errors;
using Domain.Models.Static;
using Domain.Models.Static.Data;
using Domain.Models.User;
using Domain.Results;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Users;

public sealed record UpdateUserSettingsCommand(Guid UserId, List<string> OwnedExpansionIds) : ICommand;

internal sealed class UpdateUserSettingsHandler(IAppDbContext db) : ICommandHandler<UpdateUserSettingsCommand>
{
    public async Task<Result> Handle(UpdateUserSettingsCommand request, CancellationToken cancellationToken)
    {
        if (request.OwnedExpansionIds.Any(id => GameData.Expansions.All(e => e.Id.Value != id)))
            return Result.Failure(DomainErrors.User.UnknownExpansion);

        var user = await db.Users
            .Include(u => u.UserSettings)
            .FirstOrDefaultAsync(u => u.Id == new UserId(request.UserId), cancellationToken);

        if (user is null)
            return Result.Failure(Error.NotFound("User.NotFound", "User not found."));

        var expansions = request.OwnedExpansionIds
            .Select(id => new ExpansionId(id)).ToList();

        user.UserSettings.SetExpansions(expansions);
        return Result.Success();
    }
}

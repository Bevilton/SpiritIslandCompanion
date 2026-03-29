using Application.Abstractions;
using Application.Data;
using Domain.Models.Static;
using Domain.Models.User;
using Domain.Results;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Users;

public sealed record UpdateUserSettingsCommand(Guid UserId, List<string> OwnedExpansionIds) : ICommand;

internal sealed class UpdateUserSettingsValidator : AbstractValidator<UpdateUserSettingsCommand>
{
    public UpdateUserSettingsValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
    }
}

internal sealed class UpdateUserSettingsHandler(IAppDbContext db) : ICommandHandler<UpdateUserSettingsCommand>
{
    public async Task<Result> Handle(UpdateUserSettingsCommand request, CancellationToken cancellationToken)
    {
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

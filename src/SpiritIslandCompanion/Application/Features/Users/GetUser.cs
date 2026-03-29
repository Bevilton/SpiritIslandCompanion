using Application.Abstractions;
using Application.Data;
using Domain.Models.User;
using Domain.Results;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Users;

public sealed record GetUserQuery(Guid UserId) : IQuery<GetUserResponse>;

public sealed record GetUserResponse(
    Guid Id,
    string Email,
    string Nickname,
    DateTimeOffset Registered,
    List<string> OwnedExpansionIds);

internal sealed class GetUserHandler(IAppDbContext db) : IQueryHandler<GetUserQuery, GetUserResponse>
{
    public async Task<Result<GetUserResponse>> Handle(GetUserQuery request, CancellationToken cancellationToken)
    {
        var user = await db.Users
            .AsNoTracking()
            .Include(u => u.UserSettings)
            .FirstOrDefaultAsync(u => u.Id == new UserId(request.UserId), cancellationToken);

        if (user is null)
            return Result.Failure<GetUserResponse>(Error.NotFound("User.NotFound", "User not found."));

        var response = new GetUserResponse(
            user.Id.Value,
            user.Email.Value,
            user.Nickname.Value,
            user.Registered,
            user.UserSettings.Expansions.Select(e => e.Value).ToList());

        return response;
    }
}

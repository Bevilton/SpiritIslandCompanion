using Application.Abstractions;
using Application.Data;
using Domain.Errors;
using Domain.Models.User;
using Domain.Results;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Users;

public sealed record GetUserQuery(Guid UserId) : IQuery<GetUserResponse>;

/// <param name="Nickname">The name the user chose, or null while they haven't — see User.Nickname.</param>
/// <param name="DisplayName">What to print: the nickname, falling back to the email address.</param>
public sealed record GetUserResponse(
    Guid Id,
    string Email,
    string? Nickname,
    string DisplayName,
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
            return Result.Failure<GetUserResponse>(DomainErrors.User.NotFound);

        var response = new GetUserResponse(
            user.Id.Value,
            user.Email.Value,
            user.Nickname?.Value,
            user.DisplayName,
            user.Registered,
            user.UserSettings.Expansions.Select(e => e.Value).ToList());

        return response;
    }
}

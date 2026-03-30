using Application.Abstractions;
using Application.Data;
using Domain.Errors;
using Domain.Models.Friendship;
using Domain.Models.User;
using Domain.Results;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Friendships;

/// <summary>
/// Sends a friend request to another user by email.
/// </summary>
public sealed record SendFriendRequestCommand(Guid RequesterId, string AddresseeEmail) : ICommand;

internal sealed class SendFriendRequestValidator : AbstractValidator<SendFriendRequestCommand>
{
    public SendFriendRequestValidator()
    {
        RuleFor(x => x.RequesterId).NotEmpty();
        RuleFor(x => x.AddresseeEmail).NotEmpty().EmailAddress();
    }
}

internal sealed class SendFriendRequestHandler(IAppDbContext db) : ICommandHandler<SendFriendRequestCommand>
{
    public async Task<Result> Handle(SendFriendRequestCommand request, CancellationToken cancellationToken)
    {
        var requesterId = new UserId(request.RequesterId);

        // Find the addressee by email
        var addressee = await db.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Email.Value == request.AddresseeEmail, cancellationToken);

        if (addressee is null)
            return Result.Failure(Error.NotFound("User.NotFound", "No user found with that email."));

        var addresseeId = addressee.Id;

        // Check if a friendship already exists in either direction
        var existing = await db.Friendships
            .AsNoTracking()
            .FirstOrDefaultAsync(f =>
                (f.RequesterId.Value == requesterId.Value && f.AddresseeId.Value == addresseeId.Value) ||
                (f.RequesterId.Value == addresseeId.Value && f.AddresseeId.Value == requesterId.Value),
                cancellationToken);

        if (existing is not null)
            return Result.Failure(DomainErrors.Friendship.AlreadyExists);

        var friendshipResult = Friendship.Create(
            new FriendshipId(Guid.NewGuid()),
            requesterId,
            addresseeId);

        if (friendshipResult.IsFailure)
            return Result.Failure(friendshipResult.Error);

        db.Friendships.Add(friendshipResult.Value);
        return Result.Success();
    }
}

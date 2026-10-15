using Application.Abstractions;
using Application.Behaviour;
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
        RuleFor(x => x.AddresseeEmail)
            .NotEmpty().WithDomainError(DomainErrors.User.EmailRequired)
            .MaximumTrimmedLength(Email.MaxLength).WithDomainError(DomainErrors.User.EmailTooLong)
            .EmailAddress().WithDomainError(DomainErrors.User.EmailInvalid);
    }
}

internal sealed class SendFriendRequestHandler(IAppDbContext db, TimeProvider clock) : ICommandHandler<SendFriendRequestCommand>
{
    public async Task<Result> Handle(SendFriendRequestCommand request, CancellationToken cancellationToken)
    {
        // Looked up in the address's canonical form (see Email.Create), so "Anna@…" finds anna.
        var email = Email.Create(request.AddresseeEmail);
        if (email.IsFailure)
            return Result.Failure(email.Error);

        var address = email.Value;
        var addressee = await db.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Email == address, cancellationToken);

        if (addressee is null)
            return Result.Failure(DomainErrors.User.NotFoundByEmail);

        // Written before anything is looked up about the two of them: what the request itself
        // refuses — asking yourself — is answered as such, not as a request that already exists.
        var requesterId = new UserId(request.RequesterId);
        var friendship = Friendship.Create(
            new FriendshipId(Guid.NewGuid()),
            requesterId,
            addressee.Id,
            clock.GetUtcNow());

        if (friendship.IsFailure)
            return Result.Failure(friendship.Error);

        // One row between two people, whichever of them asked and however it stands. A request
        // turned down leaves none behind (see RejectFriendRequest), so people who fall out and
        // patch things up are never blocked by an old answer.
        var exists = await db.Friendships
            .Between(requesterId, addressee.Id)
            .AnyAsync(cancellationToken);

        if (exists)
            return Result.Failure(DomainErrors.Friendship.AlreadyExists);

        db.Friendships.Add(friendship.Value);
        return Result.Success();
    }
}

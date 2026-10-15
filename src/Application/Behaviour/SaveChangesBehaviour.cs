using Application.Abstractions;
using Application.Data;
using Domain.Results;
using MediatR;

namespace Application.Behaviour;

/// <summary>
/// Commits whatever a command changed, and only when it reports success — then forgets it.
/// Queries never come through here: the constraint on <typeparamref name="TRequest"/> sees
/// to that, and they read untracked anyway.
/// <para>
/// The context is scoped, and in Blazor Server a scope is the whole circuit, so the context
/// outlives any one command. Anything a command left tracked would leak into the next one in
/// the same circuit: a failed command's half-made changes would be saved as if they belonged
/// to the next successful one, and a successful command's entities would come back from the
/// next command's load as the tracked instances — stale, if another user changed the row in
/// between. (A friend request this circuit sent and still holds as pending, accepted on the
/// other side meanwhile: removing the friendship would then skip splitting the shared games.)
/// So every command ends with the tracker cleared, whichever way it ended, and each one reads
/// what the database holds now. A command is the unit of work, not the circuit.
/// </para>
/// </summary>
public sealed class SaveChangesBehaviour<TRequest, TResponse>(IAppDbContext db)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IBaseCommand, IRequest<TResponse>
    where TResponse : Result
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        try
        {
            var result = await next(cancellationToken);

            if (result.IsSuccess)
                await db.SaveChangesAsync(cancellationToken);

            return result;
        }
        finally
        {
            db.DiscardChanges();
        }
    }
}

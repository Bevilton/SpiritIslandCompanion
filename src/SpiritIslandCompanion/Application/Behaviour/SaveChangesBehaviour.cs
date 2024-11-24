using Domain.Data;
using Domain.Results;
using MediatR;

namespace Application.Behaviour;

public class SaveChangesBehaviour<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse> where TRequest : IRequest<TResponse> where TResponse : Result
{
    private readonly IAppDbContext _dbContext;

    public SaveChangesBehaviour(IAppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var result = await next();

        if (result.IsSuccess)
            await _dbContext.SaveChangesAsync(cancellationToken);

        return result;
    }
}
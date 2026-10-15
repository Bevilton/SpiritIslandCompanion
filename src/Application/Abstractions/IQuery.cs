using Domain.Results;
using MediatR;

namespace Application.Abstractions;

/// <summary>A request that reads and changes nothing.</summary>
public interface IQuery<TResponse> : IRequest<Result<TResponse>>;

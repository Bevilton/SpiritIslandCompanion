using Domain.Results;
using MediatR;

namespace Application.Abstractions;

/// <summary>
/// What every command is, whatever it answers with: a request that changes state. The seam
/// the pipeline keys on to save after a command and never after a query.
/// </summary>
public interface IBaseCommand;

/// <summary>A request that changes state and reports only whether it succeeded.</summary>
public interface ICommand : IRequest<Result>, IBaseCommand;

/// <summary>
/// A request that changes state and hands something back — the id it settled on, say. Rare
/// on purpose: most commands have nothing to say beyond success, and a pure reader is a
/// <see cref="IQuery{TResponse}"/>.
/// </summary>
public interface ICommand<TResponse> : IRequest<Result<TResponse>>, IBaseCommand;

using Domain.Results;
using MediatR;

namespace Application.Abstractions;

public interface ICommandHandler<TCommand> : IRequestHandler<TCommand, Result> where TCommand : ICommand;
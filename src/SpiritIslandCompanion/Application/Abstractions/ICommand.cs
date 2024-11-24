using Domain.Results;
using MediatR;

namespace Application.Abstractions;

public interface ICommand : IRequest<Result>
{

}
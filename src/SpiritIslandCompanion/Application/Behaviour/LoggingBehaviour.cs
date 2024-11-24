using System.Text.Json;
using Domain.Results;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.Behaviour;

public class LoggingBehaviour<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse> where TRequest : IRequest<TResponse> where TResponse : Result
{
    private readonly ILogger<LoggingBehaviour<TRequest, TResponse>> _logger;

    public LoggingBehaviour(ILogger<LoggingBehaviour<TRequest, TResponse>> logger)
    {
        _logger = logger;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var requestString = JsonSerializer.Serialize(request);
        var requestName = typeof(TRequest).Name;

        _logger.LogInformation("Handling '{RequestName}' with request '{Request}'", requestName, requestString);
        var response = await next();

        if (response.IsFailure)
            _logger.LogError("Request '{RequestName}' failed with error: '{ErrorType}' {ErrorMessage}", requestName, response.Error.Code, response.Error.Message);
        else
            _logger.LogInformation("Handled '{RequestName}'", requestName);

        return response;
    }
}
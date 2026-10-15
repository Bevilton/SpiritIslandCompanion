using System.Text.Json;
using Domain.Results;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.Behaviour;

/// <summary>
/// One line in and one line out for every request. The payload itself is only serialised when
/// Debug logging is on: it can carry a note, an email address or a whole island arrangement,
/// none of which belongs in an Information log by default.
/// </summary>
public sealed class LoggingBehaviour<TRequest, TResponse>(ILogger<LoggingBehaviour<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
    where TResponse : Result
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;

        if (logger.IsEnabled(LogLevel.Debug))
            logger.LogDebug("Handling {RequestName} {Request}", requestName, JsonSerializer.Serialize(request));
        else
            logger.LogInformation("Handling {RequestName}", requestName);

        var response = await next(cancellationToken);

        if (response.IsFailure)
            logger.LogWarning("{RequestName} failed: {ErrorCode} {ErrorMessage}", requestName, response.Error.Code, response.Error.Message);
        else
            logger.LogInformation("Handled {RequestName}", requestName);

        return response;
    }
}

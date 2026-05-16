namespace Domain.Results;

public sealed record ValidationError(string Property, string Code, string Message);

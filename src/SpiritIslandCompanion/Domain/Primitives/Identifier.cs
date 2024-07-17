namespace Domain.Primitives;

public abstract record Identifier(Guid Value) : ValueObject;
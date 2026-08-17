using Domain.Primitives;

namespace Domain.Models.PlayerMerge;

public record PlayerMergeRequestId(Guid Value) : Identifier(Value);

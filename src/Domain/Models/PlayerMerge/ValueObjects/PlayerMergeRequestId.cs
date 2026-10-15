using Domain.Primitives;

namespace Domain.Models.PlayerMerge;

public sealed record PlayerMergeRequestId(Guid Value) : Identifier(Value);

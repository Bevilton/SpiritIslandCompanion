using Domain.Primitives;

namespace Domain.Models.IslandLayout;

public sealed record CustomIslandLayoutId(Guid Value) : Identifier(Value);

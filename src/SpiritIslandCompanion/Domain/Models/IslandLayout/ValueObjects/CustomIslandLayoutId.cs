using Domain.Primitives;

namespace Domain.Models.IslandLayout;

public record CustomIslandLayoutId(Guid Value) : Identifier(Value);

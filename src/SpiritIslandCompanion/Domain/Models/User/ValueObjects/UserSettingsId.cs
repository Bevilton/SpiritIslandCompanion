using Domain.Primitives;

namespace Domain.Models.User;

public record UserSettingsId(Guid Value) : Identifier(Value);
namespace Domain.Common;

public record Error(string Code, string Description)
{
    public static readonly Error None = new(string.Empty, string.Empty);
    public static readonly Error NullValue = new("General.Null", "A null value was provided.");

    public static Error NotFound(string entity, object id) =>
        new($"{entity}.NotFound", $"{entity} with id '{id}' was not found.");

    public static Error Conflict(string entity) =>
        new($"{entity}.Conflict", $"{entity} already exists.");

    public static Error Validation(string field, string message) =>
        new($"Validation.{field}", message);
}

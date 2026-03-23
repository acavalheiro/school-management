using Domain.Common;

namespace Domain.Entities;

public sealed class Tenant : Entity
{
    public string Name { get; private set; } = string.Empty;
    public DateTime CreatedAt { get; private set; }

    // EF Core constructor
    private Tenant() { }

    public static Result<Tenant> Create(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Error.Validation(nameof(Name), "Tenant name is required.");

        return Result<Tenant>.Success(new Tenant
        {
            Name = name.Trim(),
            CreatedAt = DateTime.UtcNow
        });
    }

    public Result Rename(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Error.Validation(nameof(Name), "Tenant name is required.");

        Name = name.Trim();
        return Result.Success();
    }
}

namespace FamilyDose.Api.Models;

public sealed class Child
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public required string Name { get; set; }

    public DateOnly DateOfBirth { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; }
        = DateTimeOffset.UtcNow;

    public bool IsActive { get; set; } = true;
}

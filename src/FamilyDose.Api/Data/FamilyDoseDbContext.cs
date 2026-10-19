using FamilyDose.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace FamilyDose.Api.Data;

public sealed class FamilyDoseDbContext(
    DbContextOptions<FamilyDoseDbContext> options)
    : DbContext(options)
{
    public DbSet<Child> Children => Set<Child>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Child>(entity =>
        {
            entity.HasKey(child => child.Id);

            entity.Property(child => child.Name)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(child => child.DateOfBirth)
                .IsRequired();

            entity.Property(child => child.CreatedAtUtc)
                .IsRequired();

            entity.Property(child => child.IsActive)
                .HasDefaultValue(true);
        });
    }
}

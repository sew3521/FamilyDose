using FamilyDose.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace FamilyDose.Api.Data;

public sealed class FamilyDoseDbContext(
    DbContextOptions<FamilyDoseDbContext> options)
    : DbContext(options)
{
    public DbSet<Child> Children => Set<Child>();

    public DbSet<Medication> Medications => Set<Medication>();

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

        modelBuilder.Entity<Medication>(entity =>
        {
            entity.HasKey(medication => medication.Id);

            // A medication belongs to one child. Never automatically
            // delete medications when a child profile is deleted.
            entity.HasOne(medication => medication.Child)
                .WithMany()
                .HasForeignKey(medication => medication.ChildId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(medication => medication.ChildId);

            entity.Property(medication => medication.Name)
                .IsRequired()
                .HasMaxLength(150);

            // Preserve exact decimal values. SQLite has no native
            // decimal type, and medication amounts must not be
            // rounded through floating-point conversion.
            entity.Property(medication => medication.DoseAmount)
                .HasConversion<string>();

            entity.Property(medication => medication.DoseUnit)
                .IsRequired()
                .HasMaxLength(30);

            entity.Property(medication => medication.Concentration)
                .HasMaxLength(150);

            entity.Property(medication => medication.ReferenceUrl)
                .HasMaxLength(2048);

            entity.Property(medication => medication.ScheduleMode)
                .HasConversion<int>();

            entity.Property(medication => medication.IsActive)
                .HasDefaultValue(true);
        });
    }
}

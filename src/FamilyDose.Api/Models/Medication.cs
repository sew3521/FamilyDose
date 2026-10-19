namespace FamilyDose.Api.Models;

/// <summary>
/// Medication instructions configured by the parent.
///
/// FamilyDose tracks these instructions but does not determine
/// whether a medication or dosage is medically appropriate.
/// </summary>
public sealed class Medication
{
    public Guid Id { get; set; } = Guid.NewGuid();

    // Every medication belongs to exactly one child.
    public Guid ChildId { get; set; }

    public Child Child { get; set; } = null!;

    public required string Name { get; set; }

    public decimal DoseAmount { get; set; }

    public required string DoseUnit { get; set; }

    // Optional description, such as "160 mg / 5 mL".
    public string? Concentration { get; set; }

    // Minimum elapsed minutes between administrations.
    public int MinimumIntervalMinutes { get; set; }

    // Maximum administrations in any rolling 24-hour period.
    public int MaxDosesPer24Hours { get; set; }

    public MedicationScheduleMode ScheduleMode { get; set; }

    public string? ReferenceUrl { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTimeOffset CreatedAtUtc { get; set; }
        = DateTimeOffset.UtcNow;
}

/// <summary>
/// Scheduled medications have specific administration times.
/// As-needed medications only enforce configured eligibility rules.
/// </summary>
public enum MedicationScheduleMode
{
    AsNeeded = 0,
    Scheduled = 1
}

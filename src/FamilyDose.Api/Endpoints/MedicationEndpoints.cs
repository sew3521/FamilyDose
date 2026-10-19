using FamilyDose.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace FamilyDose.Api.Endpoints;

/// <summary>
/// API endpoints for medication configurations.
///
/// These endpoints manage instructions entered by a caregiver.
/// They do not determine whether a medication or dosage is safe.
/// </summary>
public static class MedicationEndpoints
{
    public static IEndpointRouteBuilder MapMedicationEndpoints(
        this IEndpointRouteBuilder app)
    {
        // List medications belonging to one child.
        var childGroup = app.MapGroup("/api/children/{childId:guid}/medications")
            .WithTags("Medications");

        childGroup.MapGet("/", async (
            Guid childId,
            FamilyDoseDbContext db,
            CancellationToken cancellationToken) =>
        {
            // Return 404 if the requested child does not exist.
            var childExists = await db.Children.AnyAsync(
                child => child.Id == childId,
                cancellationToken);

            if (!childExists)
            {
                return Results.NotFound();
            }

            var medications = await db.Medications
                .AsNoTracking()
                .Where(medication => medication.ChildId == childId)
                .OrderBy(medication => medication.Name)
                .Select(medication => new
                {
                    medication.Id,
                    medication.ChildId,
                    medication.Name,
                    medication.DoseAmount,
                    medication.DoseUnit,
                    medication.Concentration,
                    medication.MinimumIntervalMinutes,
                    medication.MaxDosesPer24Hours,
                    medication.ScheduleMode,
                    medication.ReferenceUrl,
                    medication.IsActive
                })
                .ToListAsync(cancellationToken);

            return Results.Ok(medications);
        });

        // Retrieve one medication by its unique identifier.
        app.MapGet("/api/medications/{id:guid}", async (
            Guid id,
            FamilyDoseDbContext db,
            CancellationToken cancellationToken) =>
        {
            var medication = await db.Medications
                .AsNoTracking()
                .Where(medication => medication.Id == id)
                .Select(medication => new
                {
                    medication.Id,
                    medication.ChildId,
                    medication.Name,
                    medication.DoseAmount,
                    medication.DoseUnit,
                    medication.Concentration,
                    medication.MinimumIntervalMinutes,
                    medication.MaxDosesPer24Hours,
                    medication.ScheduleMode,
                    medication.ReferenceUrl,
                    medication.IsActive
                })
                .FirstOrDefaultAsync(cancellationToken);

            return medication is null
                ? Results.NotFound()
                : Results.Ok(medication);
        })
        .WithTags("Medications");


        // Store caregiver-entered medication instructions.
        // These checks validate data, not medical appropriateness.
        childGroup.MapPost("/", async (
            Guid childId,
            CreateMedicationRequest request,
            FamilyDoseDbContext db,
            CancellationToken cancellationToken) =>
        {
            var child = await db.Children
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    child => child.Id == childId,
                    cancellationToken);

            if (child is null)
            {
                return Results.NotFound();
            }

            if (!child.IsActive)
            {
                return Results.Conflict(new
                {
                    error = "Medications cannot be added to archived children."
                });
            }

            var name = request.Name?.Trim();
            var unit = request.DoseUnit?.Trim();

            if (string.IsNullOrWhiteSpace(name) || name.Length > 150)
            {
                return Results.BadRequest(new
                {
                    error = "Medication name must contain 1 to 150 characters."
                });
            }

            if (request.DoseAmount <= 0)
            {
                return Results.BadRequest(new
                {
                    error = "Dose amount must be greater than zero."
                });
            }

            if (string.IsNullOrWhiteSpace(unit) || unit.Length > 30)
            {
                return Results.BadRequest(new
                {
                    error = "Dose unit must contain 1 to 30 characters."
                });
            }

            if (request.MinimumIntervalMinutes <= 0)
            {
                return Results.BadRequest(new
                {
                    error = "Minimum interval must be greater than zero."
                });
            }

            if (request.MaxDosesPer24Hours <= 0)
            {
                return Results.BadRequest(new
                {
                    error = "Maximum doses per 24 hours must be greater than zero."
                });
            }

            if (!Enum.IsDefined(request.ScheduleMode))
            {
                return Results.BadRequest(new
                {
                    error = "Invalid medication schedule mode."
                });
            }

            var concentration = request.Concentration?.Trim();
            var referenceUrl = request.ReferenceUrl?.Trim();

            if (concentration?.Length > 150 ||
                referenceUrl?.Length > 2048)
            {
                return Results.BadRequest(new
                {
                    error = "Medication description or reference URL is too long."
                });
            }

            if (!string.IsNullOrEmpty(referenceUrl) &&
                (!Uri.TryCreate(referenceUrl, UriKind.Absolute, out var uri) ||
                 (uri.Scheme != Uri.UriSchemeHttp &&
                  uri.Scheme != Uri.UriSchemeHttps)))
            {
                return Results.BadRequest(new
                {
                    error = "Reference URL must be a valid HTTP or HTTPS address."
                });
            }

            var medication = new FamilyDose.Api.Models.Medication
            {
                ChildId = childId,
                Name = name,
                DoseAmount = request.DoseAmount,
                DoseUnit = unit,
                Concentration = concentration,
                MinimumIntervalMinutes = request.MinimumIntervalMinutes,
                MaxDosesPer24Hours = request.MaxDosesPer24Hours,
                ScheduleMode = request.ScheduleMode,
                ReferenceUrl = referenceUrl
            };

            db.Medications.Add(medication);
            await db.SaveChangesAsync(cancellationToken);

            return Results.Created(
                $"/api/medications/{medication.Id}",
                new
                {
                    medication.Id,
                    medication.ChildId,
                    medication.Name,
                    medication.DoseAmount,
                    medication.DoseUnit,
                    medication.Concentration,
                    medication.MinimumIntervalMinutes,
                    medication.MaxDosesPer24Hours,
                    medication.ScheduleMode,
                    medication.ReferenceUrl,
                    medication.IsActive
                });
        });

        return app;
    }
}

public sealed record CreateMedicationRequest(
    string? Name,
    decimal DoseAmount,
    string? DoseUnit,
    string? Concentration,
    int MinimumIntervalMinutes,
    int MaxDosesPer24Hours,
    FamilyDose.Api.Models.MedicationScheduleMode ScheduleMode,
    string? ReferenceUrl);

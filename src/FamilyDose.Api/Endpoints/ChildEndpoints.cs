using FamilyDose.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace FamilyDose.Api.Endpoints;

public static class ChildEndpoints
{
    public static IEndpointRouteBuilder MapChildEndpoints(
        this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/children")
            .WithTags("Children");

        group.MapGet("/", async (
            FamilyDoseDbContext db,
            CancellationToken cancellationToken) =>
        {
            var children = await db.Children
                .AsNoTracking()
                .OrderBy(child => child.Name)
                .Select(child => new
                {
                    child.Id,
                    child.Name,
                    child.DateOfBirth,
                    child.IsActive
                })
                .ToListAsync(cancellationToken);

            return Results.Ok(children);
        });

        group.MapGet("/{id:guid}", async (
            Guid id,
            FamilyDoseDbContext db,
            CancellationToken cancellationToken) =>
        {
            var child = await db.Children
                .AsNoTracking()
                .Where(child => child.Id == id)
                .Select(child => new
                {
                    child.Id,
                    child.Name,
                    child.DateOfBirth,
                    child.IsActive
                })
                .FirstOrDefaultAsync(cancellationToken);

            return child is null
                ? Results.NotFound()
                : Results.Ok(child);
        });


        group.MapPost("/", async (
            CreateChildRequest request,
            FamilyDoseDbContext db,
            TimeProvider clock,
            CancellationToken cancellationToken) =>
        {
            var name = request.Name?.Trim();

            if (string.IsNullOrWhiteSpace(name))
            {
                return Results.BadRequest(new
                {
                    error = "Child name is required."
                });
            }

            if (name.Length > 100)
            {
                return Results.BadRequest(new
                {
                    error = "Child name cannot exceed 100 characters."
                });
            }

            if (request.DateOfBirth is null ||
                request.DateOfBirth > DateOnly.FromDateTime(
                    clock.GetUtcNow().UtcDateTime))
            {
                return Results.BadRequest(new
                {
                    error = "A valid date of birth is required."
                });
            }

            var child = new FamilyDose.Api.Models.Child
            {
                Name = name,
                DateOfBirth = request.DateOfBirth.Value
            };

            db.Children.Add(child);

            await db.SaveChangesAsync(cancellationToken);

            return Results.Created(
                $"/api/children/{child.Id}",
                new
                {
                    child.Id,
                    child.Name,
                    child.DateOfBirth,
                    child.IsActive
                });
        });


        // Update an existing child without changing its identity
        // or any medication history associated with the profile.
        group.MapPut("/{id:guid}", async (
            Guid id,
            UpdateChildRequest request,
            FamilyDoseDbContext db,
            TimeProvider clock,
            CancellationToken cancellationToken) =>
        {
            var child = await db.Children.FindAsync(
                [id], cancellationToken);

            if (child is null)
            {
                return Results.NotFound();
            }

            var name = request.Name?.Trim();

            if (string.IsNullOrWhiteSpace(name))
            {
                return Results.BadRequest(new
                {
                    error = "Child name is required."
                });
            }

            if (name.Length > 100)
            {
                return Results.BadRequest(new
                {
                    error = "Child name cannot exceed 100 characters."
                });
            }

            var today = DateOnly.FromDateTime(
                clock.GetUtcNow().UtcDateTime);

            if (request.DateOfBirth is null ||
                request.DateOfBirth.Value == default ||
                request.DateOfBirth.Value > today)
            {
                return Results.BadRequest(new
                {
                    error = "A valid date of birth is required."
                });
            }

            child.Name = name;
            child.DateOfBirth = request.DateOfBirth.Value;

            await db.SaveChangesAsync(cancellationToken);

            return Results.Ok(new
            {
                child.Id,
                child.Name,
                child.DateOfBirth,
                child.IsActive
            });
        });


        // Archiving hides a profile from normal medication tracking
        // without destroying its history or changing its identity.
        group.MapPost("/{id:guid}/archive", async (
            Guid id,
            FamilyDoseDbContext db,
            CancellationToken cancellationToken) =>
        {
            var child = await db.Children.FindAsync(
                [id], cancellationToken);

            if (child is null)
            {
                return Results.NotFound();
            }

            child.IsActive = false;
            await db.SaveChangesAsync(cancellationToken);

            return Results.NoContent();
        });

        // Restoring makes an archived profile available again.
        group.MapPost("/{id:guid}/restore", async (
            Guid id,
            FamilyDoseDbContext db,
            CancellationToken cancellationToken) =>
        {
            var child = await db.Children.FindAsync(
                [id], cancellationToken);

            if (child is null)
            {
                return Results.NotFound();
            }

            child.IsActive = true;
            await db.SaveChangesAsync(cancellationToken);

            return Results.NoContent();
        });


        // Permanently delete a child profile.
        //
        // IMPORTANT: Before medication records are introduced,
        // this endpoint must be extended to reject deletion when
        // dependent records exist. Never cascade-delete medication
        // administration history.
        group.MapDelete("/{id:guid}", async (
            Guid id,
            FamilyDoseDbContext db,
            CancellationToken cancellationToken) =>
        {
            var child = await db.Children.FindAsync(
                [id], cancellationToken);

            if (child is null)
            {
                return Results.NotFound();
            }

            db.Children.Remove(child);
            await db.SaveChangesAsync(cancellationToken);

            return Results.NoContent();
        });

        return app;
    }
}

public sealed record CreateChildRequest(
    string? Name,
    DateOnly? DateOfBirth);

public sealed record UpdateChildRequest(
    string? Name,
    DateOnly? DateOfBirth);

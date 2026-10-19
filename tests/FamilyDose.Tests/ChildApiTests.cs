using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using FamilyDose.Api.Models;
using FamilyDose.Api.Data;
using System.Net;
using System.Net.Http.Json;
using FamilyDose.Tests.Infrastructure;

namespace FamilyDose.Tests;

public sealed class ChildApiTests
{
    [Fact]
    public async Task GetChildren_WhenDatabaseIsEmpty_ReturnsEmptyList()
    {
        // Arrange: start the real API with an isolated SQLite database.
        using var factory = new FamilyDoseApiFactory();

        await factory.InitializeDatabaseAsync();

        using var client = factory.CreateClient();

        // Act: request the list of child profiles.
        var response = await client.GetAsync("/api/children/");

        // Assert: a new household should contain no children.
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var children = await response.Content
            .ReadFromJsonAsync<List<ChildResponse>>();

        Assert.NotNull(children);
        Assert.Empty(children);
    }


    [Fact]
    public async Task CreateChild_WithValidData_ReturnsCreated()
    {
        using var factory = new FamilyDoseApiFactory();
        await factory.InitializeDatabaseAsync();

        using var client = factory.CreateClient();

        var request = new
        {
            Name = "Test Child",
            DateOfBirth = "2024-01-15"
        };

        var response = await client.PostAsJsonAsync(
            "/api/children/", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var child = await response.Content
            .ReadFromJsonAsync<ChildResponse>();

        Assert.NotNull(child);
        Assert.NotEqual(Guid.Empty, child.Id);
        Assert.Equal("Test Child", child.Name);
        Assert.True(child.IsActive);

        // Verify the record was actually saved to SQLite.
        var saved = await client.GetFromJsonAsync<ChildResponse>(
            $"/api/children/{child.Id}");

        Assert.NotNull(saved);
        Assert.Equal(child.Id, saved.Id);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task CreateChild_WithBlankName_ReturnsBadRequest(
        string name)
    {
        using var factory = new FamilyDoseApiFactory();
        await factory.InitializeDatabaseAsync();

        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/children/",
            new
            {
                Name = name,
                DateOfBirth = "2024-01-15"
            });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateChild_WithLongName_ReturnsBadRequest()
    {
        using var factory = new FamilyDoseApiFactory();
        await factory.InitializeDatabaseAsync();

        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/children/",
            new
            {
                Name = new string('A', 101),
                DateOfBirth = "2024-01-15"
            });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateChild_WithoutBirthday_ReturnsBadRequest()
    {
        using var factory = new FamilyDoseApiFactory();
        await factory.InitializeDatabaseAsync();

        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/children/",
            new { Name = "Test Child" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetChild_WithUnknownId_ReturnsNotFound()
    {
        using var factory = new FamilyDoseApiFactory();
        await factory.InitializeDatabaseAsync();

        using var client = factory.CreateClient();

        var response = await client.GetAsync(
            $"/api/children/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }


    [Fact]
    public async Task CreateChild_WithFutureBirthday_ReturnsBadRequest()
    {
        using var factory = new FamilyDoseApiFactory();
        await factory.InitializeDatabaseAsync();

        using var client = factory.CreateClient();

        // The test clock is fixed at October 19, 2026.
        var response = await client.PostAsJsonAsync(
            "/api/children/",
            new
            {
                Name = "Future Child",
                DateOfBirth = "2026-10-20"
            });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateChild_WithBirthdayToday_ReturnsCreated()
    {
        using var factory = new FamilyDoseApiFactory();
        await factory.InitializeDatabaseAsync();

        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/children/",
            new
            {
                Name = "Newborn Test",
                DateOfBirth = "2026-10-19"
            });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }


    [Fact]
    public async Task UpdateChild_WithValidData_UpdatesExistingProfile()
    {
        using var factory = new FamilyDoseApiFactory();
        await factory.InitializeDatabaseAsync();

        using var client = factory.CreateClient();

        // Create a fictional child to update.
        var createdResponse = await client.PostAsJsonAsync(
            "/api/children/",
            new
            {
                Name = "Original Name",
                DateOfBirth = "2024-01-15"
            });

        Assert.Equal(HttpStatusCode.Created, createdResponse.StatusCode);

        var created = await createdResponse.Content
            .ReadFromJsonAsync<ChildResponse>();

        Assert.NotNull(created);

        // Update the existing record rather than creating another.
        var updateResponse = await client.PutAsJsonAsync(
            $"/api/children/{created.Id}",
            new
            {
                Name = "Updated Name",
                DateOfBirth = "2023-06-10"
            });

        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);

        var updated = await updateResponse.Content
            .ReadFromJsonAsync<ChildResponse>();

        Assert.NotNull(updated);
        Assert.Equal(created.Id, updated.Id);
        Assert.Equal("Updated Name", updated.Name);
        Assert.Equal(new DateOnly(2023, 6, 10), updated.DateOfBirth);
        Assert.True(updated.IsActive);

        // Confirm the database contains the updated record.
        var saved = await client.GetFromJsonAsync<ChildResponse>(
            $"/api/children/{created.Id}");

        Assert.NotNull(saved);
        Assert.Equal("Updated Name", saved.Name);
        Assert.Equal(updated.DateOfBirth, saved.DateOfBirth);

        // Editing must not create a duplicate profile.
        var allChildren = await client.GetFromJsonAsync<List<ChildResponse>>(
            "/api/children/");

        Assert.NotNull(allChildren);
        Assert.Single(allChildren);
    }

    [Fact]
    public async Task UpdateChild_WithUnknownId_ReturnsNotFound()
    {
        using var factory = new FamilyDoseApiFactory();
        await factory.InitializeDatabaseAsync();

        using var client = factory.CreateClient();

        var response = await client.PutAsJsonAsync(
            $"/api/children/{Guid.NewGuid()}",
            new
            {
                Name = "Test Child",
                DateOfBirth = "2024-01-15"
            });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task UpdateChild_WithBlankName_ReturnsBadRequest()
    {
        using var factory = new FamilyDoseApiFactory();
        await factory.InitializeDatabaseAsync();

        using var client = factory.CreateClient();

        var createdResponse = await client.PostAsJsonAsync(
            "/api/children/",
            new
            {
                Name = "Original Name",
                DateOfBirth = "2024-01-15"
            });

        var created = await createdResponse.Content
            .ReadFromJsonAsync<ChildResponse>();

        Assert.NotNull(created);

        var response = await client.PutAsJsonAsync(
            $"/api/children/{created.Id}",
            new
            {
                Name = "   ",
                DateOfBirth = "2024-01-15"
            });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        // Invalid updates must leave the original record unchanged.
        var saved = await client.GetFromJsonAsync<ChildResponse>(
            $"/api/children/{created.Id}");

        Assert.NotNull(saved);
        Assert.Equal("Original Name", saved.Name);
    }

    [Fact]
    public async Task UpdateChild_WithFutureBirthday_ReturnsBadRequest()
    {
        using var factory = new FamilyDoseApiFactory();
        await factory.InitializeDatabaseAsync();

        using var client = factory.CreateClient();

        var createdResponse = await client.PostAsJsonAsync(
            "/api/children/",
            new
            {
                Name = "Test Child",
                DateOfBirth = "2024-01-15"
            });

        var created = await createdResponse.Content
            .ReadFromJsonAsync<ChildResponse>();

        Assert.NotNull(created);

        // Our fixed test clock is October 19, 2026.
        var response = await client.PutAsJsonAsync(
            $"/api/children/{created.Id}",
            new
            {
                Name = "Test Child",
                DateOfBirth = "2026-10-20"
            });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }


    [Fact]
    public async Task ArchiveChild_ExistingProfile_SetsInactive()
    {
        using var factory = new FamilyDoseApiFactory();
        await factory.InitializeDatabaseAsync();
        using var client = factory.CreateClient();

        var created = await CreateTestChildAsync(client);

        var response = await client.PostAsync(
            $"/api/children/{created.Id}/archive", null);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        // Archiving preserves the record and its original identity.
        var saved = await client.GetFromJsonAsync<ChildResponse>(
            $"/api/children/{created.Id}");

        Assert.NotNull(saved);
        Assert.Equal(created.Id, saved.Id);
        Assert.False(saved.IsActive);
    }

    [Fact]
    public async Task RestoreChild_ArchivedProfile_SetsActive()
    {
        using var factory = new FamilyDoseApiFactory();
        await factory.InitializeDatabaseAsync();
        using var client = factory.CreateClient();

        var created = await CreateTestChildAsync(client);

        await client.PostAsync(
            $"/api/children/{created.Id}/archive", null);

        var response = await client.PostAsync(
            $"/api/children/{created.Id}/restore", null);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var saved = await client.GetFromJsonAsync<ChildResponse>(
            $"/api/children/{created.Id}");

        Assert.NotNull(saved);
        Assert.True(saved.IsActive);
    }

    [Fact]
    public async Task ArchiveChild_UnknownId_ReturnsNotFound()
    {
        using var factory = new FamilyDoseApiFactory();
        await factory.InitializeDatabaseAsync();
        using var client = factory.CreateClient();

        var response = await client.PostAsync(
            $"/api/children/{Guid.NewGuid()}/archive", null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task RestoreChild_UnknownId_ReturnsNotFound()
    {
        using var factory = new FamilyDoseApiFactory();
        await factory.InitializeDatabaseAsync();
        using var client = factory.CreateClient();

        var response = await client.PostAsync(
            $"/api/children/{Guid.NewGuid()}/restore", null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }


    [Fact]
    public async Task DeleteChild_ExistingProfile_RemovesRecord()
    {
        using var factory = new FamilyDoseApiFactory();
        await factory.InitializeDatabaseAsync();
        using var client = factory.CreateClient();

        var child = await CreateTestChildAsync(client);

        // Delete the profile and verify the API confirms removal.
        var response = await client.DeleteAsync(
            $"/api/children/{child.Id}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        // The deleted profile must no longer be retrievable.
        var getResponse = await client.GetAsync(
            $"/api/children/{child.Id}");

        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);

        var children = await client.GetFromJsonAsync<List<ChildResponse>>(
            "/api/children/");

        Assert.NotNull(children);
        Assert.Empty(children);
    }

    [Fact]
    public async Task DeleteChild_UnknownId_ReturnsNotFound()
    {
        using var factory = new FamilyDoseApiFactory();
        await factory.InitializeDatabaseAsync();
        using var client = factory.CreateClient();

        var response = await client.DeleteAsync(
            $"/api/children/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task DeleteChild_DoesNotRemoveOtherProfiles()
    {
        using var factory = new FamilyDoseApiFactory();
        await factory.InitializeDatabaseAsync();
        using var client = factory.CreateClient();

        var first = await CreateTestChildAsync(client);
        var second = await CreateTestChildAsync(client);

        Assert.NotEqual(first.Id, second.Id);

        // Removing one child must not affect another child.
        var response = await client.DeleteAsync(
            $"/api/children/{first.Id}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var remaining = await client.GetFromJsonAsync<List<ChildResponse>>(
            "/api/children/");

        Assert.NotNull(remaining);
        var saved = Assert.Single(remaining);
        Assert.Equal(second.Id, saved.Id);
    }


    [Fact]
    public async Task DeleteChild_WithMedication_ReturnsConflict()
    {
        using var factory = new FamilyDoseApiFactory();
        await factory.InitializeDatabaseAsync();
        using var client = factory.CreateClient();

        var child = await CreateTestChildAsync(client);

        // Insert a fictional medication into this test's isolated
        // SQLite database. No development records are touched.
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider
                .GetRequiredService<FamilyDoseDbContext>();

            db.Medications.Add(new Medication
            {
                ChildId = child.Id,
                Name = "Test Medication",
                DoseAmount = 2.5m,
                DoseUnit = "mL",
                MinimumIntervalMinutes = 240,
                MaxDosesPer24Hours = 5,
                ScheduleMode = MedicationScheduleMode.AsNeeded
            });

            await db.SaveChangesAsync();
        }

        // A child with medications must not be permanently deleted.
        var response = await client.DeleteAsync(
            $"/api/children/{child.Id}");

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);

        // Verify the child and medication still exist.
        using var verificationScope = factory.Services.CreateScope();

        var verificationDb = verificationScope.ServiceProvider
            .GetRequiredService<FamilyDoseDbContext>();

        Assert.True(await verificationDb.Children
            .AnyAsync(c => c.Id == child.Id));

        Assert.True(await verificationDb.Medications
            .AnyAsync(m => m.ChildId == child.Id));
    }

    // Shared test setup keeps individual tests short and readable.
    private static async Task<ChildResponse> CreateTestChildAsync(
        HttpClient client)
    {
        var response = await client.PostAsJsonAsync(
            "/api/children/",
            new
            {
                Name = "Fictional Child",
                DateOfBirth = "2024-01-15"
            });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var child = await response.Content
            .ReadFromJsonAsync<ChildResponse>();

        Assert.NotNull(child);
        return child;
    }

    private sealed record ChildResponse(
        Guid Id,
        string Name,
        DateOnly DateOfBirth,
        bool IsActive);
}

using System.Net;
using System.Net.Http.Json;
using FamilyDose.Api.Data;
using FamilyDose.Api.Models;
using FamilyDose.Tests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace FamilyDose.Tests;

public sealed class MedicationApiTests
{
    [Fact]
    public async Task GetMedications_WhenChildHasNone_ReturnsEmptyList()
    {
        using var factory = new FamilyDoseApiFactory();
        await factory.InitializeDatabaseAsync();
        using var client = factory.CreateClient();

        var child = await CreateChildAsync(client);

        var response = await client.GetAsync(
            $"/api/children/{child.Id}/medications/");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var medications = await response.Content
            .ReadFromJsonAsync<List<MedicationResponse>>();

        Assert.NotNull(medications);
        Assert.Empty(medications);
    }

    [Fact]
    public async Task GetMedications_WithUnknownChild_ReturnsNotFound()
    {
        using var factory = new FamilyDoseApiFactory();
        await factory.InitializeDatabaseAsync();
        using var client = factory.CreateClient();

        var response = await client.GetAsync(
            $"/api/children/{Guid.NewGuid()}/medications/");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetMedication_WithExistingId_ReturnsMedication()
    {
        using var factory = new FamilyDoseApiFactory();
        await factory.InitializeDatabaseAsync();
        using var client = factory.CreateClient();

        var child = await CreateChildAsync(client);

        var medication = new Medication
        {
            ChildId = child.Id,
            Name = "Fictional Test Medicine",
            DoseAmount = 2.5m,
            DoseUnit = "mL",
            Concentration = "Test concentration",
            MinimumIntervalMinutes = 240,
            MaxDosesPer24Hours = 5,
            ScheduleMode = MedicationScheduleMode.AsNeeded
        };

        // Seed the isolated test database directly until
        // the medication creation API is implemented.
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider
                .GetRequiredService<FamilyDoseDbContext>();

            db.Medications.Add(medication);
            await db.SaveChangesAsync();
        }

        var response = await client.GetAsync(
            $"/api/medications/{medication.Id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var saved = await response.Content
            .ReadFromJsonAsync<MedicationResponse>();

        Assert.NotNull(saved);
        Assert.Equal(medication.Id, saved.Id);
        Assert.Equal(child.Id, saved.ChildId);
        Assert.Equal("Fictional Test Medicine", saved.Name);
        Assert.Equal(2.5m, saved.DoseAmount);
        Assert.Equal("mL", saved.DoseUnit);
        Assert.Equal(240, saved.MinimumIntervalMinutes);
        Assert.Equal(5, saved.MaxDosesPer24Hours);
    }

    [Fact]
    public async Task GetMedication_WithUnknownId_ReturnsNotFound()
    {
        using var factory = new FamilyDoseApiFactory();
        await factory.InitializeDatabaseAsync();
        using var client = factory.CreateClient();

        var response = await client.GetAsync(
            $"/api/medications/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }


    [Fact]
    public async Task CreateMedication_ValidRequest_ReturnsCreated()
    {
        using var factory = new FamilyDoseApiFactory();
        await factory.InitializeDatabaseAsync();
        using var client = factory.CreateClient();

        var child = await CreateChildAsync(client);

        var response = await client.PostAsJsonAsync(
            $"/api/children/{child.Id}/medications/",
            new
            {
                Name = "Fictional Test Medicine",
                DoseAmount = 2.5m,
                DoseUnit = "mL",
                Concentration = "Test concentration",
                MinimumIntervalMinutes = 240,
                MaxDosesPer24Hours = 5,
                ScheduleMode = MedicationScheduleMode.AsNeeded,
                ReferenceUrl = (string?)null
            });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var medication = await response.Content
            .ReadFromJsonAsync<MedicationResponse>();

        Assert.NotNull(medication);
        Assert.NotEqual(Guid.Empty, medication.Id);
        Assert.Equal(child.Id, medication.ChildId);
        Assert.Equal(2.5m, medication.DoseAmount);

        // Verify the medication was actually persisted to SQLite.
        var saved = await client.GetFromJsonAsync<MedicationResponse>(
            $"/api/medications/{medication.Id}");

        Assert.NotNull(saved);
        Assert.Equal(medication.Id, saved.Id);
        Assert.Equal(2.5m, saved.DoseAmount);
    }

    [Fact]
    public async Task CreateMedication_UnknownChild_ReturnsNotFound()
    {
        using var factory = new FamilyDoseApiFactory();
        await factory.InitializeDatabaseAsync();
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            $"/api/children/{Guid.NewGuid()}/medications/",
            ValidMedicationRequest());

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CreateMedication_ArchivedChild_ReturnsConflict()
    {
        using var factory = new FamilyDoseApiFactory();
        await factory.InitializeDatabaseAsync();
        using var client = factory.CreateClient();

        var child = await CreateChildAsync(client);

        var archiveResponse = await client.PostAsync(
            $"/api/children/{child.Id}/archive", null);

        Assert.Equal(HttpStatusCode.NoContent, archiveResponse.StatusCode);

        var response = await client.PostAsJsonAsync(
            $"/api/children/{child.Id}/medications/",
            ValidMedicationRequest());

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }


    [Theory]
    [InlineData("", 2.5, "mL", 240, 5, 0, null)]
    [InlineData("Test", 0, "mL", 240, 5, 0, null)]
    [InlineData("Test", -1, "mL", 240, 5, 0, null)]
    [InlineData("Test", 2.5, "", 240, 5, 0, null)]
    [InlineData("Test", 2.5, "mL", 0, 5, 0, null)]
    [InlineData("Test", 2.5, "mL", -1, 5, 0, null)]
    [InlineData("Test", 2.5, "mL", 240, 0, 0, null)]
    [InlineData("Test", 2.5, "mL", 240, -1, 0, null)]
    [InlineData("Test", 2.5, "mL", 240, 5, 99, null)]
    [InlineData("Test", 2.5, "mL", 240, 5, 0, "ftp://example.com")]
    public async Task CreateMedication_InvalidConfiguration_ReturnsBadRequest(
        string name,
        double dose,
        string unit,
        int interval,
        int dailyLimit,
        int scheduleMode,
        string? referenceUrl)
    {
        using var factory = new FamilyDoseApiFactory();
        await factory.InitializeDatabaseAsync();
        using var client = factory.CreateClient();

        var child = await CreateChildAsync(client);

        var response = await client.PostAsJsonAsync(
            $"/api/children/{child.Id}/medications/",
            new
            {
                Name = name,
                DoseAmount = (decimal)dose,
                DoseUnit = unit,
                Concentration = (string?)null,
                MinimumIntervalMinutes = interval,
                MaxDosesPer24Hours = dailyLimit,
                ScheduleMode = scheduleMode,
                ReferenceUrl = referenceUrl
            });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        // Invalid configurations must not create database records.
        var medications = await client
            .GetFromJsonAsync<List<MedicationResponse>>(
                $"/api/children/{child.Id}/medications/");

        Assert.NotNull(medications);
        Assert.Empty(medications);
    }


    [Theory]
    [InlineData("name")]
    [InlineData("unit")]
    [InlineData("concentration")]
    [InlineData("url")]
    public async Task CreateMedication_ExcessiveFieldLength_ReturnsBadRequest(
        string field)
    {
        using var factory = new FamilyDoseApiFactory();
        await factory.InitializeDatabaseAsync();
        using var client = factory.CreateClient();

        var child = await CreateChildAsync(client);

        // Start with valid instructions, then exceed one field's limit.
        var name = field == "name"
            ? new string('A', 151)
            : "Fictional Test Medicine";

        var unit = field == "unit"
            ? new string('A', 31)
            : "mL";

        var concentration = field == "concentration"
            ? new string('A', 151)
            : "Test concentration";

        var referenceUrl = field == "url"
            ? "https://example.com/" + new string('a', 2050)
            : "https://example.com";

        var response = await client.PostAsJsonAsync(
            $"/api/children/{child.Id}/medications/",
            new
            {
                Name = name,
                DoseAmount = 2.5m,
                DoseUnit = unit,
                Concentration = concentration,
                MinimumIntervalMinutes = 240,
                MaxDosesPer24Hours = 5,
                ScheduleMode = MedicationScheduleMode.AsNeeded,
                ReferenceUrl = referenceUrl
            });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        // Rejected requests must never create medication records.
        var medications = await client
            .GetFromJsonAsync<List<MedicationResponse>>(
                $"/api/children/{child.Id}/medications/");

        Assert.NotNull(medications);
        Assert.Empty(medications);
    }

    // Shared request data keeps validation tests focused.
    private static object ValidMedicationRequest() => new
    {
        Name = "Fictional Test Medicine",
        DoseAmount = 2.5m,
        DoseUnit = "mL",
        Concentration = "Test concentration",
        MinimumIntervalMinutes = 240,
        MaxDosesPer24Hours = 5,
        ScheduleMode = MedicationScheduleMode.AsNeeded,
        ReferenceUrl = (string?)null
    };

    // Creates a fictional child through the real API.
    private static async Task<ChildResponse> CreateChildAsync(
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

    private sealed record ChildResponse(Guid Id);

    private sealed record MedicationResponse(
        Guid Id,
        Guid ChildId,
        string Name,
        decimal DoseAmount,
        string DoseUnit,
        string? Concentration,
        int MinimumIntervalMinutes,
        int MaxDosesPer24Hours,
        MedicationScheduleMode ScheduleMode,
        string? ReferenceUrl,
        bool IsActive);
}

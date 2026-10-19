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

    private sealed record ChildResponse(
        Guid Id,
        string Name,
        DateOnly DateOfBirth,
        bool IsActive);
}

using FamilyDose.Api.Endpoints;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddSingleton(TimeProvider.System);

builder.Services.AddDbContext<FamilyDose.Api.Data.FamilyDoseDbContext>(
    options =>
    {
        var connectionString =
            builder.Configuration.GetConnectionString("FamilyDose")
            ?? "Data Source=/workspace/data/familydose.db";

        options.UseSqlite(connectionString);
    });


var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapGet("/api/health", () =>
{
    return Results.Ok(new
    {
        status = "healthy",
        application = "FamilyDose",
        version = "0.1.0"
    });
})
.WithName("GetHealth")
.AllowAnonymous();

app.MapChildEndpoints();

app.Run();

public partial class Program { }

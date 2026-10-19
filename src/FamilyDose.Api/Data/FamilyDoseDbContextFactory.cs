using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace FamilyDose.Api.Data;

public sealed class FamilyDoseDbContextFactory
    : IDesignTimeDbContextFactory<FamilyDoseDbContext>
{
    public FamilyDoseDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<FamilyDoseDbContext>()
            .UseSqlite("Data Source=/workspace/data/familydose.db")
            .Options;

        return new FamilyDoseDbContext(options);
    }
}

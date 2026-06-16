using Microsoft.EntityFrameworkCore;
using PropertySaaS.Application.Common;
using PropertySaaS.Application.Features;
using PropertySaaS.Domain.Entities;
using PropertySaaS.Infrastructure.Data;

namespace PropertySaaS.Tests;

public class UnitTest1
{
    [Fact]
    public async Task Dashboard_Computes_Rent_Roll_And_Occupancy()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var db = new ApplicationDbContext(options);
        db.Database.EnsureCreated();

        var current = new CurrentOrganization
        {
            OrganizationId = ApplicationDbSeeder.DemoOrganizationId,
            OrganizationName = "Maple Leaf Property Group",
            UserEmail = "owner@mapleleafpm.ca"
        };

        var service = new SaasDataService(db, current);
        var summary = await service.GetDashboardAsync();

        Assert.True(summary.Properties >= 1);
        Assert.True(summary.Units >= 1);
        Assert.True(summary.MonthlyRentRoll > 0);
        Assert.True(summary.OccupancyRate > 0);
    }

    [Fact]
    public async Task Mutations_Are_Blocked_For_Viewer_Role()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var db = new ApplicationDbContext(options);
        db.Database.EnsureCreated();

        var current = new CurrentOrganization
        {
            OrganizationId = ApplicationDbSeeder.DemoOrganizationId,
            OrganizationName = "Maple Leaf Property Group",
            UserEmail = "viewer@mapleleafpm.ca",
            Role = "Viewer"
        };

        var service = new SaasDataService(db, current);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.AddPropertyAsync(new Property { Name = "Blocked", AddressLine1 = "1 Test St", City = "Toronto", Province = "ON" }));
    }
}

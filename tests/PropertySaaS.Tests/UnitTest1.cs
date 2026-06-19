using Microsoft.EntityFrameworkCore;
using PropertySaaS.Application.Common;
using PropertySaaS.Application.Features;
using PropertySaaS.Domain.Entities;
using PropertySaaS.Domain.Enums;
using PropertySaaS.Infrastructure.Data;
using PropertySaaS.Tests.TestDoubles;

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

        var service = new SaasDataService(db, current, new NullNotificationService());
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

        var service = new SaasDataService(db, current, new NullNotificationService());

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.AddPropertyAsync(new Property { Name = "Blocked", AddressLine1 = "1 Test St", City = "Toronto", Province = "ON" }));
    }

    [Fact]
    public async Task EnsureInvoiceConversation_Creates_And_Reuses_Thread()
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
            UserEmail = "owner@mapleleafpm.ca",
            Role = "Owner"
        };

        var property = new Property { Id = Guid.NewGuid(), OrganizationId = current.OrganizationId, Name = "Test Property", AddressLine1 = "1 Test St", City = "Toronto", Province = "ON" };
        var unit = new Unit { Id = Guid.NewGuid(), OrganizationId = current.OrganizationId, PropertyId = property.Id, UnitNumber = "101", MonthlyRent = 2000m };
        var tenant = new Tenant { Id = Guid.NewGuid(), OrganizationId = current.OrganizationId, FullName = "Test Tenant", Email = "tenant@test.com", PhoneNumber = "555-0000" };
        var lease = new Lease { Id = Guid.NewGuid(), OrganizationId = current.OrganizationId, UnitId = unit.Id, TenantId = tenant.Id, StartDate = DateOnly.FromDateTime(DateTime.Today.AddMonths(-1)), EndDate = DateOnly.FromDateTime(DateTime.Today.AddMonths(11)), MonthlyRent = 2000m, Status = PropertySaaS.Domain.Enums.LeaseStatus.Active };
        var invoice = new Invoice { Id = Guid.NewGuid(), OrganizationId = current.OrganizationId, LeaseId = lease.Id, Number = "INV-T1", DueDate = DateOnly.FromDateTime(DateTime.Today.AddDays(5)), Amount = 2000m, Balance = 500m, Status = PropertySaaS.Domain.Enums.PaymentStatus.PartiallyPaid };
        db.Properties.Add(property);
        db.Units.Add(unit);
        db.Tenants.Add(tenant);
        db.Leases.Add(lease);
        db.Invoices.Add(invoice);
        await db.SaveChangesAsync();

        var service = new SaasDataService(db, current, new NullNotificationService());
        var invoiceId = invoice.Id;

        var firstConversationId = await service.EnsureInvoiceConversationAsync(invoiceId);
        var secondConversationId = await service.EnsureInvoiceConversationAsync(invoiceId);

        Assert.NotNull(firstConversationId);
        Assert.Equal(firstConversationId, secondConversationId);
        Assert.True(db.TenantMessages.Any(x => x.TenantConversationId == firstConversationId));
    }

    [Fact]
    public async Task RecommendVendorForMaintenance_Prefers_Priority_Match()
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
            UserEmail = "owner@mapleleafpm.ca",
            Role = "Owner"
        };

        var vendorFallback = new Vendor { Id = Guid.NewGuid(), OrganizationId = current.OrganizationId, Name = "Fallback HVAC", Trade = "HVAC", DispatchStatus = "Available", TypicalResponseHours = 8, IsPreferred = true };
        var vendorPriority = new Vendor { Id = Guid.NewGuid(), OrganizationId = current.OrganizationId, Name = "Rapid Emergency Plumbing", Trade = "Plumbing", DispatchStatus = "On call", PreferredForPriority = "Emergency", TypicalResponseHours = 2, IsPreferred = true };
        var request = new MaintenanceRequest { Id = Guid.NewGuid(), OrganizationId = current.OrganizationId, Title = "Pipe leak", Priority = MaintenancePriority.Emergency, Status = "Open" };

        db.Vendors.Add(vendorFallback);
        db.Vendors.Add(vendorPriority);
        db.MaintenanceRequests.Add(request);
        await db.SaveChangesAsync();

        var service = new SaasDataService(db, current, new NullNotificationService());
        var recommendation = await service.RecommendVendorForMaintenanceAsync(request.Id);

        Assert.NotNull(recommendation);
        Assert.Equal(vendorPriority.Id, recommendation!.VendorId);
        Assert.Contains("Emergency", recommendation.RecommendationReason);
    }

    [Fact]
    public async Task AssignRecommendedVendor_Updates_Request_And_Status()
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
            UserEmail = "owner@mapleleafpm.ca",
            Role = "Owner"
        };

        var vendor = new Vendor { Id = Guid.NewGuid(), OrganizationId = current.OrganizationId, Name = "Rapid Emergency Plumbing", Trade = "Plumbing", DispatchStatus = "On call", PreferredForPriority = "Emergency", TypicalResponseHours = 2, IsPreferred = true };
        var request = new MaintenanceRequest { Id = Guid.NewGuid(), OrganizationId = current.OrganizationId, Title = "Pipe leak", Priority = MaintenancePriority.Emergency, Status = "Open" };

        db.Vendors.Add(vendor);
        db.MaintenanceRequests.Add(request);
        await db.SaveChangesAsync();

        var service = new SaasDataService(db, current, new NullNotificationService());
        var assigned = await service.AssignRecommendedVendorAsync(request.Id);
        var updated = await db.MaintenanceRequests.FirstAsync(x => x.Id == request.Id);

        Assert.True(assigned);
        Assert.Equal(vendor.Name, updated.VendorName);
        Assert.Equal("Scheduled", updated.Status);
        Assert.Equal("Assigned", updated.DispatchStatus);
    }

    [Fact]
    public async Task UpdateMaintenanceDispatchStatus_Acknowledged_And_Completed_Advance_Workflow()
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
            UserEmail = "owner@mapleleafpm.ca",
            Role = "Owner"
        };

        var request = new MaintenanceRequest
        {
            Id = Guid.NewGuid(),
            OrganizationId = current.OrganizationId,
            Title = "Pipe leak",
            Priority = MaintenancePriority.Emergency,
            Status = "Scheduled",
            DispatchStatus = "Assigned",
            VendorName = "Rapid Emergency Plumbing"
        };

        db.MaintenanceRequests.Add(request);
        await db.SaveChangesAsync();

        var service = new SaasDataService(db, current, new NullNotificationService());

        var acknowledged = await service.UpdateMaintenanceDispatchStatusAsync(request.Id, "Acknowledged");
        var afterAcknowledge = await db.MaintenanceRequests.AsNoTracking().FirstAsync(x => x.Id == request.Id);
        var completed = await service.UpdateMaintenanceDispatchStatusAsync(request.Id, "Completed");
        var afterComplete = await db.MaintenanceRequests.AsNoTracking().FirstAsync(x => x.Id == request.Id);

        Assert.True(acknowledged);
        Assert.Equal("Acknowledged", afterAcknowledge.DispatchStatus);
        Assert.Equal("In Progress", afterAcknowledge.Status);
        Assert.True(completed);
        Assert.Equal("Completed", afterComplete.DispatchStatus);
        Assert.Equal("Closed", afterComplete.Status);
    }

    [Fact]
    public async Task UpdateMaintenanceDispatchStatus_Recommended_Is_Rejected_When_Not_Unassigned()
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
            UserEmail = "owner@mapleleafpm.ca",
            Role = "Owner"
        };

        var request = new MaintenanceRequest
        {
            Id = Guid.NewGuid(),
            OrganizationId = current.OrganizationId,
            Title = "Boiler issue",
            Priority = MaintenancePriority.High,
            Status = "Scheduled",
            DispatchStatus = "Assigned",
            VendorName = "Boiler Team"
        };

        db.MaintenanceRequests.Add(request);
        await db.SaveChangesAsync();

        var service = new SaasDataService(db, current, new NullNotificationService());
        var updated = await service.UpdateMaintenanceDispatchStatusAsync(request.Id, "Recommended");
        var persisted = await db.MaintenanceRequests.AsNoTracking().FirstAsync(x => x.Id == request.Id);

        Assert.False(updated);
        Assert.Equal("Assigned", persisted.DispatchStatus);
    }
}

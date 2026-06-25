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
    public async Task GetResident360Async_Returns_Resident_Centric_Summary()
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

        var property = new Property { Id = Guid.NewGuid(), OrganizationId = current.OrganizationId, Name = "Resident 360 Property", AddressLine1 = "1 Test St", City = "Toronto", Province = "ON" };
        var unit = new Unit { Id = Guid.NewGuid(), OrganizationId = current.OrganizationId, PropertyId = property.Id, UnitNumber = "1704", MonthlyRent = 2400m, IsOccupied = true };
        var tenant = new Tenant { Id = Guid.NewGuid(), OrganizationId = current.OrganizationId, FullName = "Resident Summary Tenant", Email = "resident360@test.com", PhoneNumber = "555-1212" };
        var lease = new Lease { Id = Guid.NewGuid(), OrganizationId = current.OrganizationId, UnitId = unit.Id, TenantId = tenant.Id, StartDate = DateOnly.FromDateTime(DateTime.Today.AddMonths(-1)), EndDate = DateOnly.FromDateTime(DateTime.Today.AddMonths(11)), MonthlyRent = 2400m, Status = LeaseStatus.Active, MoveInNotes = "Active resident handoff complete." };
        var invoice = new Invoice { Id = Guid.NewGuid(), OrganizationId = current.OrganizationId, LeaseId = lease.Id, Number = "INV-R360", DueDate = DateOnly.FromDateTime(DateTime.Today.AddDays(5)), Amount = 2400m, Balance = 250m, Status = PaymentStatus.PartiallyPaid };
        var payment = new PaymentEntry { Id = Guid.NewGuid(), OrganizationId = current.OrganizationId, InvoiceId = invoice.Id, ReceivedDate = DateOnly.FromDateTime(DateTime.Today.AddDays(-2)), Amount = 2150m, Method = "E-Transfer", Reference = "R360-ET-1" };
        var conversation = new TenantConversation { Id = Guid.NewGuid(), OrganizationId = current.OrganizationId, TenantId = tenant.Id, LeaseId = lease.Id, Subject = "Welcome thread", Channel = ConversationChannel.Email, Status = "Awaiting reply", CreatedUtc = DateTime.UtcNow, LastContactUtc = DateTime.UtcNow };
        var maintenance = new MaintenanceRequest { Id = Guid.NewGuid(), OrganizationId = current.OrganizationId, PropertyId = property.Id, UnitId = unit.Id, Title = "Resident issue", Priority = MaintenancePriority.Medium, Status = "Open", DispatchStatus = "Assigned", RequestedDate = DateOnly.FromDateTime(DateTime.Today) };

        db.Properties.Add(property);
        db.Units.Add(unit);
        db.Tenants.Add(tenant);
        db.Leases.Add(lease);
        db.Invoices.Add(invoice);
        db.PaymentEntries.Add(payment);
        db.TenantConversations.Add(conversation);
        db.TenantMessages.Add(new TenantMessage { Id = Guid.NewGuid(), OrganizationId = current.OrganizationId, TenantConversationId = conversation.Id, Body = "Welcome to the building", SentBy = current.UserEmail, SentUtc = DateTime.UtcNow, CreatedUtc = DateTime.UtcNow });
        db.MaintenanceRequests.Add(maintenance);
        await db.SaveChangesAsync();

        var service = new SaasDataService(db, current, new NullNotificationService());
        var summary = await service.GetResident360Async(tenant.Id);

        Assert.NotNull(summary);
        Assert.Equal(tenant.Id, summary!.Tenant!.Id);
        Assert.NotNull(summary.ActiveLease);
        Assert.Equal(lease.Id, summary.ActiveLease!.Id);
        Assert.Single(summary.Invoices);
        Assert.Single(summary.Payments);
        Assert.Single(summary.Conversations);
        Assert.Single(summary.MaintenanceRequests);
        Assert.Equal(250m, summary.OpenBalance);
        Assert.Equal(2150m, summary.PaymentsReceived);
        Assert.True(summary.ActiveLease.N1IncreaseNoticeScheduled == false);
        Assert.True(summary.HasRentFollowUp);
        Assert.True(summary.HasNoticeWorkflow);
        Assert.True(summary.HasMaintenanceWorkflow);
    }

    [Fact]
    public async Task GetResident360Async_Sets_NoticeWorkflow_When_N1_Is_Scheduled_Without_Balance()
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

        var property = new Property { Id = Guid.NewGuid(), OrganizationId = current.OrganizationId, Name = "Notice Workflow Property", AddressLine1 = "3 Test St", City = "Toronto", Province = "ON" };
        var unit = new Unit { Id = Guid.NewGuid(), OrganizationId = current.OrganizationId, PropertyId = property.Id, UnitNumber = "905", MonthlyRent = 2600m, IsOccupied = true };
        var tenant = new Tenant { Id = Guid.NewGuid(), OrganizationId = current.OrganizationId, FullName = "Notice Workflow Tenant", Email = "notice@test.com", PhoneNumber = "555-9191" };
        var lease = new Lease
        {
            Id = Guid.NewGuid(),
            OrganizationId = current.OrganizationId,
            UnitId = unit.Id,
            TenantId = tenant.Id,
            StartDate = DateOnly.FromDateTime(DateTime.Today.AddMonths(-4)),
            EndDate = DateOnly.FromDateTime(DateTime.Today.AddMonths(8)),
            MonthlyRent = 2600m,
            Status = LeaseStatus.Active,
            N1IncreaseNoticeScheduled = true
        };
        var invoice = new Invoice { Id = Guid.NewGuid(), OrganizationId = current.OrganizationId, LeaseId = lease.Id, Number = "INV-N1", DueDate = DateOnly.FromDateTime(DateTime.Today.AddDays(10)), Amount = 2600m, Balance = 0m, Status = PaymentStatus.Paid };

        db.Properties.Add(property);
        db.Units.Add(unit);
        db.Tenants.Add(tenant);
        db.Leases.Add(lease);
        db.Invoices.Add(invoice);
        await db.SaveChangesAsync();

        var service = new SaasDataService(db, current, new NullNotificationService());
        var summary = await service.GetResident360Async(tenant.Id);

        Assert.NotNull(summary);
        Assert.NotNull(summary!.ActiveLease);
        Assert.Equal(0m, summary.OpenBalance);
        Assert.False(summary.HasRentFollowUp);
        Assert.True(summary.HasNoticeWorkflow);
    }

    [Fact]
    public async Task EnsureInvoiceConversation_Creates_Context_For_Rent_Reminder_Composer()
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

        var property = new Property { Id = Guid.NewGuid(), OrganizationId = current.OrganizationId, Name = "Composer Property", AddressLine1 = "2 Test St", City = "Toronto", Province = "ON" };
        var unit = new Unit { Id = Guid.NewGuid(), OrganizationId = current.OrganizationId, PropertyId = property.Id, UnitNumber = "802", MonthlyRent = 2100m, IsOccupied = true };
        var tenant = new Tenant { Id = Guid.NewGuid(), OrganizationId = current.OrganizationId, FullName = "Composer Tenant", Email = "composer@test.com", PhoneNumber = "555-3333" };
        var lease = new Lease { Id = Guid.NewGuid(), OrganizationId = current.OrganizationId, UnitId = unit.Id, TenantId = tenant.Id, StartDate = DateOnly.FromDateTime(DateTime.Today.AddMonths(-2)), EndDate = DateOnly.FromDateTime(DateTime.Today.AddMonths(10)), MonthlyRent = 2100m, Status = LeaseStatus.Active };
        var invoice = new Invoice { Id = Guid.NewGuid(), OrganizationId = current.OrganizationId, LeaseId = lease.Id, Number = "INV-COMP", DueDate = DateOnly.FromDateTime(DateTime.Today.AddDays(4)), Amount = 2100m, Balance = 600m, Status = PaymentStatus.PartiallyPaid };

        db.Properties.Add(property);
        db.Units.Add(unit);
        db.Tenants.Add(tenant);
        db.Leases.Add(lease);
        db.Invoices.Add(invoice);
        await db.SaveChangesAsync();

        var service = new SaasDataService(db, current, new NullNotificationService());
        var conversationId = await service.EnsureInvoiceConversationAsync(invoice.Id);
        var invoices = await service.GetInvoicesAsync();
        var conversations = await service.GetTenantConversationsAsync();

        Assert.NotNull(conversationId);
        var conversation = Assert.Single(conversations, x => x.TenantConversationId == conversationId!.Value);
        var invoiceSummary = Assert.Single(invoices, x => x.InvoiceId == invoice.Id);
        Assert.Equal(tenant.Id, conversation.TenantId);
        Assert.Equal("INV-COMP", invoiceSummary.Number);
        Assert.Equal(600m, invoiceSummary.Balance);
    }

    [Fact]
    public async Task GetTenantMessageTemplatesAsync_Uses_French_When_Organization_Prefers_French()
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
            Role = "Owner",
            PreferredLanguage = "fr-CA"
        };

        var service = new SaasDataService(db, current, new NullNotificationService());
        var templates = await service.GetTenantMessageTemplatesAsync();

        var rentReminder = Assert.Single(templates, x => x.Key == "rent-reminder");
        var maintenanceEntry = Assert.Single(templates, x => x.Key == "maintenance-entry");

        Assert.Contains("Bonjour", rentReminder.BodyTemplate);
        Assert.Equal("Relance de loyer Ontario", rentReminder.Title);
        Assert.Contains("coordonnons l’accès", maintenanceEntry.BodyTemplate);
    }

    [Fact]
    public async Task GetTenantMessageTemplatesAsync_Uses_English_When_Organization_Prefers_English()
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
            Role = "Owner",
            PreferredLanguage = "en-CA"
        };

        var service = new SaasDataService(db, current, new NullNotificationService());
        var templates = await service.GetTenantMessageTemplatesAsync();

        var maintenanceEntry = Assert.Single(templates, x => x.Key == "maintenance-entry");
        Assert.Equal("Maintenance access coordination", maintenanceEntry.Title);
        Assert.Contains("we are coordinating access", maintenanceEntry.BodyTemplate);
    }

    [Fact]
    public async Task GetTenantMessageTemplatesAsync_Includes_Notice_And_N4_Templates()
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
            Role = "Owner",
            PreferredLanguage = "fr-CA"
        };

        var service = new SaasDataService(db, current, new NullNotificationService());
        var templates = await service.GetTenantMessageTemplatesAsync();

        var noticeDelivery = Assert.Single(templates, x => x.Key == "notice-delivery");
        var n4Prep = Assert.Single(templates, x => x.Key == "n4-prep");

        Assert.Contains("confirme la remise", noticeDelivery.BodyTemplate);
        Assert.Contains("avant toute étape formelle", n4Prep.BodyTemplate);
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

    [Fact]
    public async Task GetLeadsAsync_Computes_ApplicationScore_And_Summary()
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

        var listing = new Listing { Id = Guid.NewGuid(), OrganizationId = current.OrganizationId, PropertyId = Guid.NewGuid(), Title = "King West One Bedroom" };
        var lead = new Lead
        {
            Id = Guid.NewGuid(),
            OrganizationId = current.OrganizationId,
            ListingId = listing.Id,
            FullName = "Qualified Applicant",
            Email = "applicant@example.com",
            Status = LeadStatus.UnderReview,
            MonthlyIncome = 9200m,
            DesiredMoveInDate = DateOnly.FromDateTime(DateTime.Today.AddDays(14)),
            OccupantCount = 2,
            HasPets = false,
            CreditScore = 755,
            ConsentToScreening = true
        };

        db.Listings.Add(listing);
        db.Leads.Add(lead);
        await db.SaveChangesAsync();

        var service = new SaasDataService(db, current, new NullNotificationService());
        var leads = await service.GetLeadsAsync();
        var summary = Assert.Single(leads);

        Assert.True(summary.ApplicationScore >= 90);
        Assert.Contains("application fit", summary.ApplicationSummary, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(755, summary.CreditScore);
    }

    [Fact]
    public async Task ConvertLeadToTenantAsync_Creates_Tenant_Lease_And_Marks_Lead_Won()
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

        var property = new Property { Id = Guid.NewGuid(), OrganizationId = current.OrganizationId, Name = "King West", AddressLine1 = "1 Test St", City = "Toronto", Province = "ON" };
        var unit = new Unit { Id = Guid.NewGuid(), OrganizationId = current.OrganizationId, PropertyId = property.Id, UnitNumber = "402", MonthlyRent = 2800m, IsOccupied = false };
        var listing = new Listing { Id = Guid.NewGuid(), OrganizationId = current.OrganizationId, PropertyId = property.Id, UnitId = unit.Id, Title = "King West 402", AskingRent = 2800m };
        var lead = new Lead
        {
            Id = Guid.NewGuid(),
            OrganizationId = current.OrganizationId,
            ListingId = listing.Id,
            FullName = "Approved Applicant",
            Email = "approved@applicant.com",
            PhoneNumber = "416-555-0202",
            Status = LeadStatus.Approved,
            MonthlyIncome = 9100m,
            DesiredMoveInDate = DateOnly.FromDateTime(DateTime.Today.AddDays(10)),
            CreditScore = 735,
            ConsentToScreening = true
        };

        db.Properties.Add(property);
        db.Units.Add(unit);
        db.Listings.Add(listing);
        db.Leads.Add(lead);
        await db.SaveChangesAsync();

        var service = new SaasDataService(db, current, new NullNotificationService());
        var converted = await service.ConvertLeadToTenantAsync(lead.Id);

        Assert.True(converted);
        Assert.True(db.Tenants.Any(x => x.Email == lead.Email));
        Assert.True(db.Leases.Any(x => x.UnitId == unit.Id && x.MonthlyRent == listing.AskingRent));
        Assert.Equal(LeadStatus.Won, (await db.Leads.FirstAsync(x => x.Id == lead.Id)).Status);
        Assert.True((await db.Units.FirstAsync(x => x.Id == unit.Id)).IsOccupied);
        var createdLease = await db.Leases.FirstAsync(x => x.UnitId == unit.Id && x.TenantId != Guid.Empty);
        var seededDocuments = await db.MediaAssets.Where(x => x.LeaseId == createdLease.Id).OrderBy(x => x.SortOrder).ToListAsync();
        Assert.Contains(seededDocuments, x => x.DocumentType == "SignedLease");
        Assert.Contains(seededDocuments, x => x.DocumentType == "IncomeProof");
    }

    [Fact]
    public async Task AddLeaseMoveInDocumentAsync_Creates_LeaseDocument_For_TargetLease()
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

        var property = new Property { Id = Guid.NewGuid(), OrganizationId = current.OrganizationId, Name = "King West", AddressLine1 = "1 Test St", City = "Toronto", Province = "ON" };
        var unit = new Unit { Id = Guid.NewGuid(), OrganizationId = current.OrganizationId, PropertyId = property.Id, UnitNumber = "404", MonthlyRent = 2900m, IsOccupied = true };
        var tenant = new Tenant { Id = Guid.NewGuid(), OrganizationId = current.OrganizationId, FullName = "Move In Tenant", Email = "tenant@movein.com" };
        var lease = new Lease { Id = Guid.NewGuid(), OrganizationId = current.OrganizationId, UnitId = unit.Id, TenantId = tenant.Id, StartDate = DateOnly.FromDateTime(DateTime.Today), EndDate = DateOnly.FromDateTime(DateTime.Today.AddYears(1)), MonthlyRent = 2900m, Status = LeaseStatus.Draft };

        db.Properties.Add(property);
        db.Units.Add(unit);
        db.Tenants.Add(tenant);
        db.Leases.Add(lease);
        await db.SaveChangesAsync();

        var service = new SaasDataService(db, current, new NullNotificationService());
        var added = await service.AddLeaseMoveInDocumentAsync(lease.Id, "InsuranceProof", "Insurance certificate.pdf", "Tenant uploaded insurance proof.");
        var documents = await service.GetLeaseMoveInDocumentsAsync(lease.Id);

        Assert.True(added);
        var document = Assert.Single(documents);
        Assert.Equal("Insurance certificate.pdf", document.FileName);
        Assert.Equal("InsuranceProof", document.DocumentType);
        Assert.Contains("insurance", document.Caption, StringComparison.OrdinalIgnoreCase);
        Assert.True(db.MediaAssets.Any(x => x.LeaseId == lease.Id && x.Category == MediaAssetCategory.LeaseDocument));
    }

    [Fact]
    public async Task EnsureLeaseMoveInOutreachDraftAsync_Creates_Draft_Message_With_Missing_Items()
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

        var property = new Property { Id = Guid.NewGuid(), OrganizationId = current.OrganizationId, Name = "King West", AddressLine1 = "1 Test St", City = "Toronto", Province = "ON" };
        var unit = new Unit { Id = Guid.NewGuid(), OrganizationId = current.OrganizationId, PropertyId = property.Id, UnitNumber = "406", MonthlyRent = 3100m, IsOccupied = true };
        var tenant = new Tenant { Id = Guid.NewGuid(), OrganizationId = current.OrganizationId, FullName = "Outreach Tenant", Email = "outreach@movein.com" };
        var lease = new Lease { Id = Guid.NewGuid(), OrganizationId = current.OrganizationId, UnitId = unit.Id, TenantId = tenant.Id, StartDate = DateOnly.FromDateTime(DateTime.Today), EndDate = DateOnly.FromDateTime(DateTime.Today.AddYears(1)), MonthlyRent = 3100m, Status = LeaseStatus.Draft };

        db.Properties.Add(property);
        db.Units.Add(unit);
        db.Tenants.Add(tenant);
        db.Leases.Add(lease);
        db.MediaAssets.Add(new MediaAsset
        {
            Id = Guid.NewGuid(),
            OrganizationId = current.OrganizationId,
            PropertyId = property.Id,
            UnitId = unit.Id,
            LeaseId = lease.Id,
            FileName = "Draft lease.pdf",
            BlobPath = "/leases/test/signed-lease",
            Caption = "Signed lease draft ready.",
            DocumentType = "SignedLease",
            Category = MediaAssetCategory.LeaseDocument,
            SortOrder = 1,
            CreatedUtc = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var service = new SaasDataService(db, current, new NullNotificationService());
        var conversationId = await service.EnsureLeaseMoveInOutreachDraftAsync(lease.Id);
        var conversation = await db.TenantConversations.FirstOrDefaultAsync(x => x.Id == conversationId);
        var messages = await db.TenantMessages.Where(x => x.TenantConversationId == conversationId).OrderBy(x => x.CreatedUtc).ToListAsync();

        Assert.NotNull(conversationId);
        Assert.NotNull(conversation);
        Assert.Contains("Move-in documents", conversation!.Subject, StringComparison.OrdinalIgnoreCase);
        Assert.True(messages.Count >= 2);
        Assert.Contains(messages, x => x.Body.Contains("Insurance proof", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(messages, x => x.Body.Contains("Government ID", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task GetTenantConversationsAsync_Includes_MoveInMissingItems_For_LeaseThreads()
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

        var property = new Property { Id = Guid.NewGuid(), OrganizationId = current.OrganizationId, Name = "King West", AddressLine1 = "1 Test St", City = "Toronto", Province = "ON" };
        var unit = new Unit { Id = Guid.NewGuid(), OrganizationId = current.OrganizationId, PropertyId = property.Id, UnitNumber = "407", MonthlyRent = 3200m, IsOccupied = true };
        var tenant = new Tenant { Id = Guid.NewGuid(), OrganizationId = current.OrganizationId, FullName = "Context Tenant", Email = "context@movein.com" };
        var lease = new Lease { Id = Guid.NewGuid(), OrganizationId = current.OrganizationId, UnitId = unit.Id, TenantId = tenant.Id, StartDate = DateOnly.FromDateTime(DateTime.Today), EndDate = DateOnly.FromDateTime(DateTime.Today.AddYears(1)), MonthlyRent = 3200m, Status = LeaseStatus.Draft, DepositReceived = false, InsuranceProofReceived = false, MoveInChecklistCompleted = false, StandardOntarioLeaseSigned = false };
        var conversation = new TenantConversation { Id = Guid.NewGuid(), OrganizationId = current.OrganizationId, TenantId = tenant.Id, LeaseId = lease.Id, Subject = "Lease onboarding", Channel = ConversationChannel.Email, Status = "Draft", CreatedUtc = DateTime.UtcNow, LastContactUtc = DateTime.UtcNow };

        db.Properties.Add(property);
        db.Units.Add(unit);
        db.Tenants.Add(tenant);
        db.Leases.Add(lease);
        db.TenantConversations.Add(conversation);
        db.TenantMessages.Add(new TenantMessage { Id = Guid.NewGuid(), OrganizationId = current.OrganizationId, TenantConversationId = conversation.Id, Body = "Draft outreach", SentBy = current.UserEmail, SentUtc = DateTime.UtcNow, CreatedUtc = DateTime.UtcNow });
        await db.SaveChangesAsync();

        var service = new SaasDataService(db, current, new NullNotificationService());
        var conversations = await service.GetTenantConversationsAsync();
        var summary = Assert.Single(conversations);

        Assert.True(summary.HasMoveInWorkflow);
        Assert.True(summary.MoveInMissingItemCount > 0);
        Assert.Contains(summary.MoveInMissingItems, x => x.Contains("Insurance", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(summary.MoveInMissingItems, x => x.Contains("Deposit", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task CompleteLeaseMoveInActionAsync_Marks_Deposit_And_Insurance_From_Thread_Action()
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

        var lease = new Lease
        {
            Id = Guid.NewGuid(),
            OrganizationId = current.OrganizationId,
            UnitId = Guid.NewGuid(),
            TenantId = Guid.NewGuid(),
            StartDate = DateOnly.FromDateTime(DateTime.Today),
            EndDate = DateOnly.FromDateTime(DateTime.Today.AddYears(1)),
            MonthlyRent = 3000m,
            Status = LeaseStatus.Draft,
            DepositReceived = false,
            InsuranceProofReceived = false,
            MoveInChecklistCompleted = false,
            StandardOntarioLeaseSigned = false
        };

        db.Leases.Add(lease);
        await db.SaveChangesAsync();

        var service = new SaasDataService(db, current, new NullNotificationService());
        var depositUpdated = await service.CompleteLeaseMoveInActionAsync(lease.Id, "Deposit received");
        var insuranceUpdated = await service.CompleteLeaseMoveInActionAsync(lease.Id, "Insurance proof received");
        var persisted = await db.Leases.AsNoTracking().FirstAsync(x => x.Id == lease.Id);

        Assert.True(depositUpdated);
        Assert.True(insuranceUpdated);
        Assert.True(persisted.DepositReceived);
        Assert.True(persisted.InsuranceProofReceived);
    }

    [Fact]
    public async Task GetTenantConversationsAsync_Includes_MoveInDocumentActions_For_Missing_Documents()
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
            Role = "Owner",
            Province = "ON",
            PreferredLanguage = "en-CA"
        };

        var tenant = new Tenant
        {
            Id = Guid.NewGuid(),
            OrganizationId = current.OrganizationId,
            FullName = "Jordan Tenant",
            Email = "jordan@example.com"
        };

        var property = new Property { Id = Guid.NewGuid(), OrganizationId = current.OrganizationId, Name = "Harbour Flats" };
        var unit = new Unit { Id = Guid.NewGuid(), OrganizationId = current.OrganizationId, PropertyId = property.Id, UnitNumber = "1203" };
        var lease = new Lease
        {
            Id = Guid.NewGuid(),
            OrganizationId = current.OrganizationId,
            UnitId = unit.Id,
            TenantId = tenant.Id,
            StartDate = DateOnly.FromDateTime(DateTime.Today),
            EndDate = DateOnly.FromDateTime(DateTime.Today.AddYears(1)),
            MonthlyRent = 2800m,
            Status = LeaseStatus.Draft,
            DepositReceived = true,
            InsuranceProofReceived = false,
            MoveInChecklistCompleted = true,
            StandardOntarioLeaseSigned = false
        };

        var conversation = new TenantConversation
        {
            Id = Guid.NewGuid(),
            OrganizationId = current.OrganizationId,
            TenantId = tenant.Id,
            LeaseId = lease.Id,
            Subject = "Move-in onboarding",
            Channel = ConversationChannel.Email,
            Status = "Draft",
            CreatedUtc = DateTime.UtcNow
        };

        db.Properties.Add(property);
        db.Units.Add(unit);
        db.Tenants.Add(tenant);
        db.Leases.Add(lease);
        db.TenantConversations.Add(conversation);
        db.MediaAssets.Add(new MediaAsset
        {
            Id = Guid.NewGuid(),
            OrganizationId = current.OrganizationId,
            PropertyId = property.Id,
            UnitId = unit.Id,
            LeaseId = lease.Id,
            FileName = "Government ID.pdf",
            BlobPath = "/leases/id",
            Caption = "Uploaded ID",
            DocumentType = "GovernmentId",
            Category = MediaAssetCategory.LeaseDocument,
            CreatedUtc = DateTime.UtcNow
        });

        await db.SaveChangesAsync();

        var service = new SaasDataService(db, current, new NullNotificationService());
        var summary = Assert.Single(await service.GetTenantConversationsAsync());

        Assert.True(summary.MoveInDocumentActions.ContainsKey("Insurance proof"));
        Assert.Equal("InsuranceProof", summary.MoveInDocumentActions["Insurance proof"]);
        Assert.True(summary.MoveInDocumentActions.ContainsKey("Signed lease"));
        Assert.Equal("SignedLease", summary.MoveInDocumentActions["Signed lease"]);
    }

    [Fact]
    public async Task CompleteLeaseMoveInActionAsync_Logs_Automatic_Thread_Update()
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

        var tenant = new Tenant { Id = Guid.NewGuid(), OrganizationId = current.OrganizationId, FullName = "Log Tenant", Email = "log@tenant.com" };
        var property = new Property { Id = Guid.NewGuid(), OrganizationId = current.OrganizationId, Name = "Harbour Flats" };
        var unit = new Unit { Id = Guid.NewGuid(), OrganizationId = current.OrganizationId, PropertyId = property.Id, UnitNumber = "803" };
        var lease = new Lease
        {
            Id = Guid.NewGuid(),
            OrganizationId = current.OrganizationId,
            UnitId = unit.Id,
            TenantId = tenant.Id,
            StartDate = DateOnly.FromDateTime(DateTime.Today),
            EndDate = DateOnly.FromDateTime(DateTime.Today.AddYears(1)),
            MonthlyRent = 2500m,
            Status = LeaseStatus.Draft
        };

        var conversation = new TenantConversation
        {
            Id = Guid.NewGuid(),
            OrganizationId = current.OrganizationId,
            TenantId = tenant.Id,
            LeaseId = lease.Id,
            Subject = "Lease onboarding for unit 803",
            Channel = ConversationChannel.Email,
            Status = "Draft",
            CreatedUtc = DateTime.UtcNow
        };

        db.Properties.Add(property);
        db.Units.Add(unit);
        db.Tenants.Add(tenant);
        db.Leases.Add(lease);
        db.TenantConversations.Add(conversation);
        await db.SaveChangesAsync();

        var service = new SaasDataService(db, current, new NullNotificationService());
        var updated = await service.CompleteLeaseMoveInActionAsync(lease.Id, "Deposit received");
        var messages = await db.TenantMessages.AsNoTracking().OrderBy(x => x.CreatedUtc).ToListAsync();
        var persistedConversation = await db.TenantConversations.AsNoTracking().SingleAsync();

        Assert.True(updated);
        Assert.Equal(2, messages.Count);
        Assert.Contains("Deposit received", messages[0].Body, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("Internal workflow", messages[0].DeliveryMethod);
        Assert.True(messages[1].IsAISuggested);
        Assert.Equal("Awaiting reply", persistedConversation.Status);
    }

    [Fact]
    public async Task AddLeaseMoveInDocumentAsync_Logs_Automatic_Thread_Update()
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

        var tenant = new Tenant { Id = Guid.NewGuid(), OrganizationId = current.OrganizationId, FullName = "Document Tenant", Email = "docs@tenant.com" };
        var property = new Property { Id = Guid.NewGuid(), OrganizationId = current.OrganizationId, Name = "Harbour Flats" };
        var unit = new Unit { Id = Guid.NewGuid(), OrganizationId = current.OrganizationId, PropertyId = property.Id, UnitNumber = "904" };
        var lease = new Lease
        {
            Id = Guid.NewGuid(),
            OrganizationId = current.OrganizationId,
            UnitId = unit.Id,
            TenantId = tenant.Id,
            StartDate = DateOnly.FromDateTime(DateTime.Today),
            EndDate = DateOnly.FromDateTime(DateTime.Today.AddYears(1)),
            MonthlyRent = 2600m,
            Status = LeaseStatus.Draft
        };

        var conversation = new TenantConversation
        {
            Id = Guid.NewGuid(),
            OrganizationId = current.OrganizationId,
            TenantId = tenant.Id,
            LeaseId = lease.Id,
            Subject = "Lease onboarding for unit 904",
            Channel = ConversationChannel.Email,
            Status = "Draft",
            CreatedUtc = DateTime.UtcNow
        };

        db.Properties.Add(property);
        db.Units.Add(unit);
        db.Tenants.Add(tenant);
        db.Leases.Add(lease);
        db.TenantConversations.Add(conversation);
        await db.SaveChangesAsync();

        var service = new SaasDataService(db, current, new NullNotificationService());
        var added = await service.AddLeaseMoveInDocumentAsync(lease.Id, "DepositReceipt", "Deposit receipt.pdf", "Added from thread");
        var threadMessages = await db.TenantMessages.AsNoTracking().ToListAsync();

        Assert.True(added);
        Assert.Equal(2, threadMessages.Count);
        Assert.Contains(threadMessages, x => x.Body.Contains("Deposit receipt", StringComparison.OrdinalIgnoreCase) && string.Equals(x.DeliveryMethod, "Internal workflow", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(threadMessages, x => x.IsAISuggested && x.Body.Contains("remaining items", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task CompleteLeaseMoveInActionAsync_Logs_Next_Recommended_Draft()
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

        var property = new Property { Id = Guid.NewGuid(), OrganizationId = current.OrganizationId, Name = "King West", Province = "ON" };
        var unit = new Unit { Id = Guid.NewGuid(), OrganizationId = current.OrganizationId, PropertyId = property.Id, UnitNumber = "515" };
        var tenant = new Tenant { Id = Guid.NewGuid(), OrganizationId = current.OrganizationId, FullName = "Next Step Tenant", Email = "next@tenant.com" };
        var lease = new Lease
        {
            Id = Guid.NewGuid(),
            OrganizationId = current.OrganizationId,
            UnitId = unit.Id,
            TenantId = tenant.Id,
            StartDate = DateOnly.FromDateTime(DateTime.Today),
            EndDate = DateOnly.FromDateTime(DateTime.Today.AddYears(1)),
            MonthlyRent = 3100m,
            Status = LeaseStatus.Draft,
            DepositReceived = false,
            InsuranceProofReceived = false,
            MoveInChecklistCompleted = false,
            StandardOntarioLeaseSigned = false
        };

        var conversation = new TenantConversation
        {
            Id = Guid.NewGuid(),
            OrganizationId = current.OrganizationId,
            TenantId = tenant.Id,
            LeaseId = lease.Id,
            Subject = "Lease onboarding",
            Channel = ConversationChannel.Email,
            Status = "Draft",
            CreatedUtc = DateTime.UtcNow
        };

        db.Properties.Add(property);
        db.Units.Add(unit);
        db.Tenants.Add(tenant);
        db.Leases.Add(lease);
        db.TenantConversations.Add(conversation);
        await db.SaveChangesAsync();

        var service = new SaasDataService(db, current, new NullNotificationService());
        await service.CompleteLeaseMoveInActionAsync(lease.Id, "Deposit received");
        var messages = await db.TenantMessages.AsNoTracking().OrderBy(x => x.CreatedUtc).ToListAsync();

        Assert.Equal(2, messages.Count);
        Assert.True(messages.Last().IsAISuggested);
        Assert.Contains("remaining items", messages.Last().Body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Deposit received", messages.Last().Body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetTenantConversationsAsync_Includes_MoveInNextDraftBody()
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

        var property = new Property { Id = Guid.NewGuid(), OrganizationId = current.OrganizationId, Name = "King West", Province = "ON" };
        var unit = new Unit { Id = Guid.NewGuid(), OrganizationId = current.OrganizationId, PropertyId = property.Id, UnitNumber = "608" };
        var tenant = new Tenant { Id = Guid.NewGuid(), OrganizationId = current.OrganizationId, FullName = "Draft Body Tenant", Email = "draftbody@tenant.com" };
        var lease = new Lease
        {
            Id = Guid.NewGuid(),
            OrganizationId = current.OrganizationId,
            UnitId = unit.Id,
            TenantId = tenant.Id,
            StartDate = DateOnly.FromDateTime(DateTime.Today),
            EndDate = DateOnly.FromDateTime(DateTime.Today.AddYears(1)),
            MonthlyRent = 3050m,
            Status = LeaseStatus.Draft,
            DepositReceived = true,
            InsuranceProofReceived = false,
            MoveInChecklistCompleted = false,
            StandardOntarioLeaseSigned = false
        };

        var conversation = new TenantConversation
        {
            Id = Guid.NewGuid(),
            OrganizationId = current.OrganizationId,
            TenantId = tenant.Id,
            LeaseId = lease.Id,
            Subject = "Lease onboarding",
            Channel = ConversationChannel.Email,
            Status = "Draft",
            CreatedUtc = DateTime.UtcNow
        };

        db.Properties.Add(property);
        db.Units.Add(unit);
        db.Tenants.Add(tenant);
        db.Leases.Add(lease);
        db.TenantConversations.Add(conversation);
        await db.SaveChangesAsync();

        var service = new SaasDataService(db, current, new NullNotificationService());
        var summary = Assert.Single(await service.GetTenantConversationsAsync());

        Assert.False(string.IsNullOrWhiteSpace(summary.MoveInNextDraftBody));
        Assert.Contains("remaining items", summary.MoveInNextDraftBody, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Deposit received", summary.MoveInNextDraftBody, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CompleteLeaseMoveInActionAsync_NextDraft_Remains_MoveIn_Focused()
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

        var property = new Property { Id = Guid.NewGuid(), OrganizationId = current.OrganizationId, Name = "King West", Province = "ON" };
        var unit = new Unit { Id = Guid.NewGuid(), OrganizationId = current.OrganizationId, PropertyId = property.Id, UnitNumber = "710" };
        var tenant = new Tenant { Id = Guid.NewGuid(), OrganizationId = current.OrganizationId, FullName = "Focused Tenant", Email = "focused@tenant.com" };
        var lease = new Lease
        {
            Id = Guid.NewGuid(),
            OrganizationId = current.OrganizationId,
            UnitId = unit.Id,
            TenantId = tenant.Id,
            StartDate = DateOnly.FromDateTime(DateTime.Today),
            EndDate = DateOnly.FromDateTime(DateTime.Today.AddYears(1)),
            MonthlyRent = 2995m,
            Status = LeaseStatus.Draft,
            DepositReceived = true,
            InsuranceProofReceived = false,
            MoveInChecklistCompleted = false,
            StandardOntarioLeaseSigned = false
        };

        var conversation = new TenantConversation
        {
            Id = Guid.NewGuid(),
            OrganizationId = current.OrganizationId,
            TenantId = tenant.Id,
            LeaseId = lease.Id,
            Subject = "Lease onboarding",
            Channel = ConversationChannel.Email,
            Status = "Draft",
            CreatedUtc = DateTime.UtcNow
        };

        db.Properties.Add(property);
        db.Units.Add(unit);
        db.Tenants.Add(tenant);
        db.Leases.Add(lease);
        db.TenantConversations.Add(conversation);
        await db.SaveChangesAsync();

        var service = new SaasDataService(db, current, new NullNotificationService());
        var summary = Assert.Single(await service.GetTenantConversationsAsync());

        Assert.Contains("remaining items", summary.MoveInNextDraftBody, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Deposit received", summary.MoveInNextDraftBody, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Insurance proof", summary.MoveInNextDraftBody, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetLeaseMoveInOutreachDraftAsync_WhenPackageComplete_Returns_WelcomeConfirmation()
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

        var property = new Property { Id = Guid.NewGuid(), OrganizationId = current.OrganizationId, Name = "King West", Province = "ON" };
        var unit = new Unit { Id = Guid.NewGuid(), OrganizationId = current.OrganizationId, PropertyId = property.Id, UnitNumber = "1102" };
        var tenant = new Tenant { Id = Guid.NewGuid(), OrganizationId = current.OrganizationId, FullName = "Welcome Tenant", Email = "welcome@tenant.com" };
        var lease = new Lease
        {
            Id = Guid.NewGuid(),
            OrganizationId = current.OrganizationId,
            UnitId = unit.Id,
            TenantId = tenant.Id,
            StartDate = DateOnly.FromDateTime(DateTime.Today),
            EndDate = DateOnly.FromDateTime(DateTime.Today.AddYears(1)),
            MonthlyRent = 3400m,
            Status = LeaseStatus.Draft,
            DepositReceived = true,
            InsuranceProofReceived = true,
            MoveInChecklistCompleted = true,
            StandardOntarioLeaseSigned = true
        };

        db.Properties.Add(property);
        db.Units.Add(unit);
        db.Tenants.Add(tenant);
        db.Leases.Add(lease);
        db.MediaAssets.AddRange(
            new MediaAsset { Id = Guid.NewGuid(), OrganizationId = current.OrganizationId, PropertyId = property.Id, UnitId = unit.Id, LeaseId = lease.Id, FileName = "Signed lease.pdf", BlobPath = "/leases/complete/lease", Caption = "Signed lease", DocumentType = "SignedLease", Category = MediaAssetCategory.LeaseDocument, CreatedUtc = DateTime.UtcNow },
            new MediaAsset { Id = Guid.NewGuid(), OrganizationId = current.OrganizationId, PropertyId = property.Id, UnitId = unit.Id, LeaseId = lease.Id, FileName = "Insurance proof.pdf", BlobPath = "/leases/complete/insurance", Caption = "Insurance", DocumentType = "InsuranceProof", Category = MediaAssetCategory.LeaseDocument, CreatedUtc = DateTime.UtcNow },
            new MediaAsset { Id = Guid.NewGuid(), OrganizationId = current.OrganizationId, PropertyId = property.Id, UnitId = unit.Id, LeaseId = lease.Id, FileName = "Government ID.pdf", BlobPath = "/leases/complete/id", Caption = "Government ID", DocumentType = "GovernmentId", Category = MediaAssetCategory.LeaseDocument, CreatedUtc = DateTime.UtcNow },
            new MediaAsset { Id = Guid.NewGuid(), OrganizationId = current.OrganizationId, PropertyId = property.Id, UnitId = unit.Id, LeaseId = lease.Id, FileName = "Income proof.pdf", BlobPath = "/leases/complete/income", Caption = "Income proof", DocumentType = "IncomeProof", Category = MediaAssetCategory.LeaseDocument, CreatedUtc = DateTime.UtcNow },
            new MediaAsset { Id = Guid.NewGuid(), OrganizationId = current.OrganizationId, PropertyId = property.Id, UnitId = unit.Id, LeaseId = lease.Id, FileName = "Deposit receipt.pdf", BlobPath = "/leases/complete/deposit", Caption = "Deposit receipt", DocumentType = "DepositReceipt", Category = MediaAssetCategory.LeaseDocument, CreatedUtc = DateTime.UtcNow });
        await db.SaveChangesAsync();

        var service = new SaasDataService(db, current, new NullNotificationService());
        var draft = await service.GetLeaseMoveInOutreachDraftAsync(lease.Id);

        Assert.NotNull(draft);
        Assert.Empty(draft!.MissingItems);
        Assert.Contains("welcome to", draft.Subject, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("excited", draft.Body, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("move-in timing", draft.Body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CompleteLeaseOnboardingHandoffAsync_ActivatesLease_And_LogsThreadUpdate()
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

        var property = new Property { Id = Guid.NewGuid(), OrganizationId = current.OrganizationId, Name = "King West", Province = "ON" };
        var unit = new Unit { Id = Guid.NewGuid(), OrganizationId = current.OrganizationId, PropertyId = property.Id, UnitNumber = "1208", IsOccupied = true };
        var tenant = new Tenant { Id = Guid.NewGuid(), OrganizationId = current.OrganizationId, FullName = "Handoff Tenant", Email = "handoff@tenant.com" };
        var lease = new Lease
        {
            Id = Guid.NewGuid(),
            OrganizationId = current.OrganizationId,
            UnitId = unit.Id,
            TenantId = tenant.Id,
            StartDate = DateOnly.FromDateTime(DateTime.Today),
            EndDate = DateOnly.FromDateTime(DateTime.Today.AddYears(1)),
            MonthlyRent = 3550m,
            Status = LeaseStatus.Draft,
            DepositReceived = true,
            InsuranceProofReceived = true,
            MoveInChecklistCompleted = true,
            StandardOntarioLeaseSigned = true
        };

        var conversation = new TenantConversation
        {
            Id = Guid.NewGuid(),
            OrganizationId = current.OrganizationId,
            TenantId = tenant.Id,
            LeaseId = lease.Id,
            Subject = "Lease onboarding",
            Channel = ConversationChannel.Email,
            Status = "Draft",
            CreatedUtc = DateTime.UtcNow
        };

        db.Properties.Add(property);
        db.Units.Add(unit);
        db.Tenants.Add(tenant);
        db.Leases.Add(lease);
        db.TenantConversations.Add(conversation);
        db.MediaAssets.AddRange(
            new MediaAsset { Id = Guid.NewGuid(), OrganizationId = current.OrganizationId, PropertyId = property.Id, UnitId = unit.Id, LeaseId = lease.Id, FileName = "Signed lease.pdf", BlobPath = "/leases/handoff/lease", Caption = "Signed lease", DocumentType = "SignedLease", Category = MediaAssetCategory.LeaseDocument, CreatedUtc = DateTime.UtcNow },
            new MediaAsset { Id = Guid.NewGuid(), OrganizationId = current.OrganizationId, PropertyId = property.Id, UnitId = unit.Id, LeaseId = lease.Id, FileName = "Insurance proof.pdf", BlobPath = "/leases/handoff/insurance", Caption = "Insurance", DocumentType = "InsuranceProof", Category = MediaAssetCategory.LeaseDocument, CreatedUtc = DateTime.UtcNow },
            new MediaAsset { Id = Guid.NewGuid(), OrganizationId = current.OrganizationId, PropertyId = property.Id, UnitId = unit.Id, LeaseId = lease.Id, FileName = "Government ID.pdf", BlobPath = "/leases/handoff/id", Caption = "Government ID", DocumentType = "GovernmentId", Category = MediaAssetCategory.LeaseDocument, CreatedUtc = DateTime.UtcNow },
            new MediaAsset { Id = Guid.NewGuid(), OrganizationId = current.OrganizationId, PropertyId = property.Id, UnitId = unit.Id, LeaseId = lease.Id, FileName = "Income proof.pdf", BlobPath = "/leases/handoff/income", Caption = "Income proof", DocumentType = "IncomeProof", Category = MediaAssetCategory.LeaseDocument, CreatedUtc = DateTime.UtcNow },
            new MediaAsset { Id = Guid.NewGuid(), OrganizationId = current.OrganizationId, PropertyId = property.Id, UnitId = unit.Id, LeaseId = lease.Id, FileName = "Deposit receipt.pdf", BlobPath = "/leases/handoff/deposit", Caption = "Deposit receipt", DocumentType = "DepositReceipt", Category = MediaAssetCategory.LeaseDocument, CreatedUtc = DateTime.UtcNow });
        await db.SaveChangesAsync();

        var service = new SaasDataService(db, current, new NullNotificationService());
        var completed = await service.CompleteLeaseOnboardingHandoffAsync(lease.Id);
        var persistedLease = await db.Leases.AsNoTracking().FirstAsync(x => x.Id == lease.Id);
        var messages = await db.TenantMessages.AsNoTracking().OrderBy(x => x.CreatedUtc).ToListAsync();

        Assert.True(completed);
        Assert.Equal(LeaseStatus.Active, persistedLease.Status);
        Assert.Contains("handoff completed", persistedLease.MoveInNotes, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(messages, x => x.Body.Contains("Lease is now active", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task GetTenantConversationsAsync_Sets_HasActiveTenancy_After_OnboardingHandoff()
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

        var property = new Property { Id = Guid.NewGuid(), OrganizationId = current.OrganizationId, Name = "King West", Province = "ON" };
        var unit = new Unit { Id = Guid.NewGuid(), OrganizationId = current.OrganizationId, PropertyId = property.Id, UnitNumber = "1601", IsOccupied = true };
        var tenant = new Tenant { Id = Guid.NewGuid(), OrganizationId = current.OrganizationId, FullName = "Active Tenant", Email = "active@tenant.com" };
        var lease = new Lease
        {
            Id = Guid.NewGuid(),
            OrganizationId = current.OrganizationId,
            UnitId = unit.Id,
            TenantId = tenant.Id,
            StartDate = DateOnly.FromDateTime(DateTime.Today),
            EndDate = DateOnly.FromDateTime(DateTime.Today.AddYears(1)),
            MonthlyRent = 3600m,
            Status = LeaseStatus.Active,
            DepositReceived = true,
            InsuranceProofReceived = true,
            MoveInChecklistCompleted = true,
            StandardOntarioLeaseSigned = true
        };

        var conversation = new TenantConversation
        {
            Id = Guid.NewGuid(),
            OrganizationId = current.OrganizationId,
            TenantId = tenant.Id,
            LeaseId = lease.Id,
            Subject = "Lease onboarding",
            Channel = ConversationChannel.Email,
            Status = "Awaiting reply",
            CreatedUtc = DateTime.UtcNow
        };

        db.Properties.Add(property);
        db.Units.Add(unit);
        db.Tenants.Add(tenant);
        db.Leases.Add(lease);
        db.TenantConversations.Add(conversation);
        db.MediaAssets.AddRange(
            new MediaAsset { Id = Guid.NewGuid(), OrganizationId = current.OrganizationId, PropertyId = property.Id, UnitId = unit.Id, LeaseId = lease.Id, FileName = "Signed lease.pdf", BlobPath = "/leases/active/lease", Caption = "Signed lease", DocumentType = "SignedLease", Category = MediaAssetCategory.LeaseDocument, CreatedUtc = DateTime.UtcNow },
            new MediaAsset { Id = Guid.NewGuid(), OrganizationId = current.OrganizationId, PropertyId = property.Id, UnitId = unit.Id, LeaseId = lease.Id, FileName = "Insurance proof.pdf", BlobPath = "/leases/active/insurance", Caption = "Insurance", DocumentType = "InsuranceProof", Category = MediaAssetCategory.LeaseDocument, CreatedUtc = DateTime.UtcNow },
            new MediaAsset { Id = Guid.NewGuid(), OrganizationId = current.OrganizationId, PropertyId = property.Id, UnitId = unit.Id, LeaseId = lease.Id, FileName = "Government ID.pdf", BlobPath = "/leases/active/id", Caption = "Government ID", DocumentType = "GovernmentId", Category = MediaAssetCategory.LeaseDocument, CreatedUtc = DateTime.UtcNow },
            new MediaAsset { Id = Guid.NewGuid(), OrganizationId = current.OrganizationId, PropertyId = property.Id, UnitId = unit.Id, LeaseId = lease.Id, FileName = "Income proof.pdf", BlobPath = "/leases/active/income", Caption = "Income proof", DocumentType = "IncomeProof", Category = MediaAssetCategory.LeaseDocument, CreatedUtc = DateTime.UtcNow },
            new MediaAsset { Id = Guid.NewGuid(), OrganizationId = current.OrganizationId, PropertyId = property.Id, UnitId = unit.Id, LeaseId = lease.Id, FileName = "Deposit receipt.pdf", BlobPath = "/leases/active/deposit", Caption = "Deposit receipt", DocumentType = "DepositReceipt", Category = MediaAssetCategory.LeaseDocument, CreatedUtc = DateTime.UtcNow });
        await db.SaveChangesAsync();

        var service = new SaasDataService(db, current, new NullNotificationService());
        var summary = Assert.Single(await service.GetTenantConversationsAsync());

        Assert.True(summary.HasActiveTenancy);
        Assert.Equal(0, summary.MoveInMissingItemCount);
        Assert.True(summary.CanCompleteOnboardingHandoff);
    }

    [Fact]
    public async Task CompleteLeaseMoveInStepAsync_DoesNotActivate_When_RequiredPackage_IsIncomplete()
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

        var property = new Property { Id = Guid.NewGuid(), OrganizationId = current.OrganizationId, Name = "King West", AddressLine1 = "1 Test St", City = "Toronto", Province = "ON" };
        var unit = new Unit { Id = Guid.NewGuid(), OrganizationId = current.OrganizationId, PropertyId = property.Id, UnitNumber = "405", MonthlyRent = 3000m, IsOccupied = true };
        var tenant = new Tenant { Id = Guid.NewGuid(), OrganizationId = current.OrganizationId, FullName = "Activation Tenant", Email = "activation@movein.com" };
        var lease = new Lease
        {
            Id = Guid.NewGuid(),
            OrganizationId = current.OrganizationId,
            UnitId = unit.Id,
            TenantId = tenant.Id,
            StartDate = DateOnly.FromDateTime(DateTime.Today),
            EndDate = DateOnly.FromDateTime(DateTime.Today.AddYears(1)),
            MonthlyRent = 3000m,
            Status = LeaseStatus.Draft,
            StandardOntarioLeaseSigned = true,
            DepositReceived = true,
            InsuranceProofReceived = true,
            MoveInChecklistCompleted = true
        };

        db.Properties.Add(property);
        db.Units.Add(unit);
        db.Tenants.Add(tenant);
        db.Leases.Add(lease);
        await db.SaveChangesAsync();

        var service = new SaasDataService(db, current, new NullNotificationService());
        var activated = await service.CompleteLeaseMoveInStepAsync(lease.Id, false, true);
        var persisted = await db.Leases.AsNoTracking().FirstAsync(x => x.Id == lease.Id);

        Assert.False(activated);
        Assert.Equal(LeaseStatus.Draft, persisted.Status);
    }
}

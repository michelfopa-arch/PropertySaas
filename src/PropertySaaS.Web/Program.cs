using System.Text;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MudBlazor.Services;
using PropertySaaS.Application.Abstractions;
using PropertySaaS.Application.Common;
using PropertySaaS.Application.Dashboard;
using PropertySaaS.Domain.Entities;
using PropertySaaS.Infrastructure.Data;
using PropertySaaS.Infrastructure.Options;
using PropertySaaS.Infrastructure.Services;
using PropertySaaS.Web.Components;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseStaticWebAssets();
var useLocalDevelopmentAuth = builder.Environment.IsDevelopment()
    && builder.Configuration.GetValue("Authentication:UseLocalDevelopmentAuth", true);

builder.Services.AddHttpContextAccessor();
builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices(builder.Configuration);
builder.Services.AddAuthorization();
var authenticationBuilder = builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
})
.AddCookie(options =>
{
    options.LoginPath = "/account/login";
});

builder.Services.AddScoped<CurrentOrganization>(provider =>
{
    var httpContextAccessor = provider.GetRequiredService<IHttpContextAccessor>();
    var db = provider.GetRequiredService<IApplicationDbContext>();
    var user = httpContextAccessor.HttpContext?.User;
    var email = user?.Claims.FirstOrDefault(c => c.Type.Contains("email", StringComparison.OrdinalIgnoreCase))?.Value
        ?? user?.Identity?.Name
        ?? "owner@mapleleafpm.ca";

    var appUser = db.Users.AsNoTracking().FirstOrDefault(x => x.Email == email)
        ?? db.Users.AsNoTracking().FirstOrDefault();

    if (appUser is not null)
    {
        var org = db.Organizations.AsNoTracking().FirstOrDefault(x => x.Id == appUser.OrganizationId);
        return new CurrentOrganization
        {
            OrganizationId = appUser.OrganizationId,
            OrganizationName = org?.Name ?? "Maple Leaf Property Group",
            UserEmail = email,
            Role = string.IsNullOrWhiteSpace(appUser.Role) ? "Owner" : appUser.Role
        };
    }

    return new CurrentOrganization
    {
        OrganizationId = Guid.Parse("11111111-1111-1111-1111-111111111111"),
        OrganizationName = "Maple Leaf Property Group",
        UserEmail = email,
        Role = user?.Claims.FirstOrDefault(c => c.Type.Contains("role", StringComparison.OrdinalIgnoreCase))?.Value ?? "Owner"
    };
});

var clerkSection = builder.Configuration.GetSection("Clerk");
var clerkOptions = clerkSection.Get<ClerkOptions>();
var useClerkAuthentication = !useLocalDevelopmentAuth
    && !string.IsNullOrWhiteSpace(clerkOptions?.Authority)
    && !string.IsNullOrWhiteSpace(clerkOptions.ClientId);

if (useClerkAuthentication)
{
    var configuredClerkOptions = clerkOptions!;
    authenticationBuilder.AddOpenIdConnect(options =>
    {
        options.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        options.Authority = configuredClerkOptions.Authority;
        options.ClientId = configuredClerkOptions.ClientId;
        options.ClientSecret = configuredClerkOptions.ClientSecret;
        options.ResponseType = "code";
        options.SaveTokens = true;
        options.Scope.Add("openid");
        options.Scope.Add("profile");
        options.Scope.Add("email");
        options.CallbackPath = "/signin-oidc";
    });

}

builder.Services.AddCascadingAuthenticationState();
builder.Services.AddMudServices();
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    db.Database.EnsureCreated();
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();
app.UseAuthentication();
app.Use(async (context, next) =>
{
    if (!useClerkAuthentication)
    {
        if (context.User?.Identity?.IsAuthenticated != true)
        {
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, "local-dev-user"),
                new Claim(ClaimTypes.Name, "owner@mapleleafpm.ca"),
                new Claim(ClaimTypes.Email, "owner@mapleleafpm.ca"),
                new Claim(ClaimTypes.Role, "Owner")
            };

            context.User = new ClaimsPrincipal(new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme));
        }
    }

    await next();
});
app.UseAuthorization();

app.Use(async (context, next) =>
{
    if (context.User?.Identity?.IsAuthenticated == true)
    {
        var email = context.User.Claims.FirstOrDefault(c => c.Type.Contains("email", StringComparison.OrdinalIgnoreCase))?.Value;
        if (!string.IsNullOrWhiteSpace(email))
        {
            var db = context.RequestServices.GetRequiredService<ApplicationDbContext>();
            var existing = await db.Users.FirstOrDefaultAsync(x => x.Email == email);
            if (existing is null)
            {
                var org = await db.Organizations.FirstOrDefaultAsync();
                if (org is not null)
                {
                    db.Users.Add(new AppUser
                    {
                        OrganizationId = org.Id,
                        ClerkUserId = context.User.Claims.FirstOrDefault(c => c.Type == "sub")?.Value ?? string.Empty,
                        Email = email,
                        FullName = context.User.Claims.FirstOrDefault(c => c.Type.Contains("name", StringComparison.OrdinalIgnoreCase))?.Value ?? email.Split('@')[0],
                        Role = "Owner",
                        IsActive = true
                    });
                    await db.SaveChangesAsync();
                }
            }
        }
    }

    await next();
});

app.UseAntiforgery();

app.MapGet("/account/login", async (HttpContext httpContext) =>
{
    if (!useClerkAuthentication)
    {
        httpContext.Response.Redirect("/dashboard");
        return;
    }

    await AuthenticationHttpContextExtensions.ChallengeAsync(httpContext, OpenIdConnectDefaults.AuthenticationScheme, new AuthenticationProperties { RedirectUri = "/dashboard" });
});

app.MapGet("/account/logout", async (HttpContext httpContext) =>
{
    await AuthenticationHttpContextExtensions.SignOutAsync(httpContext, CookieAuthenticationDefaults.AuthenticationScheme, new AuthenticationProperties());
    if (useClerkAuthentication)
    {
        await AuthenticationHttpContextExtensions.SignOutAsync(httpContext, OpenIdConnectDefaults.AuthenticationScheme, new AuthenticationProperties { RedirectUri = "/" });
        return;
    }
    httpContext.Response.Redirect("/");
});

app.MapGet("/billing/checkout/{plan}", async (string plan, HttpContext httpContext, [FromServices] StripeBillingService billingService) =>
{
    var root = $"{httpContext.Request.Scheme}://{httpContext.Request.Host}";
    var url = await billingService.CreateCheckoutSessionAsync(plan, $"{root}/subscriptions?checkout=success", $"{root}/subscriptions?checkout=cancelled");
    return Results.Redirect(url);
});

app.MapGet("/billing/portal", async (HttpContext httpContext, [FromServices] ApplicationDbContext db, [FromServices] StripeBillingService billingService) =>
{
    var org = await db.Organizations.AsQueryable().FirstOrDefaultAsync(x => x.Slug == "maple-leaf");
    var root = $"{httpContext.Request.Scheme}://{httpContext.Request.Host}";
    var url = await billingService.CreateBillingPortalAsync(org?.StripeCustomerId ?? string.Empty, $"{root}/subscriptions");
    return Results.Redirect(url);
});

app.MapPost("/billing/webhook", async (HttpContext httpContext, [FromServices] StripeBillingService billingService) =>
{
    using var reader = new StreamReader(httpContext.Request.Body);
    var json = await reader.ReadToEndAsync();
    await billingService.HandleWebhookAsync(json, httpContext.Request.Headers["Stripe-Signature"]);
    return Results.Ok();
});

app.MapGet("/export/properties", async ([FromServices] CurrentOrganization current, [FromServices] ApplicationDbContext db) =>
{
    var items = await db.Properties.Where(x => x.OrganizationId == current.OrganizationId).OrderBy(x => x.Name).ToListAsync();
    var csv = new StringBuilder();
    csv.AppendLine("Name,PropertyType,Address,City,Province,PostalCode,TargetRevenue,AmenitySummary,NeighborhoodNotes,LeasingNotes,OperationalNotes");
    foreach (var item in items) csv.AppendLine($"\"{item.Name}\",\"{item.PropertyType}\",\"{item.AddressLine1}\",\"{item.City}\",\"{item.Province}\",\"{item.PostalCode}\",{item.MonthlyRevenueTarget},\"{item.AmenitySummary}\",\"{item.NeighborhoodNotes}\",\"{item.LeasingNotes}\",\"{item.OperationalNotes}\"");
    return Results.File(Encoding.UTF8.GetBytes(csv.ToString()), "text/csv", "properties.csv");
});

app.MapGet("/export/tenants", async ([FromServices] CurrentOrganization current, [FromServices] ApplicationDbContext db) =>
{
    var items = await db.Tenants.Where(x => x.OrganizationId == current.OrganizationId).OrderBy(x => x.FullName).ToListAsync();
    var csv = new StringBuilder();
    csv.AppendLine("FullName,Email,PhoneNumber,CreditScore,ScreeningProvider");
    foreach (var item in items) csv.AppendLine($"\"{item.FullName}\",\"{item.Email}\",\"{item.PhoneNumber}\",{item.CreditScore},\"{item.ScreeningProvider}\"");
    return Results.File(Encoding.UTF8.GetBytes(csv.ToString()), "text/csv", "tenants.csv");
});

app.MapGet("/export/leases", async ([FromServices] CurrentOrganization current, [FromServices] ApplicationDbContext db) =>
{
    var items = await db.Leases.Where(x => x.OrganizationId == current.OrganizationId).OrderByDescending(x => x.StartDate).ToListAsync();
    var csv = new StringBuilder();
    csv.AppendLine("StartDate,EndDate,MonthlyRent,Status,OntarioLeaseSigned,N1Scheduled");
    foreach (var item in items) csv.AppendLine($"{item.StartDate},{item.EndDate},{item.MonthlyRent},{item.Status},{item.StandardOntarioLeaseSigned},{item.N1IncreaseNoticeScheduled}");
    return Results.File(Encoding.UTF8.GetBytes(csv.ToString()), "text/csv", "leases.csv");
});

app.MapGet("/docs/ontario-standard-lease", () =>
{
    var text = "Ontario Standard Lease Package\n\n- Landlord Information\n- Tenant Information\n- Rent and Services\n- Mandatory Ontario disclosures\n- Signature checklist";
    return Results.File(Encoding.UTF8.GetBytes(text), "text/plain", "ontario-standard-lease.txt");
});

app.MapGet("/docs/n4-template", () =>
{
    var text = "N4 Notice Template\n\nUse for non-payment workflow review. Verify legal advice and service rules before issuance.";
    return Results.File(Encoding.UTF8.GetBytes(text), "text/plain", "n4-template.txt");
});

app.MapGet("/docs/n1-template", () =>
{
    var text = "N1 Rent Increase Notice Template\n\nTrack notice windows, guideline limits and service dates.";
    return Results.File(Encoding.UTF8.GetBytes(text), "text/plain", "n1-template.txt");
});
app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();


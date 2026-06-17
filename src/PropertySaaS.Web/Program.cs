using System.Text;
using System.Globalization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Localization;
using MudBlazor.Services;
using PropertySaaS.Application.Abstractions;
using PropertySaaS.Application.Common;
using PropertySaaS.Application.Dashboard;
using PropertySaaS.Application.Features;
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
builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");
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
    var db = provider.GetRequiredService<ApplicationDbContext>();
    var user = httpContextAccessor.HttpContext?.User;
    var isAuthenticated = user?.Identity?.IsAuthenticated == true;
    var email = user?.Claims.FirstOrDefault(c => c.Type.Contains("email", StringComparison.OrdinalIgnoreCase))?.Value
        ?? user?.Identity?.Name
        ?? string.Empty;
    var selectedOrganizationId = httpContextAccessor.HttpContext?.Request.Cookies.TryGetValue("psaas_org", out var cookieValue) == true
        && Guid.TryParse(cookieValue, out var parsedOrgId)
        ? parsedOrgId
        : Guid.Empty;

    if (!isAuthenticated)
    {
        return new CurrentOrganization
        {
            UserId = Guid.Empty,
            OrganizationId = Guid.Empty,
            OrganizationName = "Visitor",
            UserEmail = string.Empty,
            UserFullName = string.Empty,
            Role = "Guest",
            SystemRole = "Guest",
            Province = "ON",
            CountryCode = "CA",
            PreferredLanguage = ResolvePreferredLanguage(httpContextAccessor.HttpContext, null, null, "ON")
        };
    }

    var appUser = string.IsNullOrWhiteSpace(email)
        ? db.Users.AsNoTracking().FirstOrDefault()
        : db.Users.AsNoTracking().FirstOrDefault(x => x.Email == email);
    var memberships = appUser is null
        ? new List<OrganizationMembership>()
        : db.OrganizationMemberships
            .AsNoTracking()
            .Where(x => x.UserId == appUser.Id && x.Status == "Active")
            .ToList();

    if (appUser is not null)
    {
        var selectedMembership = memberships.FirstOrDefault(x => x.OrganizationId == selectedOrganizationId)
            ?? memberships.FirstOrDefault();

        if (selectedMembership is not null)
        {
            var org = db.Organizations.AsNoTracking().FirstOrDefault(x => x.Id == selectedMembership.OrganizationId);
            var trialExpired = org?.TrialEndsUtc.HasValue == true
                && org.SubscriptionTier == PropertySaaS.Domain.Enums.SubscriptionTier.Trial
                && org.TrialEndsUtc.Value < DateTime.UtcNow;
            var subscriptionIsActive = org?.IsActive ?? true;

            return new CurrentOrganization
            {
                UserId = appUser.Id,
                OrganizationId = selectedMembership.OrganizationId,
                AccessibleOrganizationCount = memberships.Count,
                OrganizationName = org?.Name ?? "Maple Leaf Property Group",
                UserEmail = email,
                UserFullName = string.IsNullOrWhiteSpace(appUser.FullName) ? email : appUser.FullName,
                Role = string.IsNullOrWhiteSpace(selectedMembership.Role) ? "Owner" : selectedMembership.Role,
                SystemRole = string.IsNullOrWhiteSpace(appUser.SystemRole) ? "User" : appUser.SystemRole,
                Province = org?.Province ?? "ON",
                CountryCode = org?.CountryCode ?? "CA",
                PreferredLanguage = ResolvePreferredLanguage(httpContextAccessor.HttpContext, appUser.PreferredLanguage, org?.PreferredLanguage, org?.Province),
                SubscriptionIsActive = subscriptionIsActive,
                TrialExpired = trialExpired
            };
        }
    }

    if (appUser is not null)
    {
        return new CurrentOrganization
        {
            UserId = appUser.Id,
            OrganizationId = Guid.Empty,
            AccessibleOrganizationCount = memberships.Count,
            OrganizationName = "Pending access",
            UserEmail = email,
            UserFullName = string.IsNullOrWhiteSpace(appUser.FullName) ? email : appUser.FullName,
            Role = "Pending",
            SystemRole = string.IsNullOrWhiteSpace(appUser.SystemRole) ? "User" : appUser.SystemRole,
            Province = "ON",
            CountryCode = "CA",
            PreferredLanguage = ResolvePreferredLanguage(httpContextAccessor.HttpContext, appUser.PreferredLanguage, null, "ON")
        };
    }

    return new CurrentOrganization
    {
        UserId = Guid.Empty,
        OrganizationId = Guid.Empty,
        AccessibleOrganizationCount = 0,
        OrganizationName = "Pending access",
        UserEmail = email,
        UserFullName = user?.Claims.FirstOrDefault(c => c.Type.Contains("name", StringComparison.OrdinalIgnoreCase))?.Value ?? email,
        Role = "Pending",
        SystemRole = "User",
        Province = "ON",
        CountryCode = "CA",
        PreferredLanguage = ResolvePreferredLanguage(httpContextAccessor.HttpContext, null, null, "ON")
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

var supportedCultures = JurisdictionCatalog.SupportedCultureNames
    .Select(cultureName => new CultureInfo(cultureName))
    .ToList();

app.UseRequestLocalization(new RequestLocalizationOptions
{
    DefaultRequestCulture = new RequestCulture("en-CA"),
    SupportedCultures = supportedCultures,
    SupportedUICultures = supportedCultures
});

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    db.Database.EnsureCreated();
    EnsureSchemaUpgrades(db);
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
            var normalizedEmail = email.Trim().ToLowerInvariant();
            var existing = await db.Users.FirstOrDefaultAsync(x => x.Email == email);
            var fullName = ResolveUserFullName(context.User, email);
            var clerkUserId = context.User.Claims.FirstOrDefault(c => c.Type == "sub")?.Value ?? string.Empty;
            var hasChanges = false;

            if (existing is null)
            {
                existing = new AppUser
                {
                    Id = Guid.NewGuid(),
                    Email = normalizedEmail,
                    FullName = fullName,
                    ClerkUserId = clerkUserId,
                    Role = "Owner",
                    SystemRole = "User",
                    PreferredLanguage = ResolvePreferredLanguage(context, null, null, "ON"),
                    IsActive = true,
                    CreatedUtc = DateTime.UtcNow
                };
                db.Users.Add(existing);
                hasChanges = true;
            }

            if (!string.Equals(existing.Email, normalizedEmail, StringComparison.Ordinal))
            {
                existing.Email = normalizedEmail;
                hasChanges = true;
            }

            if (!string.IsNullOrWhiteSpace(fullName) && !string.Equals(existing.FullName, fullName, StringComparison.Ordinal))
            {
                existing.FullName = fullName;
                hasChanges = true;
            }

            if (string.IsNullOrWhiteSpace(existing.ClerkUserId) && !string.IsNullOrWhiteSpace(clerkUserId))
            {
                existing.ClerkUserId = clerkUserId;
                hasChanges = true;
            }

            if (!existing.IsActive)
            {
                existing.IsActive = true;
                hasChanges = true;
            }

            var primaryMembership = await db.OrganizationMemberships
                .FirstOrDefaultAsync(x => x.UserId == existing.Id && x.Status == "Active");

            if (existing.OrganizationId != Guid.Empty)
            {
                var legacyMembership = await db.OrganizationMemberships.FirstOrDefaultAsync(x => x.UserId == existing.Id && x.OrganizationId == existing.OrganizationId);
                if (legacyMembership is null)
                {
                    db.OrganizationMemberships.Add(new OrganizationMembership
                    {
                        Id = Guid.NewGuid(),
                        OrganizationId = existing.OrganizationId,
                        UserId = existing.Id,
                        Role = NormalizeOrganizationRole(existing.Role),
                        Status = "Active",
                        CreatedUtc = DateTime.UtcNow
                    });
                    hasChanges = true;
                }
                else if (!string.Equals(legacyMembership.Status, "Active", StringComparison.OrdinalIgnoreCase))
                {
                    legacyMembership.Status = "Active";
                    legacyMembership.Role = NormalizeOrganizationRole(string.IsNullOrWhiteSpace(legacyMembership.Role) ? existing.Role : legacyMembership.Role);
                    hasChanges = true;
                }
            }
            else if (primaryMembership is not null)
            {
                existing.OrganizationId = primaryMembership.OrganizationId;
                existing.Role = NormalizeOrganizationRole(primaryMembership.Role);
                hasChanges = true;
            }
            else if (string.IsNullOrWhiteSpace(existing.Role))
            {
                existing.Role = "Owner";
                hasChanges = true;
            }

            if (hasChanges)
            {
                await db.SaveChangesAsync();
            }
        }
    }

    await next();
});

var privateRoutePrefixes = new[]
{
    "/dashboard",
    "/properties",
    "/units",
    "/tenants",
    "/leases",
    "/maintenance",
    "/compliance",
    "/subscriptions",
    "/auditlogs",
    "/importexport",
    "/superadmin"
};

app.Use(async (context, next) =>
{
    if (context.User?.Identity?.IsAuthenticated == true)
    {
        var currentOrganization = context.RequestServices.GetRequiredService<CurrentOrganization>();
        var path = context.Request.Path.Value ?? string.Empty;
        var requiresOrganizationAccess = privateRoutePrefixes.Any(prefix => path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));

        if (requiresOrganizationAccess && !currentOrganization.HasOrganizationAccess)
        {
            context.Response.Redirect("/");
            return;
        }

        if (requiresOrganizationAccess && currentOrganization.HasOrganizationAccess && !currentOrganization.CanAccessWorkspace)
        {
            context.Response.Redirect("/subscriptions/expired");
            return;
        }
    }

    await next();
});

app.UseAntiforgery();

app.MapGet("/account/login", async (HttpContext httpContext) =>
{
    if (!useClerkAuthentication)
    {
        httpContext.Response.Redirect("/sign-in");
        return;
    }

    var invitation = httpContext.Request.Query["invitation"].ToString();
    var redirectUri = string.IsNullOrWhiteSpace(invitation)
        ? "/account/post-login"
        : $"/account/post-login?invitation={Uri.EscapeDataString(invitation)}";

    await AuthenticationHttpContextExtensions.ChallengeAsync(httpContext, OpenIdConnectDefaults.AuthenticationScheme, new AuthenticationProperties { RedirectUri = redirectUri });
});

app.MapGet("/account/sign-up", (HttpContext httpContext, [FromServices] ClerkOptions options) =>
{
    var signInUrl = string.IsNullOrWhiteSpace(options.SignInUrl)
        ? "/sign-in"
        : options.SignInUrl;

    var invitation = httpContext.Request.Query["invitation"].ToString();
    var postLoginUrl = string.IsNullOrWhiteSpace(invitation)
        ? $"{httpContext.Request.Scheme}://{httpContext.Request.Host}/account/post-login"
        : $"{httpContext.Request.Scheme}://{httpContext.Request.Host}/account/post-login?invitation={Uri.EscapeDataString(invitation)}";
    var redirectTarget = $"{signInUrl}{(signInUrl.Contains('?') ? '&' : '?')}redirect_url={Uri.EscapeDataString(postLoginUrl)}";
    return Results.Redirect(redirectTarget);
});

app.MapGet("/account/forgot-password", (HttpContext httpContext, [FromServices] ClerkOptions options) =>
{
    if (!string.IsNullOrWhiteSpace(options.UnauthorizedSignInUrl))
    {
        return Results.Redirect(options.UnauthorizedSignInUrl);
    }

    return Results.LocalRedirect("/forgot-password");
});

app.MapGet("/account/manage", (HttpContext httpContext, [FromServices] ClerkOptions options) =>
{
    if (!string.IsNullOrWhiteSpace(options.UserProfileUrl))
    {
        return Results.Redirect(options.UserProfileUrl);
    }

    return Results.LocalRedirect("/account");
});

app.MapPost("/organizations/select", async ([FromForm] Guid organizationId, HttpContext httpContext, [FromServices] ApplicationDbContext db) =>
{
    if (httpContext.User?.Identity?.IsAuthenticated != true)
    {
        return Results.Unauthorized();
    }

    var email = httpContext.User.Claims.FirstOrDefault(c => c.Type.Contains("email", StringComparison.OrdinalIgnoreCase))?.Value;
    if (string.IsNullOrWhiteSpace(email))
    {
        return Results.Unauthorized();
    }

    var user = await db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Email == email);
    if (user is null)
    {
        return Results.Unauthorized();
    }

    var allowed = await db.OrganizationMemberships.AsNoTracking().AnyAsync(x => x.UserId == user.Id && x.OrganizationId == organizationId && x.Status == "Active");
    if (!allowed)
    {
        return Results.Forbid();
    }

    httpContext.Response.Cookies.Append("psaas_org", organizationId.ToString(), new CookieOptions
    {
        Expires = DateTimeOffset.UtcNow.AddYears(1),
        HttpOnly = true,
        IsEssential = true,
        SameSite = SameSiteMode.Lax
    });

    return Results.LocalRedirect("/");
}).RequireAuthorization();

app.MapGet("/organizations/select", (Guid organizationId) => Results.LocalRedirect($"/organizations/switch/{organizationId}"));

app.MapGet("/organizations/switch/{organizationId:guid}", async (Guid organizationId, HttpContext httpContext, [FromServices] ApplicationDbContext db) =>
{
    if (httpContext.User?.Identity?.IsAuthenticated != true)
    {
        return Results.Unauthorized();
    }

    var email = httpContext.User.Claims.FirstOrDefault(c => c.Type.Contains("email", StringComparison.OrdinalIgnoreCase))?.Value;
    if (string.IsNullOrWhiteSpace(email))
    {
        return Results.Unauthorized();
    }

    var user = await db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Email == email);
    if (user is null)
    {
        return Results.Unauthorized();
    }

    var allowed = await db.OrganizationMemberships.AsNoTracking().AnyAsync(x => x.UserId == user.Id && x.OrganizationId == organizationId && x.Status == "Active");
    if (!allowed)
    {
        return Results.Forbid();
    }

    httpContext.Response.Cookies.Append("psaas_org", organizationId.ToString(), new CookieOptions
    {
        Expires = DateTimeOffset.UtcNow.AddYears(1),
        HttpOnly = true,
        IsEssential = true,
        SameSite = SameSiteMode.Lax
    });

    return Results.LocalRedirect("/dashboard");
}).RequireAuthorization();

app.MapGet("/invitations/accept", async (string token, HttpContext httpContext, [FromServices] SaasDataService dataService) =>
{
    if (httpContext.User?.Identity?.IsAuthenticated != true)
    {
        return Results.Redirect($"/account/login?invitation={Uri.EscapeDataString(token)}");
    }

    var email = httpContext.User.Claims.FirstOrDefault(c => c.Type.Contains("email", StringComparison.OrdinalIgnoreCase))?.Value;
    var clerkUserId = httpContext.User.Claims.FirstOrDefault(c => c.Type == "sub")?.Value ?? string.Empty;
    if (string.IsNullOrWhiteSpace(email))
    {
        return Results.Redirect("/");
    }

    await dataService.AcceptInvitationAsync(token, email, clerkUserId, ResolveUserFullName(httpContext.User, email));
    return Results.LocalRedirect("/account/post-login");
});

app.MapGet("/account/logout", async (HttpContext httpContext) =>
{
    httpContext.Response.Cookies.Delete("psaas_org");
    await AuthenticationHttpContextExtensions.SignOutAsync(httpContext, CookieAuthenticationDefaults.AuthenticationScheme, new AuthenticationProperties());
    httpContext.Response.Redirect("/");
});

app.MapGet("/account/post-login", async (string? invitation, HttpContext httpContext, [FromServices] CurrentOrganization current, [FromServices] SaasDataService dataService) =>
{
    if (!current.IsAuthenticated)
    {
        return Results.LocalRedirect("/");
    }

    if (!string.IsNullOrWhiteSpace(invitation))
    {
        var email = httpContext.User.Claims.FirstOrDefault(c => c.Type.Contains("email", StringComparison.OrdinalIgnoreCase))?.Value;
        var clerkUserId = httpContext.User.Claims.FirstOrDefault(c => c.Type == "sub")?.Value ?? string.Empty;

        if (!string.IsNullOrWhiteSpace(email))
        {
            try
            {
                await dataService.AcceptInvitationAsync(invitation, email, clerkUserId, ResolveUserFullName(httpContext.User, email));
                return Results.LocalRedirect("/account/post-login");
            }
            catch (InvalidOperationException)
            {
                return Results.LocalRedirect("/onboarding");
            }
        }
    }

    if (current.HasOrganizationAccess)
    {
        return Results.LocalRedirect(current.RequiresOrganizationSelection ? "/onboarding/organization-picker" : "/dashboard");
    }

    if (current.RequiresOrganizationSelection)
    {
        return Results.LocalRedirect("/onboarding/organization-picker");
    }

    if (!current.HasOrganizationAccess)
    {
        return Results.LocalRedirect("/onboarding");
    }

    return Results.LocalRedirect("/onboarding");
}).RequireAuthorization();

app.MapGet("/culture/set", (string culture, string? redirectUri, HttpContext httpContext) =>
{
    var normalizedCulture = string.IsNullOrWhiteSpace(culture)
        ? "en-CA"
        : culture.Trim();

    if (!JurisdictionCatalog.SupportedCultureNames.Contains(normalizedCulture, StringComparer.OrdinalIgnoreCase))
    {
        normalizedCulture = "en-CA";
    }

    httpContext.Response.Cookies.Append(
        CookieRequestCultureProvider.DefaultCookieName,
        CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(normalizedCulture)),
        new CookieOptions
        {
            Expires = DateTimeOffset.UtcNow.AddYears(1),
            IsEssential = true,
            HttpOnly = false,
            SameSite = SameSiteMode.Lax
        });

    var target = string.IsNullOrWhiteSpace(redirectUri) || !Uri.IsWellFormedUriString(redirectUri, UriKind.Relative)
        ? "/profile"
        : redirectUri;

    return Results.LocalRedirect(target);
});

app.MapGet("/billing/checkout/{plan}", async (string plan, HttpContext httpContext, [FromServices] CurrentOrganization current, [FromServices] ApplicationDbContext db, [FromServices] StripeBillingService billingService) =>
{
    var organization = await db.Organizations.FirstOrDefaultAsync(x => x.Id == current.OrganizationId);
    if (organization is null)
    {
        return Results.LocalRedirect("/subscriptions?checkout=missing-organization");
    }

    var root = $"{httpContext.Request.Scheme}://{httpContext.Request.Host}";
    var url = await billingService.CreateCheckoutSessionAsync(plan, $"{root}/subscriptions?checkout=success", $"{root}/subscriptions?checkout=cancelled");
    return Results.Redirect(url);
}).RequireAuthorization();

app.MapGet("/billing/portal", async (HttpContext httpContext, [FromServices] CurrentOrganization current, [FromServices] ApplicationDbContext db, [FromServices] StripeBillingService billingService) =>
{
    var org = await db.Organizations.AsQueryable().FirstOrDefaultAsync(x => x.Id == current.OrganizationId);
    var root = $"{httpContext.Request.Scheme}://{httpContext.Request.Host}";
    var url = await billingService.CreateBillingPortalAsync(org?.StripeCustomerId ?? string.Empty, $"{root}/subscriptions");
    return Results.Redirect(url);
}).RequireAuthorization();

app.MapPost("/billing/webhook", async (HttpContext httpContext, [FromServices] StripeBillingService billingService) =>
{
    using var reader = new StreamReader(httpContext.Request.Body);
    var json = await reader.ReadToEndAsync();
    await billingService.HandleWebhookAsync(json, httpContext.Request.Headers["Stripe-Signature"]);
    return Results.Ok();
});

app.MapGet("/subscriptions/expired", ([FromServices] CurrentOrganization current) =>
{
    if (!current.IsAuthenticated)
    {
        return Results.Redirect("/sign-in");
    }

    if (!current.HasOrganizationAccess)
    {
        return Results.Redirect("/onboarding");
    }

    if (current.CanAccessWorkspace)
    {
        return Results.Redirect("/dashboard");
    }

    return Results.Redirect("/subscriptions/expired-view");
}).RequireAuthorization();

app.MapPost("/support/grant", async ([FromBody] SupportGrantRequest? request, [FromServices] SaasDataService dataService) =>
{
    if (request is null || request.OrganizationId == Guid.Empty)
    {
        return Results.BadRequest();
    }

    var session = await dataService.GrantSupportAccessAsync(request.OrganizationId, request.Reason ?? "Support review");
    return Results.Ok(session);
}).RequireAuthorization();

app.MapGet("/export/properties", async ([FromServices] CurrentOrganization current, [FromServices] ApplicationDbContext db) =>
{
    var items = await db.Properties.Where(x => x.OrganizationId == current.OrganizationId).OrderBy(x => x.Name).ToListAsync();
    var csv = new StringBuilder();
    csv.AppendLine("Name,PropertyType,Address,City,Province,PostalCode,TargetRevenue,AmenitySummary,NeighborhoodNotes,LeasingNotes,OperationalNotes");
    foreach (var item in items) csv.AppendLine($"\"{item.Name}\",\"{item.PropertyType}\",\"{item.AddressLine1}\",\"{item.City}\",\"{item.Province}\",\"{item.PostalCode}\",{item.MonthlyRevenueTarget},\"{item.AmenitySummary}\",\"{item.NeighborhoodNotes}\",\"{item.LeasingNotes}\",\"{item.OperationalNotes}\"");
    return Results.File(Encoding.UTF8.GetBytes(csv.ToString()), "text/csv", "properties.csv");
}).RequireAuthorization();

app.MapGet("/export/tenants", async ([FromServices] CurrentOrganization current, [FromServices] ApplicationDbContext db) =>
{
    var items = await db.Tenants.Where(x => x.OrganizationId == current.OrganizationId).OrderBy(x => x.FullName).ToListAsync();
    var csv = new StringBuilder();
    csv.AppendLine("FullName,Email,PhoneNumber,CreditScore,ScreeningProvider");
    foreach (var item in items) csv.AppendLine($"\"{item.FullName}\",\"{item.Email}\",\"{item.PhoneNumber}\",{item.CreditScore},\"{item.ScreeningProvider}\"");
    return Results.File(Encoding.UTF8.GetBytes(csv.ToString()), "text/csv", "tenants.csv");
}).RequireAuthorization();

app.MapGet("/export/leases", async ([FromServices] CurrentOrganization current, [FromServices] ApplicationDbContext db) =>
{
    var items = await db.Leases.Where(x => x.OrganizationId == current.OrganizationId).OrderByDescending(x => x.StartDate).ToListAsync();
    var csv = new StringBuilder();
    csv.AppendLine("StartDate,EndDate,MonthlyRent,Status,OntarioLeaseSigned,N1Scheduled");
    foreach (var item in items) csv.AppendLine($"{item.StartDate},{item.EndDate},{item.MonthlyRent},{item.Status},{item.StandardOntarioLeaseSigned},{item.N1IncreaseNoticeScheduled}");
    return Results.File(Encoding.UTF8.GetBytes(csv.ToString()), "text/csv", "leases.csv");
}).RequireAuthorization();

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

app.MapGet("/docs/jurisdiction/lease-package", ([FromServices] CurrentOrganization current) =>
{
    var profile = current.Jurisdiction;
    var text = $"{profile.LeasePackageLabel}\n\nProvince: {profile.ProvinceDisplayName}\nLanguage: {current.PreferredLanguage}\n\n- Prepare jurisdiction-specific lease wording\n- Confirm required disclosures\n- Track signature and delivery audit trail";
    return Results.File(Encoding.UTF8.GetBytes(text), "text/plain", $"{profile.ProvinceCode.ToLowerInvariant()}-lease-package.txt");
});

app.MapGet("/docs/jurisdiction/{documentKey}", (string documentKey, [FromServices] CurrentOrganization current) =>
{
    var profile = current.Jurisdiction;
    if (!profile.DocumentTemplates.TryGetValue(documentKey, out var label))
    {
        return Results.NotFound();
    }

    var text = $"{label}\n\nProvince: {profile.ProvinceDisplayName}\nLanguage: {current.PreferredLanguage}\nDocument key: {documentKey}\n\n- Use the jurisdiction-specific workflow\n- Confirm current legal wording before issue\n- Retain audit trail for delivery and acknowledgment";
    return Results.File(Encoding.UTF8.GetBytes(text), "text/plain", $"{profile.ProvinceCode.ToLowerInvariant()}-{documentKey}.txt");
});

app.MapPost("/notifications/compliance/digest", async ([FromServices] CurrentOrganization current, [FromServices] INotificationService notifications) =>
{
    if (!current.CanManageData)
    {
        return Results.Forbid();
    }

    await notifications.SendComplianceDueSoonDigestAsync();
    return Results.Ok(new { message = "Compliance digest sent." });
}).RequireAuthorization();

app.MapPost("/notifications/support-alert", async ([FromBody] SupportAlertRequest? request, [FromServices] CurrentOrganization current, [FromServices] INotificationService notifications) =>
{
    if (!current.CanManageData)
    {
        return Results.Forbid();
    }

    if (request is null || string.IsNullOrWhiteSpace(request.Subject) || string.IsNullOrWhiteSpace(request.Message))
    {
        return Results.ValidationProblem(new Dictionary<string, string[]>
        {
            ["subject"] = new[] { "Subject is required." },
            ["message"] = new[] { "Message is required." }
        });
    }

    var subject = $"[PropertySaaS] {request.Subject.Trim()}";
    var message = $"Organization: {current.OrganizationName}\nUser: {current.UserEmail}\n\n{request.Message.Trim()}";
    await notifications.SendSupportAlertAsync(subject, message);
    return Results.Ok(new { message = "Support alert sent." });
}).RequireAuthorization();

static string ResolvePreferredLanguage(HttpContext? httpContext, string? userPreferredLanguage, string? organizationPreferredLanguage, string? province)
{
    var profile = JurisdictionCatalog.GetProfile(province);
    var candidates = new[]
    {
        userPreferredLanguage,
        organizationPreferredLanguage,
        httpContext?.Request.Headers.AcceptLanguage.FirstOrDefault()?.Split(',').FirstOrDefault(),
        profile.DefaultLanguage
    };

    return candidates.FirstOrDefault(candidate => !string.IsNullOrWhiteSpace(candidate) && profile.SupportedLanguages.Contains(candidate, StringComparer.OrdinalIgnoreCase))
        ?? profile.DefaultLanguage;
}

static string ResolveUserFullName(ClaimsPrincipal? user, string email)
{
    var firstName = user?.Claims.FirstOrDefault(c => c.Type.EndsWith("given_name", StringComparison.OrdinalIgnoreCase) || c.Type.EndsWith("first_name", StringComparison.OrdinalIgnoreCase))?.Value?.Trim();
    var lastName = user?.Claims.FirstOrDefault(c => c.Type.EndsWith("family_name", StringComparison.OrdinalIgnoreCase) || c.Type.EndsWith("last_name", StringComparison.OrdinalIgnoreCase))?.Value?.Trim();

    var combined = string.Join(" ", new[] { firstName, lastName }.Where(value => !string.IsNullOrWhiteSpace(value)));
    if (!string.IsNullOrWhiteSpace(combined))
    {
        return combined;
    }

    var fullName = user?.Claims.FirstOrDefault(c => c.Type.Contains("name", StringComparison.OrdinalIgnoreCase))?.Value?.Trim();
    return string.IsNullOrWhiteSpace(fullName)
        ? email.Split('@')[0]
        : fullName;
}

static string NormalizeOrganizationRole(string? role)
    => role is "Owner" or "Manager" or "Viewer"
        ? role
        : "Owner";

static void EnsureSchemaUpgrades(ApplicationDbContext db)
{
    db.Database.ExecuteSqlRaw("""
IF COL_LENGTH('Organizations', 'CountryCode') IS NULL
    ALTER TABLE [Organizations] ADD [CountryCode] nvarchar(8) NOT NULL CONSTRAINT DF_Organizations_CountryCode DEFAULT 'CA';
IF COL_LENGTH('Organizations', 'Province') IS NULL
    ALTER TABLE [Organizations] ADD [Province] nvarchar(8) NOT NULL CONSTRAINT DF_Organizations_Province DEFAULT 'ON';
IF COL_LENGTH('Organizations', 'PreferredLanguage') IS NULL
    ALTER TABLE [Organizations] ADD [PreferredLanguage] nvarchar(16) NOT NULL CONSTRAINT DF_Organizations_PreferredLanguage DEFAULT 'en-CA';
IF COL_LENGTH('Organizations', 'TrialEndsUtc') IS NULL
    ALTER TABLE [Organizations] ADD [TrialEndsUtc] datetime2 NULL;
IF COL_LENGTH('Users', 'PreferredLanguage') IS NULL
    ALTER TABLE [Users] ADD [PreferredLanguage] nvarchar(16) NOT NULL CONSTRAINT DF_Users_PreferredLanguage DEFAULT 'en-CA';
IF COL_LENGTH('ComplianceReminders', 'Province') IS NULL
    ALTER TABLE [ComplianceReminders] ADD [Province] nvarchar(8) NOT NULL CONSTRAINT DF_ComplianceReminders_Province DEFAULT 'ON';
IF COL_LENGTH('Users', 'SystemRole') IS NULL
    ALTER TABLE [Users] ADD [SystemRole] nvarchar(32) NOT NULL CONSTRAINT DF_Users_SystemRole DEFAULT 'User';
IF OBJECT_ID('OrganizationMemberships', 'U') IS NULL
BEGIN
    CREATE TABLE [OrganizationMemberships] (
        [Id] uniqueidentifier NOT NULL,
        [CreatedUtc] datetime2 NOT NULL,
        [ModifiedUtc] datetime2 NULL,
        [OrganizationId] uniqueidentifier NOT NULL,
        [UserId] uniqueidentifier NOT NULL,
        [Role] nvarchar(32) NOT NULL,
        [Status] nvarchar(32) NOT NULL,
        CONSTRAINT [PK_OrganizationMemberships] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_OrganizationMemberships_Organizations_OrganizationId] FOREIGN KEY ([OrganizationId]) REFERENCES [Organizations]([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_OrganizationMemberships_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users]([Id]) ON DELETE CASCADE
    );
    CREATE UNIQUE INDEX [IX_OrganizationMemberships_OrganizationId_UserId] ON [OrganizationMemberships] ([OrganizationId], [UserId]);
END;
IF OBJECT_ID('OrganizationInvitations', 'U') IS NULL
BEGIN
    CREATE TABLE [OrganizationInvitations] (
        [Id] uniqueidentifier NOT NULL,
        [CreatedUtc] datetime2 NOT NULL,
        [ModifiedUtc] datetime2 NULL,
        [OrganizationId] uniqueidentifier NOT NULL,
        [Email] nvarchar(256) NOT NULL,
        [Role] nvarchar(32) NOT NULL,
        [Token] nvarchar(128) NOT NULL,
        [Status] nvarchar(32) NOT NULL,
        [InvitedBy] nvarchar(256) NOT NULL,
        [ExpiresUtc] datetime2 NOT NULL,
        [AcceptedUtc] datetime2 NULL,
        [RevokedUtc] datetime2 NULL,
        CONSTRAINT [PK_OrganizationInvitations] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_OrganizationInvitations_Organizations_OrganizationId] FOREIGN KEY ([OrganizationId]) REFERENCES [Organizations]([Id]) ON DELETE CASCADE
    );
    CREATE UNIQUE INDEX [IX_OrganizationInvitations_Token] ON [OrganizationInvitations] ([Token]);
END;
IF NOT EXISTS (SELECT 1 FROM [OrganizationMemberships] WHERE [OrganizationId] = '11111111-1111-1111-1111-111111111111' AND [UserId] = '66666666-6666-6666-6666-666666666666')
BEGIN
    INSERT INTO [OrganizationMemberships] ([Id], [CreatedUtc], [ModifiedUtc], [OrganizationId], [UserId], [Role], [Status])
    VALUES ('dddddddd-dddd-dddd-dddd-dddddddddddd', SYSUTCDATETIME(), NULL, '11111111-1111-1111-1111-111111111111', '66666666-6666-6666-6666-666666666666', 'Owner', 'Active');
END;
UPDATE [Organizations]
SET [TrialEndsUtc] = DATEADD(day, 14, [CreatedUtc])
WHERE [TrialEndsUtc] IS NULL AND [SubscriptionTier] = 0;
""");
}

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();

internal sealed record SupportAlertRequest(string? Subject, string? Message);
internal sealed record SupportGrantRequest(Guid OrganizationId, string? Reason);


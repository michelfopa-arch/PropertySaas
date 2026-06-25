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
using PropertySaaS.Web;
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
                IsDemo = false,
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
    var isSuperAdmin = appUser is not null && string.Equals(appUser.SystemRole, "SuperAdmin", StringComparison.OrdinalIgnoreCase);

    if (appUser is not null && isSuperAdmin && selectedOrganizationId != Guid.Empty)
    {
        var selectedOrganization = db.Organizations.AsNoTracking().FirstOrDefault(x => x.Id == selectedOrganizationId);
        if (selectedOrganization is not null)
        {
            var trialExpired = selectedOrganization.TrialEndsUtc.HasValue
                && selectedOrganization.SubscriptionTier == PropertySaaS.Domain.Enums.SubscriptionTier.Trial
                && selectedOrganization.TrialEndsUtc.Value < DateTime.UtcNow;

            return new CurrentOrganization
            {
                UserId = appUser.Id,
                OrganizationId = selectedOrganization.Id,
                AccessibleOrganizationCount = memberships.Count,
                HasSuperAdminOrganizationSelection = true,
                OrganizationName = selectedOrganization.Name,
                IsDemo = selectedOrganization.IsDemo,
                DemoExpiresUtc = selectedOrganization.DemoExpiresUtc,
                UserEmail = email,
                UserFullName = string.IsNullOrWhiteSpace(appUser.FullName) ? email : appUser.FullName,
                Role = "SuperAdmin",
                SystemRole = appUser.SystemRole,
                Province = selectedOrganization.Province,
                CountryCode = selectedOrganization.CountryCode,
                PreferredLanguage = ResolvePreferredLanguage(httpContextAccessor.HttpContext, appUser.PreferredLanguage, selectedOrganization.PreferredLanguage, selectedOrganization.Province),
                SubscriptionIsActive = selectedOrganization.IsActive,
                TrialExpired = trialExpired
            };
        }
    }

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
                HasSuperAdminOrganizationSelection = false,
                OrganizationName = org?.Name ?? "Maple Leaf Property Group",
                IsDemo = org?.IsDemo ?? false,
                DemoExpiresUtc = org?.DemoExpiresUtc,
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
            HasSuperAdminOrganizationSelection = false,
            OrganizationName = "Pending access",
            IsDemo = false,
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
        IsDemo = false,
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
    PurgeExpiredDemoOrganizations(db);
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
            var existing = await db.Users.FirstOrDefaultAsync(x => x.Email == normalizedEmail);
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

            if (ShouldUpdateDisplayName(existing.FullName, fullName, normalizedEmail))
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

            if (existing.OrganizationId.HasValue && existing.OrganizationId.Value != Guid.Empty)
            {
                var legacyMembership = await db.OrganizationMemberships.FirstOrDefaultAsync(x => x.UserId == existing.Id && x.OrganizationId == existing.OrganizationId.Value);
                if (legacyMembership is null)
                {
                    db.OrganizationMemberships.Add(new OrganizationMembership
                    {
                        Id = Guid.NewGuid(),
                        OrganizationId = existing.OrganizationId.Value,
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
    var isSuperAdmin = string.Equals(user.SystemRole, "SuperAdmin", StringComparison.OrdinalIgnoreCase);
    if (!allowed && !isSuperAdmin)
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
    var isSuperAdmin = string.Equals(user.SystemRole, "SuperAdmin", StringComparison.OrdinalIgnoreCase);
    if (!allowed && !isSuperAdmin)
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

    try
    {
        var organizationId = await dataService.AcceptInvitationAsync(token, email, clerkUserId, ResolveUserFullName(httpContext.User, email));
        return Results.LocalRedirect($"/organizations/switch/{organizationId}");
    }
    catch (InvalidOperationException)
    {
        return Results.LocalRedirect("/onboarding?invite=expired");
    }
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
                var organizationId = await dataService.AcceptInvitationAsync(invitation, email, clerkUserId, ResolveUserFullName(httpContext.User, email));
                return Results.LocalRedirect($"/organizations/switch/{organizationId}");
            }
            catch (InvalidOperationException)
            {
                return Results.LocalRedirect("/onboarding?invite=expired");
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

app.MapGet("/export/maintenance/dispatch-journal", async ([FromServices] CurrentOrganization current, [FromServices] ApplicationDbContext db) =>
{
    var items = await db.MaintenanceRequests
        .Where(x => x.OrganizationId == current.OrganizationId)
        .OrderByDescending(x => x.RequestedDate)
        .ThenBy(x => x.DispatchStatus)
        .ToListAsync();

    var propertyMap = await db.Properties
        .Where(x => x.OrganizationId == current.OrganizationId)
        .ToDictionaryAsync(x => x.Id, x => x.Name);

    var unitMap = await db.Units
        .Where(x => x.OrganizationId == current.OrganizationId)
        .ToDictionaryAsync(x => x.Id, x => x.UnitNumber);

    var vendorSlaMap = await db.Vendors
        .Where(x => x.OrganizationId == current.OrganizationId)
        .ToDictionaryAsync(x => x.Name, x => x.TypicalResponseHours);

    var csv = new StringBuilder();
    csv.AppendLine("Title,Property,Unit,Priority,Status,DispatchStatus,Vendor,RequestedDate,EstimatedCost,VendorResponseHours,SlaRisk");
    foreach (var item in items)
    {
        propertyMap.TryGetValue(item.PropertyId, out var propertyName);
        var unitLabel = item.UnitId.HasValue && unitMap.TryGetValue(item.UnitId.Value, out var unitNumber) ? unitNumber : string.Empty;
        var responseHours = !string.IsNullOrWhiteSpace(item.VendorName) && vendorSlaMap.TryGetValue(item.VendorName, out var hours) ? hours : 0;
        var slaRisk = item.Status != "Closed" && item.DispatchStatus != "Completed" && responseHours > 0 && item.RequestedDate <= DateOnly.FromDateTime(DateTime.Today.AddDays(-1)) ? "At risk" : "On track";
        csv.AppendLine($"\"{item.Title}\",\"{propertyName ?? string.Empty}\",\"{unitLabel}\",\"{item.Priority}\",\"{item.Status}\",\"{item.DispatchStatus}\",\"{item.VendorName}\",{item.RequestedDate},{item.EstimatedCost},{responseHours},\"{slaRisk}\"");
    }

    return Results.File(Encoding.UTF8.GetBytes(csv.ToString()), "text/csv", "maintenance-dispatch-journal.csv");
}).RequireAuthorization();

app.MapGet("/export/listings/{listingId:guid}/copy", async (Guid listingId, [FromServices] SaasDataService dataService) =>
{
    var text = await dataService.BuildListingExportTextAsync(listingId);
    return Results.File(Encoding.UTF8.GetBytes(text), "text/plain", $"listing-copy-{listingId}.txt");
}).RequireAuthorization();

app.MapGet("/export/evidence-pack/{maintenanceRequestId:guid}/html", async (Guid maintenanceRequestId, HttpContext httpContext, [FromServices] CurrentOrganization current, [FromServices] SaasDataService dataService) =>
{
    var evidence = (await dataService.GetMaintenanceEvidenceAsync()).FirstOrDefault(x => x.MaintenanceRequestId == maintenanceRequestId);
    if (evidence is null)
    {
        return Results.NotFound();
    }

    var assets = (await dataService.GetMaintenanceMediaAssetsAsync())
        .Where(x => x.MaintenanceRequestId == maintenanceRequestId)
        .OrderBy(x => x.SortOrder)
        .ThenBy(x => x.CreatedUtc)
        .ToList();
    var communications = await dataService.GetMaintenanceCommunicationMessagesAsync(maintenanceRequestId);

    static string HtmlEncode(string? value)
        => System.Net.WebUtility.HtmlEncode(value ?? string.Empty);

    static string ResolveEvidenceCategory(string category)
        => category switch
        {
            nameof(PropertySaaS.Domain.Enums.MediaAssetCategory.MaintenanceBeforePhoto) => "Before",
            nameof(PropertySaaS.Domain.Enums.MediaAssetCategory.MaintenanceAfterPhoto) => "After / notice",
            nameof(PropertySaaS.Domain.Enums.MediaAssetCategory.MaintenanceEvidenceDocument) => "Proof",
            nameof(PropertySaaS.Domain.Enums.MediaAssetCategory.MaintenanceEvidence) => "Proof",
            nameof(PropertySaaS.Domain.Enums.MediaAssetCategory.Notice) => "After / notice",
            _ => category
        };

    var html = new StringBuilder();
    html.AppendLine("<!DOCTYPE html>");
    html.AppendLine("<html lang=\"en\"><head><meta charset=\"utf-8\" /><title>Evidence Pack</title><style>");
    html.AppendLine("body{font-family:Inter,Segoe UI,Arial,sans-serif;background:#fff;color:#10233f;margin:24px;}h1,h2{margin-bottom:8px;}section{border:1px solid rgba(15,39,71,.12);border-radius:12px;padding:16px;margin-bottom:16px;}ul{margin:8px 0 0 20px;}li{margin-bottom:6px;} .muted{color:#5e6b80;}</style></head><body>");
    html.AppendLine($"<h1>{HtmlEncode("Tribunal-ready evidence pack")}</h1>");
    html.AppendLine($"<p class=\"muted\">{HtmlEncode("Use this format for a cleaner tribunal-ready review and export flow.")}</p>");
    html.AppendLine("<section>");
    html.AppendLine($"<h2>{HtmlEncode("Ticket")}</h2>");
    html.AppendLine($"<p><strong>{HtmlEncode(evidence.Title)}</strong></p>");
    html.AppendLine($"<p>{HtmlEncode("Dossier signature")}: {HtmlEncode(evidence.DossierSignature)}</p>");
    html.AppendLine($"<p>{HtmlEncode("Property")}: {HtmlEncode(evidence.PropertyName)} {HtmlEncode(string.IsNullOrWhiteSpace(evidence.UnitLabel) ? string.Empty : $"- Unit {evidence.UnitLabel}")}</p>");
    html.AppendLine($"<p>{HtmlEncode("Status")}: {HtmlEncode(evidence.Status)}</p>");
    html.AppendLine($"<p>{HtmlEncode("Dispatch status")}: {HtmlEncode(evidence.DispatchStatus)}</p>");
    html.AppendLine($"<p>{HtmlEncode("Vendor")}: {HtmlEncode(string.IsNullOrWhiteSpace(evidence.VendorName) ? "Unassigned" : evidence.VendorName)}</p>");
    html.AppendLine($"<p>{HtmlEncode("Opened")}: {HtmlEncode(evidence.RequestedDate.ToString())}</p>");
    html.AppendLine($"<p>{HtmlEncode($"{evidence.EvidenceCount} evidence item(s)")}</p>");
    html.AppendLine("</section>");
    html.AppendLine("<section><h2>LTB-ready sections</h2><ul><li>1. Issue summary and affected unit</li><li>2. Chronology of visits, photos and updates</li><li>3. Notices and resident communications</li><li>4. Attached proof, before/after items and vendor evidence</li></ul></section>");
    html.AppendLine("<section><h2>Timeline items</h2><ul>");
    if (assets.Count == 0)
    {
        html.AppendLine($"<li>{HtmlEncode("No linked evidence assets yet.")}</li>");
    }
    else
    {
        foreach (var asset in assets)
        {
            html.AppendLine($"<li>{HtmlEncode(asset.CreatedUtc.ToLocalTime().ToString("g"))} - {HtmlEncode(ResolveEvidenceCategory(asset.Category))} - {HtmlEncode(asset.FileName)} - {HtmlEncode(asset.Caption)}</li>");
        }
    }
    html.AppendLine("</ul></section>");
    html.AppendLine($"<section><h2>{HtmlEncode("Notice and communication summary")}</h2>");
    if (communications.Count == 0)
    {
        html.AppendLine($"<p>{HtmlEncode("Use this section to summarize notice delivery, resident updates and service attempts before presenting the pack externally.")}</p>");
    }
    else
    {
        html.AppendLine("<ul>");
        foreach (var message in communications)
        {
            var direction = message.IsIncoming ? "Incoming" : "Outgoing";
            var delivered = message.DeliveredUtc.HasValue ? message.DeliveredUtc.Value.ToLocalTime().ToString("g") : "not marked delivered";
            var method = string.IsNullOrWhiteSpace(message.DeliveryMethod) ? "logged" : message.DeliveryMethod;
            var proof = string.IsNullOrWhiteSpace(message.DeliveryProof) ? string.Empty : $"; {message.DeliveryProof}";
            html.AppendLine($"<li>{HtmlEncode(message.SentUtc.ToLocalTime().ToString("g"))} - {HtmlEncode(direction)} - {HtmlEncode(message.Body)} ({HtmlEncode(method)}, {HtmlEncode(delivered + proof)})</li>");
        }
        html.AppendLine("</ul>");
    }
    html.AppendLine("</section>");
    html.AppendLine($"<section><h2>{HtmlEncode("Evidence pack preview")}</h2><p>{HtmlEncode(evidence.EvidencePackSummary)}</p></section>");
    html.AppendLine("</body></html>");

    return Results.File(Encoding.UTF8.GetBytes(html.ToString()), "text/html", $"evidence-pack-{maintenanceRequestId}.html");
}).RequireAuthorization();

app.MapGet("/export/evidence-pack/{maintenanceRequestId:guid}/pdf", async (Guid maintenanceRequestId, HttpContext httpContext, [FromServices] CurrentOrganization current, [FromServices] SaasDataService dataService) =>
{
    var evidence = (await dataService.GetMaintenanceEvidenceAsync()).FirstOrDefault(x => x.MaintenanceRequestId == maintenanceRequestId);
    if (evidence is null)
    {
        return Results.NotFound();
    }

    var assets = (await dataService.GetMaintenanceMediaAssetsAsync())
        .Where(x => x.MaintenanceRequestId == maintenanceRequestId)
        .OrderBy(x => x.SortOrder)
        .ThenBy(x => x.CreatedUtc)
        .ToList();
    var communications = await dataService.GetMaintenanceCommunicationMessagesAsync(maintenanceRequestId);

    var organizationName = string.IsNullOrWhiteSpace(current.OrganizationName) ? "PropertySaaS" : current.OrganizationName;
    var exportedUtc = DateTime.UtcNow;

    static string ResolveEvidenceCategoryLabel(string category)
        => category switch
        {
            nameof(PropertySaaS.Domain.Enums.MediaAssetCategory.MaintenanceBeforePhoto) => "Before",
            nameof(PropertySaaS.Domain.Enums.MediaAssetCategory.MaintenanceAfterPhoto) => "After / notice",
            nameof(PropertySaaS.Domain.Enums.MediaAssetCategory.MaintenanceEvidenceDocument) => "Proof",
            nameof(PropertySaaS.Domain.Enums.MediaAssetCategory.MaintenanceEvidence) => "Proof",
            nameof(PropertySaaS.Domain.Enums.MediaAssetCategory.Notice) => "After / notice",
            _ => category
        };

    var pdf = EvidencePackPdfBuilder.Build(evidence, assets, communications, organizationName, exportedUtc, ResolveEvidenceCategoryLabel);
    return Results.File(pdf, "application/pdf", $"evidence-pack-{maintenanceRequestId}.pdf");
}).RequireAuthorization();

app.MapGet("/docs/ontario-standard-lease", () =>
{
    return Results.Redirect("https://forms.mgcs.gov.on.ca/dataset/edff7620-980b-455f-9666-643196d8312f/resource/05677ea2-3173-4c0e-9a14-7a06cbcb41b9/download/2229e_standard-lease_static.pdf");
});

app.MapGet("/docs/n4-template", () =>
{
    return Results.Redirect("https://tribunalsontario.ca/documents/ltb/Notices%20of%20Termination%20%26%20Instructions/N4.pdf");
});

app.MapGet("/docs/n1-template", () =>
{
    return Results.Redirect("https://tribunalsontario.ca/documents/ltb/Notices%20of%20Rent%20Increase%20%26%20Instructions/N1.pdf");
});

app.MapGet("/docs/jurisdiction/lease-package", ([FromServices] CurrentOrganization current) =>
{
    var profile = current.Jurisdiction;
    if (profile.OfficialDocumentUrls.TryGetValue("lease-package", out var officialUrl))
    {
        return Results.Redirect(string.Equals(current.PreferredLanguage, "fr-CA", StringComparison.OrdinalIgnoreCase)
            && profile.OfficialDocumentUrls.TryGetValue("lease-package-fr", out var officialFrenchUrl)
                ? officialFrenchUrl
                : officialUrl);
    }

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

    if (profile.OfficialDocumentUrls.TryGetValue(documentKey, out var officialDocumentUrl))
    {
        return Results.Redirect(officialDocumentUrl);
    }

    var text = $"{label}\n\nProvince: {profile.ProvinceDisplayName}\nLanguage: {current.PreferredLanguage}\nDocument key: {documentKey}\n\n- Use the jurisdiction-specific workflow\n- Confirm current legal wording before issue\n- Retain audit trail for delivery and acknowledgment";
    return Results.File(Encoding.UTF8.GetBytes(text), "text/plain", $"{profile.ProvinceCode.ToLowerInvariant()}-{documentKey}.txt");
});

app.MapGet("/docs/move-in/{documentType}", (string documentType, [FromQuery] string? propertyName, [FromQuery] string? unitLabel, [FromQuery] string? tenantName, [FromServices] CurrentOrganization current) =>
{
    static (string Title, string Subtitle, IReadOnlyList<string> BulletPoints) ResolveTemplate(string type)
        => type switch
        {
            "DepositReceipt" => (
                "Move-in deposit receipt",
                "Internal receipt template for onboarding and audit trail.",
                new[]
                {
                    "Confirm amount received and payment method.",
                    "Reference the related lease and unit.",
                    "Capture who accepted the funds and when.",
                    "Provide a copy to the resident for records."
                }),
            "IncomeProof" => (
                "Income verification checklist",
                "Internal review sheet for application-to-lease conversion.",
                new[]
                {
                    "Attach pay stubs, employment letter or bank statements.",
                    "Verify affordability against monthly rent target.",
                    "Log any manual exceptions or guarantor notes.",
                    "Keep approval evidence in the lease package."
                }),
            "GovernmentId" => (
                "Government ID verification sheet",
                "Internal identity verification template for move-in readiness.",
                new[]
                {
                    "Review one primary government-issued photo ID.",
                    "Confirm name matches the lease package.",
                    "Record review date and reviewer initials.",
                    "Do not expose sensitive ID values in exported notes."
                }),
            _ => (
                "Move-in document template",
                "Internal onboarding template.",
                new[]
                {
                    "Collect the required onboarding evidence.",
                    "Review the package before activation.",
                    "Log the audit trail in the tenant thread."
                })
        };

    var template = ResolveTemplate(documentType);
    var pdf = MoveInDocumentPdfBuilder.Build(
        string.IsNullOrWhiteSpace(current.OrganizationName) ? "PropertySaaS" : current.OrganizationName,
        string.IsNullOrWhiteSpace(propertyName) ? "Property" : propertyName,
        string.IsNullOrWhiteSpace(unitLabel) ? "Unit" : unitLabel,
        string.IsNullOrWhiteSpace(tenantName) ? "Tenant" : tenantName,
        template.Title,
        template.Subtitle,
        template.BulletPoints,
        DateTime.UtcNow);

    return Results.File(pdf, "application/pdf", $"{documentType.ToLowerInvariant()}-template.pdf");
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
    var firstName = ResolveClaimValue(user,
        ClaimTypes.GivenName,
        "given_name",
        "givenname",
        "first_name",
        "firstname");
    var lastName = ResolveClaimValue(user,
        ClaimTypes.Surname,
        "family_name",
        "surname",
        "last_name",
        "lastname");

    var combined = string.Join(" ", new[] { firstName, lastName }.Where(value => !string.IsNullOrWhiteSpace(value)));
    if (!string.IsNullOrWhiteSpace(combined))
    {
        return combined;
    }

    var fullName = ResolveClaimValue(user,
        ClaimTypes.Name,
        "name",
        "full_name",
        "display_name");

    if (LooksGeneratedDisplayName(fullName, email))
    {
        return string.Empty;
    }

    return fullName;
}

static string ResolveClaimValue(ClaimsPrincipal? user, params string[] claimTypes)
    => user?.Claims
        .FirstOrDefault(c => claimTypes.Any(type => string.Equals(c.Type, type, StringComparison.OrdinalIgnoreCase) || c.Type.EndsWith(type, StringComparison.OrdinalIgnoreCase)))?
        .Value?
        .Trim()
        ?? string.Empty;

static bool ShouldUpdateDisplayName(string? currentValue, string? candidateValue, string email)
{
    var normalizedCandidate = candidateValue?.Trim() ?? string.Empty;
    if (string.IsNullOrWhiteSpace(normalizedCandidate) || LooksGeneratedDisplayName(normalizedCandidate, email))
    {
        return false;
    }

    var normalizedCurrent = currentValue?.Trim() ?? string.Empty;
    return string.IsNullOrWhiteSpace(normalizedCurrent)
        || LooksGeneratedDisplayName(normalizedCurrent, email)
        || !string.Equals(normalizedCurrent, normalizedCandidate, StringComparison.Ordinal);
}

static bool LooksGeneratedDisplayName(string? value, string email)
{
    if (string.IsNullOrWhiteSpace(value))
    {
        return true;
    }

    var normalized = value.Trim();
    var emailLocalPart = email.Split('@')[0];

    return string.Equals(normalized, email, StringComparison.OrdinalIgnoreCase)
        || string.Equals(normalized, emailLocalPart, StringComparison.OrdinalIgnoreCase)
        || normalized.StartsWith("user_", StringComparison.OrdinalIgnoreCase)
        || normalized.Contains("clerk", StringComparison.OrdinalIgnoreCase);
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
IF COL_LENGTH('Organizations', 'IsDemo') IS NULL
    ALTER TABLE [Organizations] ADD [IsDemo] bit NOT NULL CONSTRAINT DF_Organizations_IsDemo DEFAULT 0;
IF COL_LENGTH('Organizations', 'DemoTemplate') IS NULL
    ALTER TABLE [Organizations] ADD [DemoTemplate] nvarchar(32) NOT NULL CONSTRAINT DF_Organizations_DemoTemplate DEFAULT '';
IF COL_LENGTH('Organizations', 'DemoExpiresUtc') IS NULL
    ALTER TABLE [Organizations] ADD [DemoExpiresUtc] datetime2 NULL;
IF COL_LENGTH('Organizations', 'DemoResetAtUtc') IS NULL
    ALTER TABLE [Organizations] ADD [DemoResetAtUtc] datetime2 NULL;
IF COL_LENGTH('Users', 'PreferredLanguage') IS NULL
    ALTER TABLE [Users] ADD [PreferredLanguage] nvarchar(16) NOT NULL CONSTRAINT DF_Users_PreferredLanguage DEFAULT 'en-CA';
IF COL_LENGTH('ComplianceReminders', 'Province') IS NULL
    ALTER TABLE [ComplianceReminders] ADD [Province] nvarchar(8) NOT NULL CONSTRAINT DF_ComplianceReminders_Province DEFAULT 'ON';
IF COL_LENGTH('Users', 'SystemRole') IS NULL
    ALTER TABLE [Users] ADD [SystemRole] nvarchar(32) NOT NULL CONSTRAINT DF_Users_SystemRole DEFAULT 'User';
IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Users') AND name = 'OrganizationId' AND is_nullable = 0)
BEGIN
    IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Users_Organizations_OrganizationId')
        ALTER TABLE [Users] DROP CONSTRAINT [FK_Users_Organizations_OrganizationId];
    ALTER TABLE [Users] ALTER COLUMN [OrganizationId] uniqueidentifier NULL;
    ALTER TABLE [Users] WITH CHECK ADD CONSTRAINT [FK_Users_Organizations_OrganizationId] FOREIGN KEY([OrganizationId]) REFERENCES [Organizations]([Id]);
END;
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
IF OBJECT_ID('Vendors', 'U') IS NULL
BEGIN
    CREATE TABLE [Vendors] (
        [Id] uniqueidentifier NOT NULL,
        [CreatedUtc] datetime2 NOT NULL,
        [ModifiedUtc] datetime2 NULL,
        [OrganizationId] uniqueidentifier NOT NULL,
        [Name] nvarchar(max) NOT NULL,
        [Trade] nvarchar(max) NOT NULL,
        [Email] nvarchar(max) NOT NULL,
        [PhoneNumber] nvarchar(max) NOT NULL,
        [ServiceArea] nvarchar(max) NOT NULL,
        [IsPreferred] bit NOT NULL,
        [IsActive] bit NOT NULL,
        [Notes] nvarchar(max) NOT NULL,
        CONSTRAINT [PK_Vendors] PRIMARY KEY ([Id])
    );
END;
IF OBJECT_ID('Listings', 'U') IS NULL
BEGIN
    CREATE TABLE [Listings] (
        [Id] uniqueidentifier NOT NULL,
        [CreatedUtc] datetime2 NOT NULL,
        [ModifiedUtc] datetime2 NULL,
        [OrganizationId] uniqueidentifier NOT NULL,
        [PropertyId] uniqueidentifier NOT NULL,
        [UnitId] uniqueidentifier NULL,
        [Title] nvarchar(max) NOT NULL,
        [Description] nvarchar(max) NOT NULL,
        [AskingRent] decimal(18,2) NOT NULL,
        [Status] int NOT NULL,
        [PublishTargets] nvarchar(max) NOT NULL,
        [PublishedUtc] datetime2 NULL,
        CONSTRAINT [PK_Listings] PRIMARY KEY ([Id])
    );
END;
IF OBJECT_ID('Leads', 'U') IS NULL
BEGIN
    CREATE TABLE [Leads] (
        [Id] uniqueidentifier NOT NULL,
        [CreatedUtc] datetime2 NOT NULL,
        [ModifiedUtc] datetime2 NULL,
        [OrganizationId] uniqueidentifier NOT NULL,
        [ListingId] uniqueidentifier NOT NULL,
        [FullName] nvarchar(max) NOT NULL,
        [Email] nvarchar(max) NOT NULL,
        [PhoneNumber] nvarchar(max) NOT NULL,
        [Source] nvarchar(max) NOT NULL,
        [Status] int NOT NULL,
        [Notes] nvarchar(max) NOT NULL,
        CONSTRAINT [PK_Leads] PRIMARY KEY ([Id])
    );
END;
IF COL_LENGTH('Leads', 'MonthlyIncome') IS NULL
    ALTER TABLE [Leads] ADD [MonthlyIncome] decimal(18,2) NOT NULL CONSTRAINT [DF_Leads_MonthlyIncome] DEFAULT 0;
IF COL_LENGTH('Leads', 'DesiredMoveInDate') IS NULL
    ALTER TABLE [Leads] ADD [DesiredMoveInDate] date NULL;
IF COL_LENGTH('Leads', 'OccupantCount') IS NULL
    ALTER TABLE [Leads] ADD [OccupantCount] int NOT NULL CONSTRAINT [DF_Leads_OccupantCount] DEFAULT 0;
IF COL_LENGTH('Leads', 'HasPets') IS NULL
    ALTER TABLE [Leads] ADD [HasPets] bit NOT NULL CONSTRAINT [DF_Leads_HasPets] DEFAULT 0;
IF COL_LENGTH('Leads', 'CreditScore') IS NULL
    ALTER TABLE [Leads] ADD [CreditScore] int NOT NULL CONSTRAINT [DF_Leads_CreditScore] DEFAULT 0;
IF COL_LENGTH('Leads', 'ConsentToScreening') IS NULL
    ALTER TABLE [Leads] ADD [ConsentToScreening] bit NOT NULL CONSTRAINT [DF_Leads_ConsentToScreening] DEFAULT 0;
IF OBJECT_ID('Showings', 'U') IS NULL
BEGIN
    CREATE TABLE [Showings] (
        [Id] uniqueidentifier NOT NULL,
        [CreatedUtc] datetime2 NOT NULL,
        [ModifiedUtc] datetime2 NULL,
        [OrganizationId] uniqueidentifier NOT NULL,
        [ListingId] uniqueidentifier NOT NULL,
        [LeadId] uniqueidentifier NULL,
        [ScheduledUtc] datetime2 NOT NULL,
        [Status] nvarchar(max) NOT NULL,
        [Notes] nvarchar(max) NOT NULL,
        CONSTRAINT [PK_Showings] PRIMARY KEY ([Id])
    );
END;
IF OBJECT_ID('Invoices', 'U') IS NULL
BEGIN
    CREATE TABLE [Invoices] (
        [Id] uniqueidentifier NOT NULL,
        [CreatedUtc] datetime2 NOT NULL,
        [ModifiedUtc] datetime2 NULL,
        [OrganizationId] uniqueidentifier NOT NULL,
        [LeaseId] uniqueidentifier NOT NULL,
        [Number] nvarchar(max) NOT NULL,
        [DueDate] date NOT NULL,
        [Amount] decimal(18,2) NOT NULL,
        [Balance] decimal(18,2) NOT NULL,
        [Status] int NOT NULL,
        [Notes] nvarchar(max) NOT NULL,
        CONSTRAINT [PK_Invoices] PRIMARY KEY ([Id])
    );
END;
IF COL_LENGTH('Leases', 'DepositReceived') IS NULL
    ALTER TABLE [Leases] ADD [DepositReceived] bit NOT NULL CONSTRAINT [DF_Leases_DepositReceived] DEFAULT 0;
IF COL_LENGTH('Leases', 'InsuranceProofReceived') IS NULL
    ALTER TABLE [Leases] ADD [InsuranceProofReceived] bit NOT NULL CONSTRAINT [DF_Leases_InsuranceProofReceived] DEFAULT 0;
IF COL_LENGTH('Leases', 'MoveInChecklistCompleted') IS NULL
    ALTER TABLE [Leases] ADD [MoveInChecklistCompleted] bit NOT NULL CONSTRAINT [DF_Leases_MoveInChecklistCompleted] DEFAULT 0;
IF COL_LENGTH('Leases', 'MoveInNotes') IS NULL
    ALTER TABLE [Leases] ADD [MoveInNotes] nvarchar(max) NOT NULL CONSTRAINT [DF_Leases_MoveInNotes] DEFAULT N'';
IF OBJECT_ID('PaymentEntries', 'U') IS NULL
BEGIN
    CREATE TABLE [PaymentEntries] (
        [Id] uniqueidentifier NOT NULL,
        [CreatedUtc] datetime2 NOT NULL,
        [ModifiedUtc] datetime2 NULL,
        [OrganizationId] uniqueidentifier NOT NULL,
        [InvoiceId] uniqueidentifier NOT NULL,
        [ReceivedDate] date NOT NULL,
        [Amount] decimal(18,2) NOT NULL,
        [Method] nvarchar(max) NOT NULL,
        [Reference] nvarchar(max) NOT NULL,
        [Notes] nvarchar(max) NOT NULL,
        CONSTRAINT [PK_PaymentEntries] PRIMARY KEY ([Id])
    );
END;
IF OBJECT_ID('MediaAssets', 'U') IS NULL
BEGIN
    CREATE TABLE [MediaAssets] (
        [Id] uniqueidentifier NOT NULL,
        [CreatedUtc] datetime2 NOT NULL,
        [ModifiedUtc] datetime2 NULL,
        [OrganizationId] uniqueidentifier NOT NULL,
        [PropertyId] uniqueidentifier NULL,
        [UnitId] uniqueidentifier NULL,
        [ListingId] uniqueidentifier NULL,
        [MaintenanceRequestId] uniqueidentifier NULL,
        [FileName] nvarchar(max) NOT NULL,
        [BlobPath] nvarchar(max) NOT NULL,
        [Caption] nvarchar(max) NOT NULL,
        [SortOrder] int NOT NULL,
        [IsPrimary] bit NOT NULL,
        [Category] int NOT NULL,
        CONSTRAINT [PK_MediaAssets] PRIMARY KEY ([Id])
    );
END;
IF COL_LENGTH('MediaAssets', 'ListingId') IS NULL
    ALTER TABLE [MediaAssets] ADD [ListingId] uniqueidentifier NULL;
IF COL_LENGTH('MediaAssets', 'LeaseId') IS NULL
    ALTER TABLE [MediaAssets] ADD [LeaseId] uniqueidentifier NULL;
IF COL_LENGTH('MediaAssets', 'DocumentType') IS NULL
    ALTER TABLE [MediaAssets] ADD [DocumentType] nvarchar(max) NOT NULL CONSTRAINT [DF_MediaAssets_DocumentType] DEFAULT N'';
IF OBJECT_ID('AISuggestionLogs', 'U') IS NULL
BEGIN
    CREATE TABLE [AISuggestionLogs] (
        [Id] uniqueidentifier NOT NULL,
        [CreatedUtc] datetime2 NOT NULL,
        [ModifiedUtc] datetime2 NULL,
        [OrganizationId] uniqueidentifier NOT NULL,
        [SuggestionType] int NOT NULL,
        [SourceEntityName] nvarchar(max) NOT NULL,
        [SourceEntityId] uniqueidentifier NULL,
        [PromptSummary] nvarchar(max) NOT NULL,
        [SuggestedContent] nvarchar(max) NOT NULL,
        [ReviewedByHuman] bit NOT NULL,
        [ReviewOutcome] nvarchar(max) NOT NULL,
        CONSTRAINT [PK_AISuggestionLogs] PRIMARY KEY ([Id])
    );
END;
IF OBJECT_ID('TenantConversations', 'U') IS NULL
BEGIN
    CREATE TABLE [TenantConversations] (
        [Id] uniqueidentifier NOT NULL,
        [CreatedUtc] datetime2 NOT NULL,
        [ModifiedUtc] datetime2 NULL,
        [OrganizationId] uniqueidentifier NOT NULL,
        [TenantId] uniqueidentifier NOT NULL,
        [LeaseId] uniqueidentifier NULL,
        [MaintenanceRequestId] uniqueidentifier NULL,
        [Subject] nvarchar(max) NOT NULL,
        [Channel] int NOT NULL,
        [Status] nvarchar(max) NOT NULL,
        [LastContactUtc] datetime2 NULL,
        CONSTRAINT [PK_TenantConversations] PRIMARY KEY ([Id])
    );
END;
IF OBJECT_ID('TenantMessages', 'U') IS NULL
BEGIN
    CREATE TABLE [TenantMessages] (
        [Id] uniqueidentifier NOT NULL,
        [CreatedUtc] datetime2 NOT NULL,
        [ModifiedUtc] datetime2 NULL,
        [OrganizationId] uniqueidentifier NOT NULL,
        [TenantConversationId] uniqueidentifier NOT NULL,
        [IsIncoming] bit NOT NULL,
        [Body] nvarchar(max) NOT NULL,
        [SentBy] nvarchar(max) NOT NULL,
        [SentUtc] datetime2 NOT NULL,
        [IsAISuggested] bit NOT NULL,
        CONSTRAINT [PK_TenantMessages] PRIMARY KEY ([Id])
    );
END;
IF COL_LENGTH('TenantMessages', 'DeliveryMethod') IS NULL
    ALTER TABLE [TenantMessages] ADD [DeliveryMethod] nvarchar(max) NOT NULL CONSTRAINT [DF_TenantMessages_DeliveryMethod] DEFAULT N'';
IF COL_LENGTH('TenantMessages', 'DeliveredUtc') IS NULL
    ALTER TABLE [TenantMessages] ADD [DeliveredUtc] datetime2 NULL;
IF COL_LENGTH('TenantMessages', 'DeliveryProof') IS NULL
    ALTER TABLE [TenantMessages] ADD [DeliveryProof] nvarchar(max) NOT NULL CONSTRAINT [DF_TenantMessages_DeliveryProof] DEFAULT N'';
IF COL_LENGTH('Vendors', 'DispatchStatus') IS NULL
    ALTER TABLE [Vendors] ADD [DispatchStatus] nvarchar(max) NOT NULL CONSTRAINT [DF_Vendors_DispatchStatus] DEFAULT N'Available';
IF COL_LENGTH('Vendors', 'PreferredForPriority') IS NULL
    ALTER TABLE [Vendors] ADD [PreferredForPriority] nvarchar(max) NOT NULL CONSTRAINT [DF_Vendors_PreferredForPriority] DEFAULT N'';
IF COL_LENGTH('Vendors', 'TypicalResponseHours') IS NULL
    ALTER TABLE [Vendors] ADD [TypicalResponseHours] int NOT NULL CONSTRAINT [DF_Vendors_TypicalResponseHours] DEFAULT 0;
IF COL_LENGTH('MaintenanceRequests', 'DispatchStatus') IS NULL
    ALTER TABLE [MaintenanceRequests] ADD [DispatchStatus] nvarchar(max) NOT NULL CONSTRAINT [DF_MaintenanceRequests_DispatchStatus] DEFAULT N'Unassigned';
IF NOT EXISTS (SELECT 1 FROM [OrganizationMemberships] WHERE [OrganizationId] = '11111111-1111-1111-1111-111111111111' AND [UserId] = '66666666-6666-6666-6666-666666666666')
BEGIN
    INSERT INTO [OrganizationMemberships] ([Id], [CreatedUtc], [ModifiedUtc], [OrganizationId], [UserId], [Role], [Status])
    VALUES ('dddddddd-dddd-dddd-dddd-dddddddddddd', SYSUTCDATETIME(), NULL, '11111111-1111-1111-1111-111111111111', '66666666-6666-6666-6666-666666666666', 'Owner', 'Active');
END;
IF COL_LENGTH('Organizations', 'TrialEndsUtc') IS NOT NULL
BEGIN
    EXEC(N'UPDATE [Organizations]
    SET [TrialEndsUtc] = DATEADD(day, 14, [CreatedUtc])
    WHERE [TrialEndsUtc] IS NULL AND [SubscriptionTier] = 0;');
END;
""");
}

static void PurgeExpiredDemoOrganizations(ApplicationDbContext db)
{
    var expiredDemoIds = db.Organizations
        .AsNoTracking()
        .Where(x => x.IsDemo && x.DemoExpiresUtc.HasValue && x.DemoExpiresUtc.Value < DateTime.UtcNow)
        .Select(x => x.Id)
        .ToList();

    if (expiredDemoIds.Count == 0)
    {
        return;
    }

    var usersToDetach = db.Users.Where(x => x.OrganizationId.HasValue && expiredDemoIds.Contains(x.OrganizationId.Value)).ToList();
    foreach (var user in usersToDetach)
    {
        user.OrganizationId = null;
    }

    db.AuditLogs.Where(x => expiredDemoIds.Contains(x.OrganizationId)).ExecuteDelete();
    db.ComplianceReminders.Where(x => expiredDemoIds.Contains(x.OrganizationId)).ExecuteDelete();
    db.DocumentTemplates.Where(x => expiredDemoIds.Contains(x.OrganizationId)).ExecuteDelete();
    db.TenantMessages.Where(x => expiredDemoIds.Contains(x.OrganizationId)).ExecuteDelete();
    db.TenantConversations.Where(x => expiredDemoIds.Contains(x.OrganizationId)).ExecuteDelete();
    db.MaintenanceRequests.Where(x => expiredDemoIds.Contains(x.OrganizationId)).ExecuteDelete();
    db.Leases.Where(x => expiredDemoIds.Contains(x.OrganizationId)).ExecuteDelete();
    db.Tenants.Where(x => expiredDemoIds.Contains(x.OrganizationId)).ExecuteDelete();
    db.Units.Where(x => expiredDemoIds.Contains(x.OrganizationId)).ExecuteDelete();
    db.Properties.Where(x => expiredDemoIds.Contains(x.OrganizationId)).ExecuteDelete();
    db.OrganizationInvitations.Where(x => expiredDemoIds.Contains(x.OrganizationId)).ExecuteDelete();
    db.OrganizationMemberships.Where(x => expiredDemoIds.Contains(x.OrganizationId)).ExecuteDelete();

    db.SaveChanges();
    db.Organizations.Where(x => expiredDemoIds.Contains(x.Id)).ExecuteDelete();
}

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();

internal sealed record SupportAlertRequest(string? Subject, string? Message);
internal sealed record SupportGrantRequest(Guid OrganizationId, string? Reason);


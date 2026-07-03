using System.Globalization;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MudBlazor.Services;
using Runtira.Application.Abstractions;
using Runtira.Application.Common;
using Runtira.Application.Features;
using Runtira.Infrastructure.Data;
using Runtira.Infrastructure.Options;
using Runtira.Infrastructure.Services;
using Runtira.Web.Components;
using Runtira.Web.Localization;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseStaticWebAssets();
builder.Configuration
    .AddJsonFile(Path.Combine(AppContext.BaseDirectory, "appsettings.json"), optional: true, reloadOnChange: false)
    .AddJsonFile(Path.Combine(AppContext.BaseDirectory, $"appsettings.{builder.Environment.EnvironmentName}.json"), optional: true, reloadOnChange: false)
    .AddUserSecrets<Program>(optional: true, reloadOnChange: false);
var useLocalDevelopmentAuth = builder.Environment.IsDevelopment()
    && builder.Configuration.GetValue("Authentication:UseLocalDevelopmentAuth", true);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddLocalization();
builder.Services.AddHttpContextAccessor();
builder.Services.AddMudServices();
builder.Services.AddRuntiraApplication();
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

var runtiraInfrastructureEnabled = !string.IsNullOrWhiteSpace(builder.Configuration.GetConnectionString("RuntiraDb"))
    || !string.IsNullOrWhiteSpace(builder.Configuration.GetConnectionString("PropertyDb"));

if (runtiraInfrastructureEnabled)
{
    builder.Services.AddRuntiraInfrastructure(builder.Configuration);
}

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

builder.Services.AddScoped<CurrentOrganization>(provider =>
{
    var httpContextAccessor = provider.GetRequiredService<IHttpContextAccessor>();
    var request = httpContextAccessor.HttpContext?.Request;
    var user = httpContextAccessor.HttpContext?.User;
    var tenantSlug = request?.Path.Value?
        .Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
        .FirstOrDefault() ?? string.Empty;

    if (string.Equals(tenantSlug, "not-found", StringComparison.OrdinalIgnoreCase))
    {
        tenantSlug = string.Empty;
    }

    if (runtiraInfrastructureEnabled)
    {
        try
        {
            var dbOptions = provider.GetService<DbContextOptions<ApplicationDbContext>>();
            if (dbOptions is not null)
            {
                using var db = new ApplicationDbContext(dbOptions);

                var userEmail = user?.FindFirstValue(ClaimTypes.Email)
                    ?? user?.FindFirstValue("email")
                    ?? string.Empty;
                var clerkUserId = user?.FindFirstValue(ClaimTypes.NameIdentifier)
                    ?? user?.FindFirstValue("sub")
                    ?? string.Empty;
                var acceptLanguage = request?.Headers.AcceptLanguage.ToString();
                var userLocale = user?.FindFirstValue("locale")
                    ?? user?.FindFirstValue("ui_locale")
                    ?? ResolvePreferredLanguageFromHeader(acceptLanguage)
                    ?? string.Empty;
                var headerRegion = acceptLanguage?.ToUpperInvariant() switch
                {
                    var value when !string.IsNullOrWhiteSpace(value) && value.Contains("FR-CA") => "QC",
                    var value when !string.IsNullOrWhiteSpace(value) && value.Contains("EN-CA") => "ON",
                    var value when !string.IsNullOrWhiteSpace(value) && (value.Contains("EN-US") || value.Contains("ES-MX")) => "TX",
                    _ => string.Empty
                };
                var regionClaim = user?.FindFirstValue("region")
                    ?? user?.FindFirstValue("zoneinfo")
                    ?? headerRegion;

                var organizationQuery = db.RuntiraOrganizations.AsNoTracking();
                Runtira.Domain.Entities.RuntiraOrganization? organization = null;

                if (!string.IsNullOrWhiteSpace(tenantSlug))
                {
                    organization = organizationQuery.FirstOrDefault(x => x.Slug == tenantSlug);
                }

                var matchedUser = string.IsNullOrWhiteSpace(userEmail) && string.IsNullOrWhiteSpace(clerkUserId)
                    ? null
                    : db.RuntiraUsers
                        .AsNoTracking()
                        .FirstOrDefault(x =>
                            (!string.IsNullOrWhiteSpace(userEmail) && x.Email == userEmail)
                            || (!string.IsNullOrWhiteSpace(clerkUserId) && x.ClerkUserId == clerkUserId));

                var memberships = matchedUser is null
                    ? new List<Runtira.Domain.Entities.RuntiraMembership>()
                    : db.RuntiraMemberships
                        .AsNoTracking()
                        .Where(x => x.UserId == matchedUser.Id)
                        .OrderByDescending(x => x.LastSelectedUtc ?? x.CreatedUtc)
                        .ToList();

                if (organization is null && memberships.Count > 0)
                {
                    var candidateTenantIds = memberships.Select(x => x.TenantId).ToList();
                    organization = organizationQuery.FirstOrDefault(x => candidateTenantIds.Contains(x.Id));
                }

                organization ??= organizationQuery.OrderBy(x => x.Name).FirstOrDefault();

                if (organization is not null)
                {
                    var activeMembership = memberships.FirstOrDefault(x => x.TenantId == organization.Id);
                    var accessibleOrganizationCount = memberships.Select(x => x.TenantId).Distinct().Count();
                    var isSuperAdmin = matchedUser?.IsSuperAdmin == true
                        || string.Equals(organization.OwnerEmail, "michelfopa@gmail.com", StringComparison.OrdinalIgnoreCase)
                        || string.Equals(userEmail, "michelfopa@gmail.com", StringComparison.OrdinalIgnoreCase);

                    var effectiveLocale = !string.IsNullOrWhiteSpace(userLocale)
                        ? userLocale
                        : matchedUser?.PreferredLanguage ?? organization.DefaultLocale;
                    var effectiveRegion = !string.IsNullOrWhiteSpace(ResolveRegionFromClaim(regionClaim))
                        ? ResolveRegionFromClaim(regionClaim)
                        : organization.RegionCode;
                    var effectiveCountryCode = !string.IsNullOrWhiteSpace(organization.CountryCode)
                        ? organization.CountryCode
                        : ResolveCountryCode(effectiveRegion);

                    return new CurrentOrganization
                    {
                        UserId = matchedUser?.Id ?? Guid.Empty,
                        OrganizationId = organization.Id,
                        AccessibleOrganizationCount = Math.Max(accessibleOrganizationCount, 1),
                        HasSuperAdminOrganizationSelection = isSuperAdmin,
                        OrganizationName = organization.Name,
                        OrganizationSlug = organization.Slug,
                        UserEmail = !string.IsNullOrWhiteSpace(userEmail) ? userEmail : organization.OwnerEmail,
                        UserFullName = matchedUser?.FullName ?? user?.Identity?.Name ?? organization.OwnerEmail,
                        Role = activeMembership?.Role ?? (isSuperAdmin ? "SuperAdmin" : "Owner"),
                        SystemRole = isSuperAdmin ? "SuperAdmin" : "User",
                        Province = string.IsNullOrWhiteSpace(effectiveRegion) ? "AB" : effectiveRegion,
                        CountryCode = effectiveCountryCode,
                        PreferredLanguage = ResolvePreferredLanguage(effectiveLocale),
                        SubscriptionIsActive = organization.IsActive,
                        TrialExpired = false
                    };
                }
            }
        }
        catch
        {
        }
    }

    var fallbackAcceptLanguage = request?.Headers.AcceptLanguage.ToString();
    var fallbackLocale = user?.FindFirstValue("locale")
        ?? user?.FindFirstValue("ui_locale")
        ?? ResolvePreferredLanguageFromHeader(fallbackAcceptLanguage);
    var fallbackHeaderRegion = fallbackAcceptLanguage?.ToUpperInvariant() switch
    {
        var value when !string.IsNullOrWhiteSpace(value) && value.Contains("FR-CA") => "QC",
        var value when !string.IsNullOrWhiteSpace(value) && value.Contains("EN-CA") => "ON",
        var value when !string.IsNullOrWhiteSpace(value) && (value.Contains("EN-US") || value.Contains("ES-MX")) => "TX",
        _ => string.Empty
    };
    var fallbackRegion = ResolveRegionFromClaim(user?.FindFirstValue("region") ?? user?.FindFirstValue("zoneinfo") ?? fallbackHeaderRegion);
    var province = string.IsNullOrWhiteSpace(fallbackRegion) ? "AB" : fallbackRegion;
    var organizationName = string.IsNullOrWhiteSpace(tenantSlug) ? "Runtira Demo" : ToDisplayName(tenantSlug);

    return new CurrentOrganization
    {
        OrganizationId = Guid.Empty,
        AccessibleOrganizationCount = 0,
        HasSuperAdminOrganizationSelection = string.Equals(user?.FindFirstValue(ClaimTypes.Email), "michelfopa@gmail.com", StringComparison.OrdinalIgnoreCase),
        OrganizationName = organizationName,
        OrganizationSlug = tenantSlug,
        UserEmail = user?.FindFirstValue(ClaimTypes.Email) ?? string.Empty,
        UserFullName = user?.Identity?.Name ?? "Workspace invité",
        Role = string.IsNullOrWhiteSpace(tenantSlug) ? "Guest" : "Owner",
        SystemRole = string.Equals(user?.FindFirstValue(ClaimTypes.Email), "michelfopa@gmail.com", StringComparison.OrdinalIgnoreCase) ? "SuperAdmin" : (string.IsNullOrWhiteSpace(tenantSlug) ? "Guest" : "User"),
        Province = province,
        CountryCode = ResolveCountryCode(province),
        PreferredLanguage = ResolvePreferredLanguage(fallbackLocale),
        SubscriptionIsActive = true,
        TrialExpired = false
    };
});

builder.Services.AddScoped<ITenantContextAccessor>(provider =>
{
    var currentOrganization = provider.GetRequiredService<CurrentOrganization>();
    return new TenantContext
    {
        TenantId = currentOrganization.OrganizationId == Guid.Empty ? null : currentOrganization.OrganizationId,
        BypassTenantFilter = false,
        TenantSlug = currentOrganization.OrganizationSlug,
        UserEmail = currentOrganization.UserEmail
    };
});

var app = builder.Build();

var supportedCultures = new[]
{
    new CultureInfo("fr-CA"),
    new CultureInfo("en-CA"),
    new CultureInfo("es-MX")
};

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseRequestLocalization(new RequestLocalizationOptions
{
    DefaultRequestCulture = new RequestCulture("fr-CA"),
    SupportedCultures = supportedCultures,
    SupportedUICultures = supportedCultures
});

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
                new Claim(ClaimTypes.Name, "michelfopa@gmail.com"),
                new Claim(ClaimTypes.Email, "michelfopa@gmail.com"),
                new Claim(ClaimTypes.Role, "Owner")
            };

            context.User = new ClaimsPrincipal(new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme));
        }
    }

    await next();
});
app.UseAuthorization();

app.UseAntiforgery();

app.MapGet("/account/login", async (HttpContext httpContext) =>
{
    if (!useClerkAuthentication)
    {
        httpContext.Response.Redirect("/");
        return;
    }

    await Microsoft.AspNetCore.Authentication.AuthenticationHttpContextExtensions.ChallengeAsync(httpContext, OpenIdConnectDefaults.AuthenticationScheme, new Microsoft.AspNetCore.Authentication.AuthenticationProperties { RedirectUri = "/" });
});

app.MapGet("/account/sign-up", (HttpContext httpContext, [FromServices] ClerkOptions options) =>
{
    var signInUrl = string.IsNullOrWhiteSpace(options.SignUpUrl) ? "/" : options.SignUpUrl;
    return Results.Redirect(signInUrl);
});

app.MapPost("/api/providers/resend/test", async (HttpContext httpContext, [FromServices] CurrentOrganization currentOrganization, [FromServices] IEmailService emailService) =>
{
    var recipient = string.IsNullOrWhiteSpace(currentOrganization.UserEmail) ? "support@runtira.com" : currentOrganization.UserEmail;
    var region = $"{currentOrganization.CountryCode}-{currentOrganization.Province}";
    var language = currentOrganization.PreferredLanguage;
    var displayName = string.IsNullOrWhiteSpace(currentOrganization.UserFullName) ? currentOrganization.UserEmail : currentOrganization.UserFullName;
    var subject = RuntiraText.Format("Billing_WebhookSubject", language, region);
    var html = RuntiraText.Format("Billing_WebhookHtml", language, displayName, currentOrganization.OrganizationName, language, region);
    var text = RuntiraText.Format("Billing_WebhookText", language, currentOrganization.OrganizationName, language, region);
    await emailService.SendAsync(recipient, subject, html, text, httpContext.RequestAborted);
    return Results.Ok(new { recipient, region, language });
});

app.MapPost("/api/invoices/draft-email", async (HttpContext httpContext, [FromServices] CurrentOrganization currentOrganization, [FromServices] RuntiraWorkspaceService workspaceService, [FromServices] IEmailService emailService) =>
{
    var invoice = await workspaceService.GetInvoiceComposerAsync(httpContext.RequestAborted);
    if (invoice is null)
    {
        return Results.NotFound();
    }

    var recipient = string.IsNullOrWhiteSpace(currentOrganization.UserEmail) ? "support@runtira.com" : currentOrganization.UserEmail;
    var language = currentOrganization.PreferredLanguage;
    var subject = RuntiraText.Format("Notification_InvoiceReadySubject", language, invoice.OrganizationName);
    var html = RuntiraText.Format("Notification_InvoiceReadyHtml", language, invoice.OrganizationName, invoice.BillingPeriod, invoice.JurisdictionDisplayName, language);
    var text = RuntiraText.Format("Notification_InvoiceReadyText", language, invoice.OrganizationName, invoice.BillingPeriod, invoice.JurisdictionDisplayName, language);
    await emailService.SendAsync(recipient, subject, html, text, httpContext.RequestAborted);
    return Results.Ok(new { recipient, language, invoice.OrganizationName, invoice.BillingPeriod });
});

app.MapGet("/{tenantSlug}/billing/checkout/{plan}", async (string tenantSlug, string plan, HttpContext httpContext, [FromServices] Runtira.Infrastructure.Data.ApplicationDbContext db, [FromServices] Runtira.Infrastructure.Services.StripeBillingService billingService) =>
{
    var organization = await db.RuntiraOrganizations.FirstOrDefaultAsync(x => x.Slug == tenantSlug);
    if (organization is null)
    {
        return Results.LocalRedirect($"/{tenantSlug}");
    }

    var root = $"{httpContext.Request.Scheme}://{httpContext.Request.Host}";
    var url = await billingService.CreateCheckoutSessionAsync(organization, plan, $"{root}/{tenantSlug}/billing", $"{root}/{tenantSlug}/billing");
    return Results.Redirect(url);
});

app.MapGet("/{tenantSlug}/billing/portal", async (string tenantSlug, HttpContext httpContext, [FromServices] Runtira.Infrastructure.Data.ApplicationDbContext db, [FromServices] Runtira.Infrastructure.Services.StripeBillingService billingService) =>
{
    var organization = await db.RuntiraOrganizations.FirstOrDefaultAsync(x => x.Slug == tenantSlug);
    var root = $"{httpContext.Request.Scheme}://{httpContext.Request.Host}";
    var url = await billingService.CreateBillingPortalAsync(organization?.StripeCustomerId ?? string.Empty, $"{root}/{tenantSlug}/billing");
    return Results.Redirect(url);
});

app.MapPost("/billing/webhook", async (HttpContext httpContext, [FromServices] Runtira.Infrastructure.Services.StripeBillingService billingService) =>
{
    using var reader = new StreamReader(httpContext.Request.Body);
    var json = await reader.ReadToEndAsync();
    await billingService.HandleWebhookAsync(json, httpContext.Request.Headers["Stripe-Signature"]);
    return Results.Ok();
});

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();

static string ResolvePreferredLanguage(string? preferredLocale = null)
{
    if (!string.IsNullOrWhiteSpace(preferredLocale))
    {
        if (preferredLocale.StartsWith("es", StringComparison.OrdinalIgnoreCase))
        {
            return "es-MX";
        }

        if (preferredLocale.StartsWith("en", StringComparison.OrdinalIgnoreCase))
        {
            return "en-CA";
        }

        if (preferredLocale.StartsWith("fr", StringComparison.OrdinalIgnoreCase))
        {
            return "fr-CA";
        }
    }

    var cultureName = CultureInfo.CurrentUICulture.Name;
    if (cultureName.StartsWith("es", StringComparison.OrdinalIgnoreCase))
    {
        return "es-MX";
    }

    if (cultureName.StartsWith("en", StringComparison.OrdinalIgnoreCase))
    {
        return "en-CA";
    }

    return "fr-CA";
}

static string? ResolvePreferredLanguageFromHeader(string? acceptLanguage)
{
    if (string.IsNullOrWhiteSpace(acceptLanguage))
    {
        return null;
    }

    var firstLanguage = acceptLanguage.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).FirstOrDefault();
    return firstLanguage;
}

static string ResolveRegionFromClaim(string? regionOrTimeZone)
{
    if (string.IsNullOrWhiteSpace(regionOrTimeZone))
    {
        return string.Empty;
    }

    var value = regionOrTimeZone.Trim();
    if (value.Length == 2)
    {
        return value.ToUpperInvariant();
    }

    if (value.Contains("Toronto", StringComparison.OrdinalIgnoreCase))
    {
        return "ON";
    }

    if (value.Contains("Montreal", StringComparison.OrdinalIgnoreCase))
    {
        return "QC";
    }

    if (value.Contains("Edmonton", StringComparison.OrdinalIgnoreCase) || value.Contains("Calgary", StringComparison.OrdinalIgnoreCase))
    {
        return "AB";
    }

    if (value.Contains("Chicago", StringComparison.OrdinalIgnoreCase) || value.Contains("Dallas", StringComparison.OrdinalIgnoreCase))
    {
        return "TX";
    }

    return string.Empty;
}

static string ResolveCountryCode(string? regionCode)
{
    return regionCode?.ToUpperInvariant() switch
    {
        "TX" => "US",
        _ => "CA"
    };
}

static string ToDisplayName(string slug)
{
    if (string.IsNullOrWhiteSpace(slug))
    {
        return "Runtira Demo";
    }

    return string.Join(' ', slug.Split('-', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
        .Select(part => char.ToUpperInvariant(part[0]) + part[1..]));
}

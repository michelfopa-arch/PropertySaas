using System.Globalization;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using MudBlazor.Services;
using Runtira.Application.Abstractions;
using Runtira.Application.Common;
using Runtira.Application.Features;
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

var cosmosEnabled = builder.Configuration.GetSection("Cosmos").GetValue("Enabled", false);
var runtiraInfrastructureEnabled = cosmosEnabled;

if (runtiraInfrastructureEnabled)
{
    builder.Services.AddRuntiraInfrastructure(builder.Configuration);
}

var clerkSection = builder.Configuration.GetSection("Clerk");
var clerkOptions = clerkSection.Get<ClerkOptions>();
var mockModeEnabled = builder.Configuration.GetSection("Cosmos").GetValue("MockModeEnabled", true);
var useClerkAuthentication = !useLocalDevelopmentAuth
    && !mockModeEnabled
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
    var currentOrganization = new CurrentOrganization();
    var httpContext = provider.GetRequiredService<IHttpContextAccessor>().HttpContext;
    var request = httpContext?.Request;
    var user = httpContext?.User;
    var segments = request?.Path.Value?
        .Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
        ?? Array.Empty<string>();
    var tenantSlug = segments.FirstOrDefault() ?? string.Empty;
    var propertySlug = segments.Length > 1 && !string.Equals(segments[1], "billing", StringComparison.OrdinalIgnoreCase)
        && !string.Equals(segments[1], "account", StringComparison.OrdinalIgnoreCase)
        && !string.Equals(segments[1], "sign-in", StringComparison.OrdinalIgnoreCase)
        && !string.Equals(segments[1], "sign-up", StringComparison.OrdinalIgnoreCase)
        && !string.Equals(segments[1], "dashboard", StringComparison.OrdinalIgnoreCase)
        && !string.Equals(segments[1], "properties", StringComparison.OrdinalIgnoreCase)
        && !string.Equals(segments[1], "units", StringComparison.OrdinalIgnoreCase)
        && !string.Equals(segments[1], "residents", StringComparison.OrdinalIgnoreCase)
        && !string.Equals(segments[1], "leases", StringComparison.OrdinalIgnoreCase)
        && !string.Equals(segments[1], "documents", StringComparison.OrdinalIgnoreCase)
        && !string.Equals(segments[1], "maintenance", StringComparison.OrdinalIgnoreCase)
        && !string.Equals(segments[1], "imports", StringComparison.OrdinalIgnoreCase)
        && !string.Equals(segments[1], "exports", StringComparison.OrdinalIgnoreCase)
        && !string.Equals(segments[1], "invoice-composer", StringComparison.OrdinalIgnoreCase)
        && !string.Equals(segments[1], "inbox", StringComparison.OrdinalIgnoreCase)
        && !string.Equals(segments[1], "legislation", StringComparison.OrdinalIgnoreCase)
        && !string.Equals(segments[1], "settings", StringComparison.OrdinalIgnoreCase)
        && !string.Equals(segments[1], "onboarding", StringComparison.OrdinalIgnoreCase)
        && !string.Equals(segments[1], "not-found", StringComparison.OrdinalIgnoreCase)
        ? segments[1]
        : string.Empty;

    if (string.Equals(tenantSlug, "not-found", StringComparison.OrdinalIgnoreCase))
    {
        tenantSlug = string.Empty;
    }

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

    if (runtiraInfrastructureEnabled)
    {
        try
        {
            var readModelStore = provider.GetRequiredService<IRuntiraReadModelStore>();
            var resolved = Task.Run(() => readModelStore.ResolveCurrentOrganizationAsync(
                tenantSlug,
                user?.FindFirstValue(ClaimTypes.Email) ?? user?.FindFirstValue("email") ?? string.Empty,
                user?.FindFirstValue(ClaimTypes.NameIdentifier) ?? user?.FindFirstValue("sub") ?? string.Empty,
                ResolvePreferredLanguage(userLocale),
                ResolveRegionFromClaim(regionClaim),
                user?.Identity?.Name ?? string.Empty,
                httpContext?.RequestAborted ?? CancellationToken.None)).GetAwaiter().GetResult();

            if (resolved is not null)
            {
                CopyCurrentOrganization(resolved, currentOrganization);
                currentOrganization.PropertySlug = propertySlug;
                currentOrganization.PropertyName = string.IsNullOrWhiteSpace(propertySlug) ? string.Empty : ToDisplayName(propertySlug);
                return currentOrganization;
            }
        }
        catch
        {
        }
    }

    var fallbackRegion = ResolveRegionFromClaim(regionClaim);
    var province = string.IsNullOrWhiteSpace(fallbackRegion) ? "AB" : fallbackRegion;
    var organizationName = string.IsNullOrWhiteSpace(tenantSlug) ? "Runtira Demo" : ToDisplayName(tenantSlug);

    currentOrganization.OrganizationId = Guid.Empty;
    currentOrganization.AccessibleOrganizationCount = 0;
    currentOrganization.HasSuperAdminOrganizationSelection = string.Equals(user?.FindFirstValue(ClaimTypes.Email), "michelfopa@gmail.com", StringComparison.OrdinalIgnoreCase);
    currentOrganization.OrganizationName = organizationName;
    currentOrganization.OrganizationSlug = tenantSlug;
    currentOrganization.PropertySlug = propertySlug;
    currentOrganization.PropertyName = string.IsNullOrWhiteSpace(propertySlug) ? string.Empty : ToDisplayName(propertySlug);
    currentOrganization.UserEmail = user?.FindFirstValue(ClaimTypes.Email) ?? string.Empty;
    currentOrganization.UserFullName = user?.Identity?.Name ?? "Workspace invité";
    currentOrganization.Role = string.IsNullOrWhiteSpace(tenantSlug) ? "Guest" : "Owner";
    currentOrganization.SystemRole = string.Equals(user?.FindFirstValue(ClaimTypes.Email), "michelfopa@gmail.com", StringComparison.OrdinalIgnoreCase) ? "SuperAdmin" : (string.IsNullOrWhiteSpace(tenantSlug) ? "Guest" : "User");
    currentOrganization.Province = province;
    currentOrganization.CountryCode = ResolveCountryCode(province);
    currentOrganization.PreferredLanguage = ResolvePreferredLanguage(userLocale);
    currentOrganization.SubscriptionIsActive = true;
    currentOrganization.TrialExpired = false;
    currentOrganization.OrganizationOptions = Array.Empty<OrganizationAccessOptionDto>();
    return currentOrganization;
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
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}
app.UseAuthentication();
app.Use(async (context, next) =>
{
    if (!useClerkAuthentication && context.User?.Identity?.IsAuthenticated != true)
    {
        // Fallback for requests that hit a mock-role link without an explicit cookie yet
        // (e.g. first paint before /mock-login redirected). Uses the same defaulting rules
        // as the /mock-login endpoint below, but does not persist a cookie.
        var mockRole = context.Request.Query["mockRole"].ToString();
        if (!string.IsNullOrWhiteSpace(mockRole))
        {
            context.User = BuildMockPrincipal(mockRole);
        }
    }

    await next();
});
app.Use(async (context, next) =>
{
    var currentOrganization = context.RequestServices.GetRequiredService<CurrentOrganization>();
    var request = context.Request;
    var user = context.User;
    var tenantSlug = request.Path.Value?
        .Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
        .FirstOrDefault() ?? string.Empty;

    if (string.Equals(tenantSlug, "not-found", StringComparison.OrdinalIgnoreCase))
    {
        tenantSlug = string.Empty;
    }

    var acceptLanguage = request.Headers.AcceptLanguage.ToString();
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

    if (runtiraInfrastructureEnabled)
    {
        try
        {
            var readModelStore = context.RequestServices.GetRequiredService<IRuntiraReadModelStore>();
            var resolved = await readModelStore.ResolveCurrentOrganizationAsync(
                tenantSlug,
                user?.FindFirstValue(ClaimTypes.Email) ?? user?.FindFirstValue("email") ?? string.Empty,
                user?.FindFirstValue(ClaimTypes.NameIdentifier) ?? user?.FindFirstValue("sub") ?? string.Empty,
                ResolvePreferredLanguage(userLocale),
                ResolveRegionFromClaim(regionClaim),
                user?.Identity?.Name ?? string.Empty,
                context.RequestAborted);

            if (resolved is not null)
            {
                CopyCurrentOrganization(resolved, currentOrganization);
                ApplyMockRoleOverride(currentOrganization, user);
                await next();
                return;
            }
        }
        catch
        {
        }
    }

    var fallbackRegion = ResolveRegionFromClaim(regionClaim);
    var province = string.IsNullOrWhiteSpace(fallbackRegion) ? "AB" : fallbackRegion;
    var organizationName = string.IsNullOrWhiteSpace(tenantSlug) ? "Runtira Demo" : ToDisplayName(tenantSlug);

    currentOrganization.OrganizationId = Guid.Empty;
    currentOrganization.AccessibleOrganizationCount = 0;
    currentOrganization.HasSuperAdminOrganizationSelection = string.Equals(user?.FindFirstValue(ClaimTypes.Email), "michelfopa@gmail.com", StringComparison.OrdinalIgnoreCase);
    currentOrganization.OrganizationName = organizationName;
    currentOrganization.OrganizationSlug = tenantSlug;
    currentOrganization.UserEmail = user?.FindFirstValue(ClaimTypes.Email) ?? string.Empty;
    currentOrganization.UserFullName = user?.Identity?.Name ?? "Workspace invité";
    currentOrganization.Role = string.IsNullOrWhiteSpace(tenantSlug) ? "Guest" : "Owner";
    currentOrganization.SystemRole = string.Equals(user?.FindFirstValue(ClaimTypes.Email), "michelfopa@gmail.com", StringComparison.OrdinalIgnoreCase) ? "SuperAdmin" : (string.IsNullOrWhiteSpace(tenantSlug) ? "Guest" : "User");
    currentOrganization.Province = province;
    currentOrganization.CountryCode = ResolveCountryCode(province);
    currentOrganization.PreferredLanguage = ResolvePreferredLanguage(userLocale);
    currentOrganization.SubscriptionIsActive = true;
    currentOrganization.TrialExpired = false;
    ApplyMockRoleOverride(currentOrganization, user);

    await next();
});
app.UseAuthorization();

app.UseAntiforgery();

app.MapGet("/mock-login", async (HttpContext httpContext, string? mockRole) =>
{
    if (useClerkAuthentication)
    {
        httpContext.Response.Redirect("/");
        return;
    }

    var principal = BuildMockPrincipal(mockRole ?? string.Empty);
    await httpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

    var organizationSlug = (mockRole ?? string.Empty).ToLowerInvariant() switch
    {
        "viewer" => "demo-texas",
        "manager" => "demo-ontario",
        "owner" => "demo-alberta",
        _ => string.Empty
    };

    httpContext.Response.Redirect(string.IsNullOrWhiteSpace(organizationSlug) ? "/" : $"/{organizationSlug}");
});

app.MapGet("/mock-logout", async (HttpContext httpContext) =>
{
    await httpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    httpContext.Response.Redirect("/sign-in");
});

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

app.MapGet("/{tenantSlug}/exports/leads.csv", async (string tenantSlug, HttpContext httpContext, [FromServices] CurrentOrganization currentOrganization, [FromServices] RuntiraWorkspaceService workspaceService) =>
{
    if (!string.Equals(currentOrganization.OrganizationSlug, tenantSlug, StringComparison.OrdinalIgnoreCase))
    {
        return Results.NotFound();
    }

    var export = await workspaceService.ExportLeadsCsvAsync(httpContext.RequestAborted);
    if (export is null || export.Content.Length == 0)
    {
        return Results.NotFound();
    }

    return Results.File(export.Content, export.ContentType, export.FileName);
});

app.MapGet("/{tenantSlug}/billing/checkout/{plan}", async (string tenantSlug, string plan, HttpContext httpContext, [FromServices] IRuntiraReadModelStore readModelStore, [FromServices] Runtira.Infrastructure.Services.StripeBillingService billingService) =>
{
    var billingOrganization = await readModelStore.GetBillingOrganizationAsync(tenantSlug, httpContext.RequestAborted);
    if (billingOrganization is null)
    {
        return Results.LocalRedirect($"/{tenantSlug}");
    }

    var root = $"{httpContext.Request.Scheme}://{httpContext.Request.Host}";
    var currentOrganization = await readModelStore.ResolveCurrentOrganizationAsync(tenantSlug, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, httpContext.RequestAborted);
    var ownerEmail = currentOrganization?.UserEmail ?? string.Empty;
    var url = await billingService.CreateCheckoutSessionAsync(billingOrganization.Value.OrganizationId, ownerEmail, plan, $"{root}/{tenantSlug}/billing", $"{root}/{tenantSlug}/billing");
    return Results.Redirect(url);
});

app.MapGet("/{tenantSlug}/billing/portal", async (string tenantSlug, HttpContext httpContext, [FromServices] IRuntiraReadModelStore readModelStore, [FromServices] Runtira.Infrastructure.Services.StripeBillingService billingService) =>
{
    var billingOrganization = await readModelStore.GetBillingOrganizationAsync(tenantSlug, httpContext.RequestAborted);
    var root = $"{httpContext.Request.Scheme}://{httpContext.Request.Host}";
    var url = await billingService.CreateBillingPortalAsync(billingOrganization?.StripeCustomerId ?? string.Empty, $"{root}/{tenantSlug}/billing");
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
app.MapRazorComponents<Runtira.Web.Components.App>()
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

static ClaimsPrincipal BuildMockPrincipal(string mockRole)
{
    var mockEmail = mockRole.ToLowerInvariant() switch
    {
        "viewer" => "viewer@demo-texas.local",
        "manager" => "manager@demo-ontario.local",
        "owner" => "owner@demo-alberta.local",
        _ => "michelfopa@gmail.com"
    };
    var role = mockRole.ToLowerInvariant() switch
    {
        "viewer" => "Viewer",
        "manager" => "Manager",
        "owner" => "Owner",
        _ => "SuperAdmin"
    };
    var claims = new[]
    {
        new Claim(ClaimTypes.NameIdentifier, $"mock-{role.ToLowerInvariant()}"),
        new Claim(ClaimTypes.Name, mockEmail),
        new Claim(ClaimTypes.Email, mockEmail),
        new Claim(ClaimTypes.Role, role)
    };

    return new ClaimsPrincipal(new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme));
}

static void ApplyMockRoleOverride(CurrentOrganization target, ClaimsPrincipal? user)
{
    // Mock identities are tagged with NameIdentifier "mock-*" in the mock-auth middleware above.
    // Every seeded demo organization shares the same ownerEmail (michelfopa@gmail.com), which would
    // otherwise make ResolveCurrentOrganizationAsync/the fallback branch report SuperAdmin for every
    // mock persona. Force the role that was actually requested via ?mockRole= so viewer/manager/owner
    // links produce a visibly different experience from the super admin one.
    var mockNameIdentifier = user?.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
    if (!mockNameIdentifier.StartsWith("mock-", StringComparison.OrdinalIgnoreCase))
    {
        return;
    }

    var mockRole = user?.FindFirstValue(ClaimTypes.Role) ?? string.Empty;
    if (string.IsNullOrWhiteSpace(mockRole))
    {
        return;
    }

    var isSuperAdmin = string.Equals(mockRole, "SuperAdmin", StringComparison.OrdinalIgnoreCase);
    target.Role = mockRole;
    target.SystemRole = isSuperAdmin ? "SuperAdmin" : "User";
    target.HasSuperAdminOrganizationSelection = isSuperAdmin;
}

static void CopyCurrentOrganization(CurrentOrganization source, CurrentOrganization target)
{
    target.UserId = source.UserId;
    target.OrganizationId = source.OrganizationId;
    target.AccessibleOrganizationCount = source.AccessibleOrganizationCount;
    target.HasSuperAdminOrganizationSelection = source.HasSuperAdminOrganizationSelection;
    target.OrganizationName = source.OrganizationName;
    target.OrganizationSlug = source.OrganizationSlug;
    target.IsDemo = source.IsDemo;
    target.DemoExpiresUtc = source.DemoExpiresUtc;
    target.UserEmail = source.UserEmail;
    target.UserFullName = source.UserFullName;
    target.Role = source.Role;
    target.SystemRole = source.SystemRole;
    target.Province = source.Province;
    target.CountryCode = source.CountryCode;
    target.PreferredLanguage = source.PreferredLanguage;
    target.SubscriptionIsActive = source.SubscriptionIsActive;
    target.TrialExpired = source.TrialExpired;
}

# PropertySaaS

PropertySaaS is an Ontario-first property management SaaS for independent landlords and growing portfolios. It combines portfolio visibility, leasing operations, maintenance coordination, compliance tracking, and marketing-ready property playbooks in one Blazor application.

## Why this product exists

Most landlord tools stop at basic record keeping. PropertySaaS is positioned as an execution system:

- portfolio-first operating views
- property playbooks that improve leasing consistency
- Ontario Residential Tenancies Act workflow support
- lease, tenant, unit, maintenance, and compliance modules that stay connected
- a clean path from lightweight portfolio scanning to deep property detail pages

## Product highlights

- compact portfolio index for properties with dedicated property profile pages
- list-first management experience for units, leases, tenants, maintenance, and compliance
- drawer-based create/edit flows to reduce page clutter
- Ontario-specific reminders and notice workflow visibility
- tenant screening provider tracking
- maintenance and vendor coordination workflows
- import/export support for common portfolio operations
- audit logging for operational changes
- local development auth fallback plus Clerk-ready auth wiring
- Stripe-ready billing extension points

## Tech stack

- Blazor Web App on .NET 10
- Clean Architecture style with Domain, Application, Infrastructure, Web, and Tests
- Entity Framework Core with SQL Server
- MudBlazor for the main application UI
- Clerk-ready OpenID Connect configuration
- Stripe-ready subscription configuration
- Azure-ready deployment design

## Current UX direction

The application now follows a consistent management pattern across the core modules:

- search + filters at the top
- compact KPI strip
- paginated tables for fast scanning
- side-drawer forms for create/edit actions
- dedicated property profile pages for full operating context

This pattern is implemented across:

- Properties
- Units
- Leases
- Tenants
- Maintenance
- Compliance

## Ontario-specific scope

- Standard Ontario Lease tracked at lease level
- N1 rent increase workflow support
- N4 notice template availability
- compliance reminders for RTA workflows
- notice and service-date audit readiness
- tenant screening provider capture

## Solution structure

- `src/PropertySaaS.Domain` - entities, enums, and domain model
- `src/PropertySaaS.Application` - application services and tenant-aware operations
- `src/PropertySaaS.Infrastructure` - EF Core, database wiring, seed data, Stripe service
- `src/PropertySaaS.Web` - Blazor UI, routes, auth wiring, exports, and pages
- `tests/PropertySaaS.Tests` - starter automated coverage

## Local setup

1. Ensure SQL Server Express is available on `WIN-QVV1GR7G0KH\SQLEXPRESS`.
2. The application uses database `PropertyDB`.
3. Restore, build, and run:
   - `dotnet build`
   - `dotnet ef migrations add InitialCreate --project src/PropertySaaS.Infrastructure --startup-project src/PropertySaaS.Web`
   - `dotnet ef database update --project src/PropertySaaS.Infrastructure --startup-project src/PropertySaaS.Web`
   - `dotnet run --project src/PropertySaaS.Web`

## Azure target architecture

- Azure App Service for the web host
- Azure SQL Database for `PropertyDB`
- Azure Key Vault for Clerk and Stripe secrets
- Azure Front Door or Application Gateway for edge routing
- Azure Monitor and Application Insights for telemetry
- optional Azure Storage for document export/import archives

## Authentication and billing

Clerk and Stripe are wired as extension points. The local MVP remains runnable with development-friendly auth settings while preserving a path to production-grade identity and subscriptions.

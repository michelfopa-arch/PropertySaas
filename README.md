# PropertySaaS

PropertySaaS is a multi-tenant property management SaaS application for Ontario landlords built with Blazor Web App, Entity Framework Core, SQL Server, Clerk-ready authentication wiring and Stripe-ready subscription wiring.

## Stack
- Blazor Web App (.NET 10)
- Clean Architecture style with Domain, Application, Infrastructure, Web
- Entity Framework Core with SQL Server
- Clerk via OpenID Connect configuration
- Stripe subscription configuration
- Azure-ready deployment design

## Product scope implemented
- Marketing home page and pricing page
- Multi-tenant domain model
- Dashboard metrics
- CRUD-read modules for properties, units, tenants, leases and maintenance
- Ontario-specific compliance reminders and document templates
- Audit log entity
- Import/export strategy page
- Seed data and SQL Server database bootstrap
- Starter test coverage

## Ontario-specific decisions
- Standard Ontario Lease tracked at lease level
- N1 increase notice workflow flag
- N4 template seeded
- Compliance reminder center for RTA workflows
- Tenant screening provider capture
- Vendor marketplace concept in maintenance operations

## Local setup
1. Ensure SQL Server Express is available on `WIN-QVV1GR7G0KH\\SQLEXPRESS`.
2. The app uses database `PropertyDB`.
3. Run:
   - `dotnet build`
   - `dotnet ef migrations add InitialCreate --project src/PropertySaaS.Infrastructure --startup-project src/PropertySaaS.Web`
   - `dotnet ef database update --project src/PropertySaaS.Infrastructure --startup-project src/PropertySaaS.Web`
   - `dotnet run --project src/PropertySaaS.Web`

## Azure target architecture
- Azure App Service for web host
- Azure SQL Database for `PropertyDB`
- Azure Key Vault for Clerk and Stripe secrets
- Azure Front Door or Application Gateway for edge routing
- Azure Monitor and Application Insights for telemetry
- Optional Azure Storage for document exports/import archives

## Auth and billing
Clerk and Stripe are wired through configuration but not fully completed with production flows. This keeps the local MVP runnable while preserving the right extension points.

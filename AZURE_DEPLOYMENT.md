# Azure Deployment

## Target architecture
- Azure App Service (Linux)
- Azure SQL Database
- Azure Key Vault
- Application Insights

## Provision
```powershell
az group create -n rg-propsaas-dev -l canadacentral
az deployment group what-if -g rg-propsaas-dev --template-file infra/main.bicep --parameters environmentName=dev sqlAdminLogin=propsaasadmin sqlAdminPassword=<SecurePassword>
az deployment group create -g rg-propsaas-dev --template-file infra/main.bicep --parameters environmentName=dev sqlAdminLogin=propsaasadmin sqlAdminPassword=<SecurePassword>
```

## Publish app
```powershell
dotnet publish src/PropertySaaS.Web/PropertySaaS.Web.csproj -c Release
```
Deploy the published output to the created App Service.

## Secrets
Move Clerk and Stripe secrets to Key Vault and reference them from App Service settings.

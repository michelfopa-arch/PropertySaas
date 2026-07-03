# Onboarding nouvel ordinateur — Runtira

## Important
Ne pas stocker les mots de passe, clés API ou chaînes de connexion en clair dans le dépôt Git.

Ce document décrit :
- les prérequis
- la procédure d’installation
- l’inventaire des secrets à restaurer
- les commandes à exécuter sur un nouveau poste

Les vraies valeurs doivent rester dans un coffre-fort, dans les user-secrets locaux, ou dans les secrets de la plateforme de déploiement.

---

## 1. Pré-requis machine
Installer :
- Visual Studio 2026 avec la charge .NET
- .NET SDK 10
- Git
- PowerShell
- accès réseau à SQL Server / Azure SQL
- accès aux comptes : Clerk, Stripe, Resend, Azure Storage, Microsoft 365

Repository :
- `C:\Users\miche\source\repos\PropertySaaS`

---

## 2. Cloner le dépôt
```powershell
cd C:\Users\miche\source\repos
git clone https://github.com/michelfopa-arch/PropertySaas.git
cd PropertySaaS
```

---

## 3. Restaurer et builder
```powershell
dotnet restore
dotnet build
```

---

## 4. Projet web à configurer
Les secrets locaux doivent être configurés au minimum pour :
- `src\Runtira.Web\Runtira.Web.csproj`

Le projet utilise le même ensemble logique de providers :
- Clerk
- Stripe
- Resend
- Azure Blob
- SQL
- Microsoft 365

---

## 5. Inventaire des secrets à restaurer
## Clerk
- `Clerk:Authority`
- `Clerk:ClientId`
- `Clerk:ClientSecret`
- `Clerk:PublishableKey`
- `Clerk:SecretKey`
- `Clerk:SignInUrl`
- `Clerk:SignUpUrl`
- `Clerk:UnauthorizedSignInUrl`
- `Clerk:UserProfileUrl`

## Stripe
- `Stripe:PublishableKey`
- `Stripe:SecretKey`
- `Stripe:WebhookSecret`
- `Stripe:StarterPriceId`
- `Stripe:GrowthPriceId`
- `Stripe:ProPriceId`

## Resend
- `Resend:ApiKey`
- `Resend:FromEmail`
- `Resend:FromName`
- `Resend:SupportEmail`

## Azure Blob
- `AzureBlob:ConnectionString`

## SQL / base de données
- `ConnectionStrings:PropertyDbPassword`
- `ConnectionStrings:RuntiraDbPassword`

## Microsoft 365
- `Microsoft365:TenantId`
- `Microsoft365:ClientId`
- `Microsoft365:ClientSecret`
- `Microsoft365:SupportMailbox`

---

## 6. Vérifier les secrets existants sur une ancienne machine
Sur l’ancienne machine :

### Runtira.Web
```powershell
dotnet user-secrets list --project "C:\Users\miche\source\repos\PropertySaaS\src\Runtira.Web\Runtira.Web.csproj"
```

Copier les valeurs dans un coffre-fort sécurisé, pas dans Git.

---

## 7. Restaurer les secrets sur un nouveau poste
Exemples :

### Runtira.Web
```powershell
dotnet user-secrets set --project "C:\Users\miche\source\repos\PropertySaaS\src\Runtira.Web\Runtira.Web.csproj" "Clerk:Authority" "<valeur>"
dotnet user-secrets set --project "C:\Users\miche\source\repos\PropertySaaS\src\Runtira.Web\Runtira.Web.csproj" "Clerk:ClientId" "<valeur>"
dotnet user-secrets set --project "C:\Users\miche\source\repos\PropertySaaS\src\Runtira.Web\Runtira.Web.csproj" "Clerk:ClientSecret" "<valeur>"
dotnet user-secrets set --project "C:\Users\miche\source\repos\PropertySaaS\src\Runtira.Web\Runtira.Web.csproj" "Resend:ApiKey" "<valeur>"
dotnet user-secrets set --project "C:\Users\miche\source\repos\PropertySaaS\src\Runtira.Web\Runtira.Web.csproj" "AzureBlob:ConnectionString" "<valeur>"
dotnet user-secrets set --project "C:\Users\miche\source\repos\PropertySaaS\src\Runtira.Web\Runtira.Web.csproj" "ConnectionStrings:PropertyDbPassword" "<valeur>"
dotnet user-secrets set --project "C:\Users\miche\source\repos\PropertySaaS\src\Runtira.Web\Runtira.Web.csproj" "ConnectionStrings:RuntiraDbPassword" "<valeur>"
```

---

## 8. Vérifications après restauration
Vérifier que les secrets sont présents :

```powershell
dotnet user-secrets list --project "C:\Users\miche\source\repos\PropertySaaS\src\Runtira.Web\Runtira.Web.csproj"
```

Builder :
```powershell
dotnet build
```

Lancer Runtira :
```powershell
dotnet run --project "C:\Users\miche\source\repos\PropertySaaS\src\Runtira.Web\Runtira.Web.csproj"
```

---

## 9. Ports locaux
Ports locaux Runtira :
- HTTP : `5166`
- HTTPS : `7087`

Si le port est déjà utilisé, arrêter la session Visual Studio ou le process déjà en cours.

---

## 10. Vérifications fonctionnelles
### Clerk
- ouvrir `/sign-in`
- tester `/account/login`
- vérifier la redirection Clerk
- vérifier le retour après authentification

### Resend
- ouvrir `/account`
- utiliser le bouton de test Resend
- vérifier la réception de l’email

### Stripe
- ouvrir la page billing du tenant
- tester checkout
- tester portal

### Microsoft 365
- vérifier la configuration Graph
- vérifier l’accès à la boîte support
- vérifier l’ingestion et le classement

---

## 11. Procédure recommandée de transfert entre ordinateurs
### Option recommandée
- conserver les secrets dans un gestionnaire de mots de passe / coffre-fort
- recréer les user-secrets sur chaque machine via `dotnet user-secrets set`

### Option locale avancée
Exporter uniquement dans un stockage chiffré privé :
- lister les secrets de l’ancienne machine
- copier les valeurs dans un coffre-fort
- restaurer manuellement sur la nouvelle machine

Ne pas :
- committer des secrets dans `appsettings.json`
- pousser des mots de passe dans GitHub
- partager des captures d’écran avec les clés visibles

---

## 12. Checklist de nouvel ordinateur
- [ ] Visual Studio installé
- [ ] .NET 10 installé
- [ ] repo cloné
- [ ] restore OK
- [ ] build OK
- [ ] secrets Clerk restaurés
- [ ] secrets Stripe restaurés
- [ ] secrets Resend restaurés
- [ ] secret Azure Blob restauré
- [ ] secrets SQL restaurés
- [ ] secrets Microsoft 365 restaurés
- [ ] Runtira.Web démarre
- [ ] Clerk login fonctionne
- [ ] Resend fonctionne
- [ ] Stripe fonctionne
- [ ] Microsoft 365 fonctionne

---

## 13. Rappel sécurité
Le dépôt contient maintenant la procédure, mais pas les secrets.
Si un document séparé contenant les vraies valeurs est nécessaire, il doit être :
- hors dépôt Git
- chiffré
- stocké dans un coffre-fort ou un emplacement privé sécurisé

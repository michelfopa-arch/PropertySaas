# Runtira Roadmap

## Vision produit
Runtira doit être un workspace AI-first multi-tenant, vendable rapidement, capable de supporter un maximum de juridictions dès le départ, tout en restant extensible pour de nouvelles langues, de nouveaux pays et de nouveaux workflows métier.

Le produit doit permettre à une organisation de centraliser :
- les leads
- les emails
- les imports de données et documents
- la facturation
- les règles locales par juridiction
- l’archivage structuré

## Positionnement
Runtira se vend comme :

> Un workspace AI-first multi-tenant pour gérer leads, emails, imports documentaires et facturation, avec adaptation automatique par juridiction, langue et marché.

## Décision produit
Runtira n’est pas conçu comme un PMS traditionnel complet au départ.

Le produit doit se positionner comme un **AI operations layer** verticalisé pour les opérations locatives et administratives, avec trois promesses fortes :
- ingestion intelligente des entrées : emails, leads, imports, documents
- exécution guidée par règles locales : juridictions, langue, conformité, facturation
- production rapide des sorties : facture, réponse, archive, action recommandée

L’objectif commercial est d’être plus rapide à adopter et plus différenciant qu’un outil immobilier classique trop lourd ou trop comptable.

## Segment cible
Priorité commerciale initiale :
- property managers small et mid-market
- propriétaires multi-biens
- petites équipes operations / administration
- structures qui gèrent beaucoup d’emails, documents, factures et règles locales

Ne pas viser l’enterprise lourd au départ.

## Démonstration commerciale cible
La démo vendable doit tenir en moins de 5 minutes :
1. un email, un lead ou un document entre
2. l’IA classe et structure l’information
3. le système propose l’action suivante
4. une facture, une réponse ou une archive est produite
5. le tout respecte la juridiction active et la langue du marché

## KPI produit à suivre
- organisations actives
- leads créés ou importés
- emails classés
- imports traités
- factures générées
- temps moyen jusqu’à la première action utile
- taux d’usage des suggestions IA
- conversion trial vers abonnement

## Principes produit
- Multi-tenant par organisation avec URL `/{tenantSlug}`
- Expérience AI-first, simple, guidée, orientée questions/réponses
- Support multilingue dès le départ
- Support multi-juridiction piloté par configuration
- Providers alignés sur la configuration Runtira active et réutilisables selon l’environnement
- Archivage JSON local compatible Azure Blob
- Extensibilité future par configuration plutôt que par code dur
- Déploiement Azure visé dès le MVP avec configuration cloud-native simple

## MVP vendable recommandé
Le MVP doit inclure :
- authentification Clerk
- routage multi-tenant
- rôles owner / member / super admin
- shell AI-first
- dashboard minimal orienté actions et questions
- templates de facture multiples
- génération mensuelle de factures
- génération de PDF envoyable
- Stripe pour abonnement et billing
- Resend pour emails transactionnels
- connexion Microsoft 365
- lecture des emails
- classement IA des emails
- Microsoft 365 mocké au départ tant que le compte n’est pas disponible
- import IA Excel / CSV / texte / PDF / documents
- validation utilisateur avant enregistrement
- archive JSON par tenant, prête pour Azure Blob
- gestion des leads
- moteur juridictionnel JSON
- support initial FR / EN / ES
- support initial Canada / États-Unis, extensible

## Fonctionnalités coeur

### 1. Accès et multi-tenant
- authentification Clerk
- profil utilisateur
- gestion des organisations
- URL par tenant
- sélection de tenant
- rôles et permissions
- mode super admin transversal

### 2. Facturation
- création de facture par IA ou question guidée
- templates multiples
- factures mensuelles
- règles locales selon juridiction
- PDF envoyable
- historique des factures
- emails transactionnels liés aux factures

### 3. Emails et communications
- connexion Microsoft 365
- lecture de la boîte support
- ingestion des emails dans le tenant
- classement IA des emails
- suggestions de réponse
- rattachement des pièces jointes
- envoi d’emails via Resend

### 4. Imports IA
- import Excel
- import CSV
- import texte libre
- import PDF / documents
- extraction structurée par IA
- proposition de mapping métier
- validation utilisateur
- archivage des sources importées

### 5. Leads
- création manuelle de lead
- création/import depuis email
- import de leads depuis fichiers
- qualification IA
- pipeline simple
- statuts : nouveau, qualifié, en attente, converti, fermé
- conversion vers client / dossier / entité métier future

### 6. Juridictions et conformité
- pays
- province / état / région
- langue par défaut
- langues supportées
- taxes applicables
- champs obligatoires
- règles invoice
- règles validation
- mentions légales
- options visibles seulement si conformes au marché

## Fonctionnalités différenciantes
- page d’accueil orientée questions/réponses
- prompts rapides par contexte
- résumé automatique du tenant
- actions recommandées par IA
- moteur de règles JSON par juridiction
- contenus marketing localisés par marché
- archives JSON traçables par tenant
- emails et templates localisés

## Architecture produit recommandée
- coeur métier neutre
- UI pilotée par règles
- juridictions en JSON
- localisation centralisée
- abstraction providers pour :
  - Clerk
  - Stripe
  - Resend
  - Microsoft Graph / Microsoft 365
  - OpenAI / IA simulée puis réelle
- abstraction d’archive locale prête pour Azure Blob
- modèle de données extensible pour nouveaux pays/langues

## Modèle métier recommandé
### Contexte SaaS
- `RuntiraOrganization`
- `RuntiraUser`
- `RuntiraMembership`

### Opérations immobilières
- `PropertyAsset`
- `Unit`
- `Lease`
- `Resident`
- `Lead`
- `InvoiceDraft`
- `InvoiceRecord`

### Communication
- `InboxMessage`
- `ConversationThread`
- `AttachmentRecord`

### Conformité et configuration
- `JurisdictionProfile`
- `JurisdictionRuleSet`
- `LocalizedTemplate`

Décision de vocabulaire :
- `Organization` pour le tenant SaaS
- `Resident` ou `Occupant` pour le locataire immobilier
- éviter d’utiliser `Tenant` pour les deux contextes

## Cible Azure MVP
Pour avoir quelque chose de fonctionnel sur Azure en 3 semaines, viser une architecture simple :
- hébergement de `Runtira.Web` sur Azure App Service
- base SQL sur Azure SQL
- archivage JSON sur Azure Blob Storage
- secrets et configuration sensibles dans Azure Key Vault à terme
- identité applicative Azure via Managed Identity quand l’hébergement Azure sera activé
- logs applicatifs exploitables et diagnostics minimum dès le MVP

Microsoft 365 peut rester mocké au départ :
- service d’inbox mocké
- emails de démonstration seedés ou simulés
- pipeline de classement IA branché sur ces mocks
- remplacement ultérieur par Microsoft Graph sans casser l’UI ni le domaine

## Roadmap 3 semaines

### Semaine 1 — Stabilisation et socle
Objectif : rendre la plateforme fiable et cohérente.

À faire :
- finaliser Clerk
  - login
  - logout
  - sign-up
  - profil
  - callback OIDC
  - redirections
- valider Resend
  - email test
	- expéditeur validé pour l’environnement actif
  - messages d’erreur UI
- valider Stripe
  - checkout
  - portail
  - webhook
- stabiliser la configuration
  - secrets/config centralisés pour Runtira
  - même port local
  - retrait des secrets versionnés
- préparer Azure MVP
  - choix App Service + Azure SQL + Blob
  - vérifier que la configuration est compatible cloud
  - préparer les variables d’environnement nécessaires
- stabiliser la base Runtira
  - migrations
  - seed
  - vérification multi-tenant
- valider les parcours critiques
  - accès tenant
  - compte
  - billing
  - workspace

Livrables :
- socle providers fonctionnel
- environnement local propre
- build propre
- checklist technique MVP
- configuration prête pour premier déploiement Azure

### Semaine 2 — MVP produit vendable
Objectif : livrer le coeur utilisable.

À faire :
- finaliser le shell AI-first
- finaliser le dashboard minimal
- finaliser le composeur de facture
- finaliser les templates de facture
- finaliser la génération mensuelle
- finaliser le PDF envoyable
- ajouter les premiers flux IA simulés
- finaliser la gestion des leads
- ajouter l’import IA
  - Excel
  - CSV
  - texte
  - PDF
- ajouter le service inbox mocké
- fournir des emails mockés dans le workspace
- classifier les emails mockés par IA
- rattacher emails/imports/leads au tenant
- continuer la localisation FR / EN / ES
- préparer le stockage Azure Blob pour les archives JSON
- tester le comportement applicatif avec configuration cloud

Livrables :
- MVP navigable de bout en bout
- leads utilisables
- import IA utilisable
- emails mockés ingérés et classés
- facturation principale utilisable
- archive Blob-ready validée

### Semaine 3 — Qualité, conformité et préparation vente
Objectif : rendre le produit présentable, extensible et prêt démo.

À faire :
- durcir les permissions
- finaliser le mode super admin
- finaliser la visibilité des règles par juridiction
- améliorer les messages marketing par langue/région
- ajouter observabilité minimale
- ajouter logs et erreurs claires
- tester les scénarios complets
  - onboarding
  - lead
  - import
  - email
  - facture
  - billing
- nettoyer UX/UI
- préparer le déploiement Azure
  - config App Service
  - config Azure SQL
  - config Blob
  - secrets d’environnement
  - smoke test post-déploiement
- préparer la documentation d’exploitation
- documenter le modèle d’ajout d’un nouveau pays / langue / juridiction
- documenter le remplacement futur des mocks Microsoft 365 par Microsoft Graph

Livrables :
- MVP prêt démo
- produit vendable V1
- base extensible pour nouveaux marchés
- backlog V2 clair
- application fonctionnelle sur Azure avec inbox mockée

## Priorités absolues MVP
1. Auth Clerk
2. Multi-tenant par slug
3. Stripe
4. Resend
5. Inbox mockée + classement IA
6. Azure App Service + Azure SQL + Blob-ready
7. Imports IA
8. Leads
9. Facturation + PDF
10. Juridictions JSON
11. Localisation FR/EN/ES
12. Archive JSON / Azure Blob-ready

## Extensions V2 recommandées
- score IA des leads
- suggestions automatiques de réponse
- recherche sémantique globale
- workflows d’automatisation
- analytics par tenant
- assistant conformité
- nouveaux pays
- nouvelles langues
- portail client externe

## Règle d’évolution produit
Toute nouvelle fonctionnalité doit être pensée pour :
- plusieurs langues
- plusieurs pays
- plusieurs juridictions
- plusieurs tenants
- plusieurs providers potentiels
- archivage traçable

## Notes importantes
- Clerk et Resend doivent réutiliser les mêmes configurations que le legacy quand elles existent
- Runtira doit utiliser le même port local que le legacy
- la région de l’utilisateur doit influencer l’expérience et la conformité visible
- l’interface doit rester simple, épurée et très AI-first

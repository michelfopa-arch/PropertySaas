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

## Principes produit
- Multi-tenant par organisation avec URL `/{tenantSlug}`
- Expérience AI-first, simple, guidée, orientée questions/réponses
- Support multilingue dès le départ
- Support multi-juridiction piloté par configuration
- Providers alignés sur la configuration Runtira active et réutilisables selon l’environnement
- Archivage JSON local compatible Azure Blob
- Extensibilité future par configuration plutôt que par code dur

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
- ajouter la connexion Microsoft 365
- lire les emails du support
- classifier les emails par IA
- rattacher emails/imports/leads au tenant
- continuer la localisation FR / EN / ES

Livrables :
- MVP navigable de bout en bout
- leads utilisables
- import IA utilisable
- emails Microsoft 365 ingérés
- facturation principale utilisable

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
- préparer la documentation d’exploitation
- documenter le modèle d’ajout d’un nouveau pays / langue / juridiction

Livrables :
- MVP prêt démo
- produit vendable V1
- base extensible pour nouveaux marchés
- backlog V2 clair

## Priorités absolues MVP
1. Auth Clerk
2. Multi-tenant par slug
3. Stripe
4. Resend
5. Microsoft 365
6. Inbox + classement IA
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

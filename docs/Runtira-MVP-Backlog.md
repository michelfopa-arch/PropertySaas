# Runtira MVP Backlog

## Objectif
Transformer la vision Runtira en backlog exécutable pour obtenir un produit vendable, compétitif et fonctionnel sur Azure en 3 semaines.

---

## Priorité 0 — Fondations critiques
### 0.1 Auth et accès
- finaliser Clerk login/logout/sign-up
- finaliser callback OIDC
- finaliser profil utilisateur
- vérifier redirections et retour session
- sécuriser les rôles owner/member/super admin

### 0.2 Multi-tenant
- fiabiliser `CurrentOrganization`
- fiabiliser le routage `/{tenantSlug}`
- fiabiliser l’accès direct à une organisation
- gérer la sélection d’organisation pour super admin

### 0.3 Providers
- valider Stripe checkout
- valider Stripe portal
- valider webhook Stripe
- valider Resend test email
- homogénéiser config providers

### 0.4 Azure-ready
- préparer variables d’environnement App Service
- préparer configuration Azure SQL
- préparer configuration Blob
- vérifier que les secrets ne sont pas versionnés
- préparer la stratégie Managed Identity / Key Vault pour plus tard

---

## Priorité 1 — Produit vendable visible
### 1.1 Shell AI-first
- page d’accueil claire
- champ libre principal
- questions fréquentes
- cartes d’action rapide
- vue synthèse tenant / marché / langue

### 1.2 Facturation
- finaliser les templates de facture
- finaliser le composeur de facture
- finaliser les champs dynamiques par juridiction
- finaliser la génération mensuelle
- finaliser PDF envoyable
- finaliser archive JSON liée aux factures

### 1.3 Leads
- créer entité métier `Lead`
- créer liste de leads
- créer formulaire de lead manuel
- créer statuts de lead
- créer qualification IA simulée
- prévoir conversion future du lead

### 1.4 Inbox mockée
- créer entité `InboxMessage`
- créer entité `ConversationThread`
- fournir des emails mockés
- afficher inbox dans le workspace
- classifier les emails mockés par IA
- rattacher email à lead / facture / document

### 1.5 Imports IA
- créer point d’entrée import
- supporter Excel / CSV / texte / PDF
- créer pipeline d’extraction simulée
- créer vue de validation avant sauvegarde
- archiver le document source et le résultat structuré

---

## Priorité 2 — Domaine métier minimal
### 2.1 Entités métier MVP
- `PropertyAsset`
- `Unit`
- `Lease`
- `Resident`
- `Lead`
- `InvoiceDraft`
- `InvoiceRecord`
- `InboxMessage`
- `AttachmentRecord`
- `JurisdictionProfile`

### 2.2 Décisions domaine
- utiliser `Organization` pour le client SaaS
- utiliser `Resident` pour le locataire immobilier
- éviter l’ambiguïté autour du mot `Tenant`
- garder les agrégats légers pour le MVP

### 2.3 Persistance
- tables minimales pour domaine MVP
- seed de démo
- compatibilité multi-tenant
- compatibilité archive JSON/Blob

---

## Priorité 3 — Juridictions, langues et conformité
### 3.1 Moteur juridictionnel
- fiabiliser `JurisdictionProfile`
- créer `JurisdictionRuleSet`
- normaliser les règles JSON
- supporter Canada + US au départ
- prévoir fallback pays/global

### 3.2 Localisation
- finaliser FR / EN / ES
- retirer les derniers textes hardcodés
- localiser messages UI
- localiser templates email
- localiser wording marketing

### 3.3 Conformité visible
- afficher seulement les options conformes au marché
- masquer les options non pertinentes
- mettre en avant le marché actif
- expliquer l’extensibilité sans surcharger l’UI

---

## Priorité 4 — Azure MVP
### 4.1 Hébergement
- déployer `Runtira.Web` sur Azure App Service
- configurer Azure SQL
- configurer Azure Blob
- vérifier la connectivité applicative

### 4.2 Validation cloud
- smoke test de l’app déployée
- test login
- test billing
- test Resend
- test archive Blob-ready

### 4.3 Observabilité minimale
- logs utiles
- erreurs claires
- traces simples de parcours critiques
- préparation Application Insights plus tard si nécessaire

---

## Priorité 5 — Démo commerciale
### 5.1 Scénarios de démo
- scénario lead entrant
- scénario email mocké entrant
- scénario import document
- scénario génération facture
- scénario adaptation juridiction/langue

### 5.2 Présentation produit
- wording simple
- design propre
- parcours sans friction
- CTA visibles
- compte de démo cohérent

---

## Hors MVP immédiat
- Microsoft 365 réel via Graph
- maintenance complexe
- portail client complet
- analytics avancés
- comptabilité complète
- workflows enterprise

---

## Critères de sortie MVP
Le MVP est prêt à être montré si :
- login fonctionne
- organisation/tenant fonctionne
- shell AI-first fonctionne
- leads fonctionnent
- imports IA fonctionnent
- inbox mockée fonctionne
- facturation fonctionne
- PDF fonctionne
- archive fonctionne
- juridictions visibles fonctionnent
- app déployée sur Azure fonctionne

---

## Ordre recommandé d’implémentation
1. Auth + multi-tenant
2. Shell AI-first
3. Facturation + PDF
4. Leads
5. Imports IA
6. Inbox mockée
7. Domaine métier MVP
8. Juridictions JSON
9. Localisation finale
10. Déploiement Azure
11. Démo commerciale

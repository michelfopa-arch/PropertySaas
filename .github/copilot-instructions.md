# Copilot Instructions

## Directives de projet
- Ce repo doit maintenant être transformé directement en Runtira; ne plus traiter ce projet comme un simple PropertySaaS séparé.
- Privilégier une refonte complète AI-first: UI très épurée type ChatGPT, support FR/EN/ES dès le départ, routage par organisation via sous-domaine en production avec fallback local, une session toujours rattachée à une seule organisation, législation modélisée via JSON/extensions/validations, et archivage fichiers via Azure Blob en conservant seulement les connexions provider/settings essentielles.
- Conserver seulement les intégrations/fondations utiles: Clerk, Azure SQL, Azure Blob, Microsoft 365/Graph pour les emails entrants, et les paramètres provider nécessaires.
- Utiliser Microsoft Agent Framework comme base d'intégration IA si c'est l'option la plus simple et propre côté .NET/Azure.
- Toutes les tables métier doivent porter un TenantId avec filtre global EF pour isoler les données; minimiser les données stockées en SQL par tenant et rendre les quotas/maximums raisonnables configurables.
- Les activités, actions détaillées, archives et historiques volumineux peuvent être stockés en JSON dans Blob, avec seulement les métadonnées et états utiles en base SQL.
- L'application doit permettre des commandes en langage naturel métier, poser automatiquement les questions obligatoires selon la juridiction applicable, puis exécuter ou proposer l'action.

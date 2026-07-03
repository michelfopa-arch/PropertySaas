# Session Handoff

## État actuel
- Le repo a été poussé sur `main` avec le commit `88a3b04`.
- La solution cible `.NET 10` et l’UI principale est en Blazor.
- Le produit est repositionné autour de la facturation, des baux, des résidents, des documents et de la conformité locale; les leads restent un module de support.
- L’application ouvre automatiquement l’organisation si l’utilisateur n’en a qu’une seule.
- Les providers principaux (Clerk, Stripe, Resend) et la détection langue/région sont déjà intégrés côté structure produit.

## Ce qui a été implémenté récemment
- standardisation des composants partagés : `PageLoadingState`, `PageEmptyState`, `SectionToolbar`, `BusyActionButton`
- ajout d’un `BusyOverlay`
- ajout d’un `StandardActionMenu`
- actions réelles sur les leads : archivage / suppression
- actions réelles sur les unités : maintenance / disponible / suppression protégée
- actions réelles sur les résidents et baux : mise en suivi / revue / réactivation
- création d’un hub exports dédié
- export CSV des leads
- amélioration forte du shell et du layout global
- seed Alberta étendu pour tester une propriété avec 50 unités
- ajout d’un pattern d’exploration master-detail pour les unités, baux et résidents dans `Assets.razor`
- début de factorisation du pattern d’exploration via :
  - `src/Runtira.Web/Components/Shared/ExplorerLayout.razor`
  - `src/Runtira.Web/Components/Shared/ExplorerSearchBox.razor`
  - `src/Runtira.Web/Components/Shared/ExplorerList.razor`

## Décisions UX / architecture à conserver
- préférer des pages lisibles et des parcours dédiés plutôt que des popups métier
- privilégier un pattern explorer pour les gros volumes (unités, baux, résidents, inbox, documents)
- pour ~50 unités, la grille simple n’est pas suffisante; il faut recherche, sélection et détail
- au-delà de 40 à 50 éléments, l’autocomplete devient recommandé en plus de l’explorer
- conserver une approche hybride : coeur métier relationnel + extensions/contextes pilotés par JSON
- les champs de formulaires doivent rester dépendants du contexte marché/législation et pilotés par les règles JSON
- l’expérience d’export doit rester centralisée dans une zone dédiée

## Points techniques importants
- `src/Runtira.Web/Components/App.razor.cs` a été ajouté pour stabiliser la résolution du composant racine `App` depuis `Program.cs`.
- `Assets.razor` concentre actuellement beaucoup de logique d’exploration; il faudra le nettoyer et poursuivre la factorisation en composants réutilisables.
- Les builds peuvent être perturbés si l’app tourne en debug dans Visual Studio à cause des DLL verrouillées.
- Le pattern d’exploration est déjà visible et doit être progressivement mutualisé sans casser le build.

## Fichiers clés à relire en priorité sur une autre machine
- `src/Runtira.Web/Components/Pages/Assets.razor`
- `src/Runtira.Web/Components/Pages/Leads.razor`
- `src/Runtira.Web/Components/Shared/BusyOverlay.razor`
- `src/Runtira.Web/Components/Shared/StandardActionMenu.razor`
- `src/Runtira.Web/Components/Shared/ExplorerLayout.razor`
- `src/Runtira.Web/Components/Shared/ExplorerSearchBox.razor`
- `src/Runtira.Web/Components/Shared/ExplorerList.razor`
- `src/Runtira.Web/Localization/RuntiraText.cs`
- `src/Runtira.Web/wwwroot/app.css`
- `src/Runtira.Application/RuntiraApplication.cs`
- `src/Runtira.Infrastructure/RuntiraInfrastructure.cs`
- `docs/Runtira-MVP-Backlog.md`

## Prochaines étapes recommandées
1. nettoyer `Assets.razor` pour réduire la densité du fichier
2. poursuivre la factorisation du pattern explorer en composants partagés plus génériques
3. réappliquer le même pattern à l’inbox et aux documents
4. ajouter tri, filtres et éventuellement virtualisation pour les listes volumineuses
5. finaliser PDF envoyable et les parcours d’export avancés

## Reprise sur une autre machine
1. cloner le repo
2. ouvrir la branche `main`
3. lire ce fichier `docs/session-handoff.md`
4. lire ensuite `docs/Runtira-MVP-Backlog.md`
5. relancer l’application hors conflit de process de debug avant de modifier `Assets.razor`

## Commandes utiles
```powershell
 git pull origin main
 dotnet build PropertySaaS.slnx
```

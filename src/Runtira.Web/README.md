# Runtira.Web transition

Ce projet est le nouveau point d'entrée de la refonte Runtira.

Objectif immédiat:
- déplacer progressivement le shell AI-first, le routage tenant, l'onboarding et le workspace
- conserver uniquement les fondations utiles du repo actuel
- supprimer ensuite le legacy PropertySaaS une fois le parcours Runtira stabilisé

Ordre recommandé:
1. migrer Program.cs et la config minimale
2. migrer le shell Blazor (App, Routes, Layout, pages Runtira)
3. brancher Application/Infrastructure Runtira
4. retirer les derniers liens legacy

# Upgrade Options — OD.MDReader

Assessment: 2 projets (MDRead — WPF, MDRead.Tests — xUnit), tous SDK-style sur `net8.0-windows`, cible `net10.0-windows`; aucun package incompatible, 1 package déconseillé (xunit 2.9.2), changements d'API signalés (396 binaires, 1 source, 32 comportementaux).

## Strategy

### Upgrade Strategy
Solution moderne (net8.0+) de 2 projets seulement, graphe de dépendances plat (1 niveau) et migration purement mécanique du TFM.

| Value | Description |
|-------|-------------|
| **All-at-Once** (selected) | Met à niveau les deux projets en une seule passe atomique — le plus rapide, pas de multi-ciblage, la solution est validée une fois l'ensemble migré. |
| Top-Down | Met à niveau l'application d'abord, en multi-ciblant temporairement les bibliothèques partagées pour garder la solution compilable en continu. |

## Compatibility

### Unsupported API Handling
L'évaluation signale des changements d'API entre .NET 8 et .NET 10 (Api.0001 binaire, Api.0002 source, Api.0003 comportemental), majoritairement liés à WPF.

| Value | Description |
|-------|-------------|
| **Fix Inline** (selected) | Résout tous les changements d'API dans la tâche concernée, y compris les cas complexes — aucun travail différé, aucun stub à nettoyer. |
| Defer Complex Changes | Applique les remplacements simples immédiatement et génère un stub compilable + une sous-tâche de résolution pour les cas complexes. |

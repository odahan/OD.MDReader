# .NET Version Upgrade Progress

## Overview

Mise à niveau de la solution OD.MDReader (application WPF MDRead et sa suite de tests) de .NET 8 vers .NET 10 (LTS). L'approche All-At-Once migre les deux projets simultanément, puis corrige les incompatibilités d'API et met à jour les packages NuGet.

**Progress**: 1/3 tasks complete <progress value="33" max="100"></progress> 33%

## Tasks

- ✅ 01-prerequisites: Vérifier la chaîne d'outils et la base de référence ([Content](tasks/01-prerequisites/task.md), [Progress](tasks/01-prerequisites/progress-details.md))
- 🔲 02-solution-upgrade: Migrer les deux projets vers .NET 10 et corriger les incompatibilités
- 🔲 03-validation: Valider la solution migrée

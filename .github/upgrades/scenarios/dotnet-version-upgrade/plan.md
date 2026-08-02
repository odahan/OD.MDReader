# .NET Version Upgrade Plan

## Overview

**Target**: Mise à niveau de la solution OD.MDReader de `net8.0-windows` vers `net10.0-windows`
**Scope**: 2 projets (MDRead — application WPF ; MDRead.Tests — suite xUnit), tous SDK-style, graphe de dépendances plat

### Selected Strategy
**All-At-Once** — Tous les projets sont mis à niveau simultanément en une seule opération.
**Rationale**: 2 projets, tous déjà sur .NET 8 moderne et SDK-style, dépendance unique MDRead.Tests → MDRead. Une approche incrémentale n'apporterait aucun bénéfice de validation intermédiaire.

## Tasks

### 01-prerequisites: Vérifier la chaîne d'outils et la base de référence

Confirmer que le SDK .NET 10 est installé et utilisable, et vérifier qu'aucun fichier `global.json` n'épingle une version de SDK incompatible avec la cible. Établir également une base de référence en compilant la solution et en exécutant la suite de tests sur `net8.0-windows`, afin de disposer d'un point de comparaison fiable une fois la migration effectuée.

Cette tâche ne modifie pas le code applicatif. Si un `global.json` existe et contraint le SDK, il doit être mis à jour pour autoriser .NET 10.

**Done when**: Le SDK .NET 10 est confirmé disponible, aucun `global.json` ne bloque la cible, la solution compile sans erreur et la suite de tests passe sur le TFM actuel (résultats enregistrés comme référence).

### 02-solution-upgrade: Migrer les deux projets vers .NET 10 et corriger les incompatibilités

Passer le `TargetFramework` de `MDRead` et `MDRead.Tests` de `net8.0-windows` à `net10.0-windows` en une passe atomique, puis mettre à jour les références de packages NuGet vers les versions supportées par la nouvelle cible. Le projet WPF conserve ses propriétés de publication (`RuntimeIdentifier` win-x64, `SelfContained`, `PublishSingleFile`, compression) — il faut vérifier qu'elles restent valides et cohérentes sous .NET 10.

L'évaluation signale des changements d'API entre .NET 8 et .NET 10 (396 incompatibilités binaires, 1 incompatibilité source, 32 changements de comportement), très majoritairement concentrés sur WPF. Les incompatibilités binaires se résolvent par recompilation ; l'incompatibilité source et les changements comportementaux doivent être examinés et corrigés directement dans le code, conformément à l'option **Fix Inline** retenue — aucun stub ni report.

Le package `xunit` 2.9.2 est signalé comme déconseillé : évaluer le passage à la version supportée (ou au successeur recommandé), en alignant `xunit.runner.visualstudio` et `Microsoft.NET.Test.Sdk`. Les packages `CommunityToolkit.Mvvm`, `Markdig` et `Microsoft.Web.WebView2` sont compatibles mais doivent être portés vers leurs versions stables les plus récentes compatibles avec la cible.

**Done when**: Les deux projets ciblent `net10.0-windows`, tous les packages sont sur des versions compatibles et non déconseillées, le code compile sans erreur ni avertissement, et aucun changement d'API signalé ne reste non traité.

### 03-validation: Valider la solution migrée

Effectuer une compilation complète de la solution en configuration Release, exécuter l'intégralité de la suite de tests et comparer les résultats à la base de référence établie en tâche 01. Vérifier également que la publication de l'application WPF (single-file, self-contained, win-x64) produit un artefact fonctionnel sous .NET 10.

Documenter toute recommandation différée — notamment l'introduction éventuelle de la gestion centralisée des packages (`Directory.Packages.props`), pertinente maintenant que les deux projets sont sur un TFM unique.

**Done when**: La solution compile en Release sans erreur ni avertissement, tous les tests passent (parité ou mieux avec la base de référence), la publication produit un exécutable valide, et les recommandations différées sont consignées.

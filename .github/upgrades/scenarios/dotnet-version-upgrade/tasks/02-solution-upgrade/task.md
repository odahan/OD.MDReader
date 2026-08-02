# 02-solution-upgrade: Migrer les deux projets vers .NET 10 et corriger les incompatibilités

Passer le `TargetFramework` de `MDRead` et `MDRead.Tests` de `net8.0-windows` à `net10.0-windows` en une passe atomique, puis mettre à jour les références de packages NuGet vers les versions supportées par la nouvelle cible. Le projet WPF conserve ses propriétés de publication (`RuntimeIdentifier` win-x64, `SelfContained`, `PublishSingleFile`, compression) — il faut vérifier qu'elles restent valides et cohérentes sous .NET 10.

L'évaluation signale des changements d'API entre .NET 8 et .NET 10 (396 incompatibilités binaires, 1 incompatibilité source, 32 changements de comportement), très majoritairement concentrés sur WPF. Les incompatibilités binaires se résolvent par recompilation ; l'incompatibilité source et les changements comportementaux doivent être examinés et corrigés directement dans le code, conformément à l'option **Fix Inline** retenue — aucun stub ni report.

Le package `xunit` 2.9.2 est signalé comme déconseillé : évaluer le passage à la version supportée (ou au successeur recommandé), en alignant `xunit.runner.visualstudio` et `Microsoft.NET.Test.Sdk`. Les packages `CommunityToolkit.Mvvm`, `Markdig` et `Microsoft.Web.WebView2` sont compatibles mais doivent être portés vers leurs versions stables les plus récentes compatibles avec la cible.

**Done when**: Les deux projets ciblent `net10.0-windows`, tous les packages sont sur des versions compatibles et non déconseillées, le code compile sans erreur ni avertissement, et aucun changement d'API signalé ne reste non traité.

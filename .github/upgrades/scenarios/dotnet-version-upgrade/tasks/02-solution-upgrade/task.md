# 02-solution-upgrade: Migrer les deux projets vers .NET 10 et corriger les incompatibilités

Passer le `TargetFramework` de `MDRead` et `MDRead.Tests` de `net8.0-windows` à `net10.0-windows` en une passe atomique, puis mettre à jour les références de packages NuGet vers les versions supportées par la nouvelle cible. Le projet WPF conserve ses propriétés de publication (`RuntimeIdentifier` win-x64, `SelfContained`, `PublishSingleFile`, compression) — il faut vérifier qu'elles restent valides et cohérentes sous .NET 10.

L'évaluation signale des changements d'API entre .NET 8 et .NET 10 (396 incompatibilités binaires, 1 incompatibilité source, 32 changements de comportement), très majoritairement concentrés sur WPF. Les incompatibilités binaires se résolvent par recompilation ; l'incompatibilité source et les changements comportementaux doivent être examinés et corrigés directement dans le code, conformément à l'option **Fix Inline** retenue — aucun stub ni report.

Le package `xunit` 2.9.2 est signalé comme déconseillé : évaluer le passage à la version supportée (ou au successeur recommandé), en alignant `xunit.runner.visualstudio` et `Microsoft.NET.Test.Sdk`. Les packages `CommunityToolkit.Mvvm`, `Markdig` et `Microsoft.Web.WebView2` sont compatibles mais doivent être portés vers leurs versions stables les plus récentes compatibles avec la cible.

**Done when**: Les deux projets ciblent `net10.0-windows`, tous les packages sont sur des versions compatibles et non déconseillées, le code compile sans erreur ni avertissement, et aucun changement d'API signalé ne reste non traité.

## Research Findings

### Projects Affected
- `MDRead\MDRead.csproj` — WPF, TFM `net8.0-windows` → `net10.0-windows`
- `MDRead.Tests\MDRead.Tests.csproj` — xUnit, TFM `net8.0-windows` → `net10.0-windows`

### Packages to Update
| Package | Current | Target | Notes |
|---------|---------|--------|-------|
| CommunityToolkit.Mvvm | 8.4.2 | 8.4.2 | Déjà la version supportée |
| Markdig | 1.3.2 | 1.3.2 | Déjà la version supportée |
| Microsoft.Web.WebView2 | 1.0.4078.44 | 1.0.4078.44 | Compatible ; l'outil ne renvoie pas de version cible pour ce TFM Windows |
| coverlet.collector | 6.0.4 | 10.0.1 | Alignement sur l'écosystème .NET 10 |
| Microsoft.NET.Test.Sdk | 17.12.0 | 18.8.1 | Requis pour l'exécution des tests sur .NET 10 |
| xunit | 2.9.2 | 2.9.3 | 2.9.2 déconseillé ; 2.9.3 est la version supportée de la ligne v2 |
| xunit.runner.visualstudio | 2.8.2 | 2.8.2 | Seule alternative proposée = 4.0.0-pre.5 (préversion) → conservée en stable |

### API Changes / Migration Patterns
- **Api.0001 (396, binaire)** — quasi exclusivement WPF (`System.Windows.Media.Color`, `SolidColorBrush.Color`, `Window.Close`, …) : résolu par recompilation sur la nouvelle cible, aucune modification de code requise.
- **Api.0002 (1, source)** — `System.TimeSpan.FromSeconds(double)` dans `MDRead.Tests\MainWindowLoadingTests.cs:30` : la surcharge reste valide en .NET 10, compilation confirmée sans erreur ni avertissement.
- **Api.0003 (32, comportement)** — `System.Uri` / `Uri.AbsoluteUri` (tests de sécurité `MainWindowViewModelSecurityTests.cs`) : comportement vérifié par la suite de tests, aucun écart constaté.

### Dependencies & Risks
- Graphe plat : `MDRead.Tests` → `MDRead`. Aucune dépendance circulaire, aucune directive `#if`, aucun target MSBuild personnalisé.
- Propriétés de publication WPF (`RuntimeIdentifier=win-x64`, `SelfContained`, `PublishSingleFile`, `EnableCompressionInSingleFile`) inchangées et toujours valides sous .NET 10.

### Decisions Made
- Rester sur la ligne **xUnit v2 (2.9.3)** plutôt que migrer vers xUnit v3 : la migration v3 est un changement structurel hors périmètre de la mise à niveau du framework, et 2.9.3 lève l'avertissement de dépréciation.
- Conserver `xunit.runner.visualstudio` 2.8.2 : la seule version plus récente proposée est une préversion.

## Files Modified
- MDRead\MDRead.csproj
- MDRead.Tests\MDRead.Tests.csproj

## Build Result
- Errors: 0
- Warnings: 0
- Projects built: MDRead, MDRead.Tests (net10.0-windows, Debug)

## Test Result
- Tests run: 81
- Passed: 81
- Failed: 0

## Changes Summary
- `TargetFramework` de MDRead et MDRead.Tests passé de `net8.0-windows` à `net10.0-windows`.
- Packages de test mis à jour : `Microsoft.NET.Test.Sdk` 17.12.0 → 18.8.1, `xunit` 2.9.2 → 2.9.3 (lève la dépréciation), `coverlet.collector` 6.0.4 → 10.0.1.
- `CommunityToolkit.Mvvm`, `Markdig`, `Microsoft.Web.WebView2` et `xunit.runner.visualstudio` conservés : déjà sur la version supportée pour la cible (la seule alternative pour le runner est une préversion).
- Aucune modification de code applicatif nécessaire : les 396 incompatibilités binaires signalées (WPF) sont résolues par recompilation ; l'incompatibilité source `TimeSpan.FromSeconds(double)` et les changements de comportement `System.Uri` n'entraînent ni erreur de compilation ni régression de test.
- Propriétés de publication WPF (win-x64, self-contained, single-file, compression) conservées et valides sous .NET 10.

## Issues Encountered
- `get_supported_package_version` ne renvoie pas de version pour `Microsoft.Web.WebView2` sur `net10.0-windows` ; l'évaluation classe le package comme compatible et la compilation/les tests le confirment — version conservée.
- `xunit.runner.visualstudio` : seule version supérieure disponible = 4.0.0-pre.5 (préversion), non retenue pour rester sur du stable.

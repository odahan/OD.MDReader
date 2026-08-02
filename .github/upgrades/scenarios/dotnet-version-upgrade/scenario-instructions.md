# .NET Version Upgrade

## Preferences
- **Flow Mode**: Automatic
- **Target Framework**: net10.0 (LTS)

## Upgrade Options
**Source**: .github/upgrades/scenarios/dotnet-version-upgrade/upgrade-options.md

### Strategy
- Upgrade Strategy: All-at-Once

### Compatibility
- Unsupported API Handling: Fix Inline

## Source Control
- **Source Branch**: master
- **Working Branch**: upgrade-dotnet-10
- **Commit Strategy**: After Each Task
- **Branch Sync**: Auto (Merge)

## Key Decisions Log
- Pendant la mise à niveau .NET 10, rester sur xUnit v2 (2.9.3) — la migration v3 est traitée comme un chantier distinct.
- Migration vers xUnit v3 planifiée après la mise à niveau : cible `xunit.v3` 3.2.2 + `xunit.runner.visualstudio` 3.1.5 (versions stables, pas de préversion 4.0.0-pre.x).
- Migration xUnit v3 : conserver VSTest (`Microsoft.NET.Test.Sdk`) plutôt que basculer sur Microsoft Testing Platform, afin de préserver `dotnet test`, le Test Explorer et la couverture via `coverlet.collector`.

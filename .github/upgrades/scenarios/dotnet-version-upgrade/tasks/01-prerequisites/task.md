# 01-prerequisites: Vérifier la chaîne d'outils et la base de référence

Confirmer que le SDK .NET 10 est installé et utilisable, et vérifier qu'aucun fichier `global.json` n'épingle une version de SDK incompatible avec la cible. Établir également une base de référence en compilant la solution et en exécutant la suite de tests sur `net8.0-windows`, afin de disposer d'un point de comparaison fiable une fois la migration effectuée.

Cette tâche ne modifie pas le code applicatif. Si un `global.json` existe et contraint le SDK, il doit être mis à jour pour autoriser .NET 10.

**Done when**: Le SDK .NET 10 est confirmé disponible, aucun `global.json` ne bloque la cible, la solution compile sans erreur et la suite de tests passe sur le TFM actuel (résultats enregistrés comme référence).

## Research Findings

### Chaîne d'outils
- SDK actif : **10.0.302** (`dotnet --version`)
- SDK 10 disponibles : 10.0.110, 10.0.204, 10.0.302 — `validate_dotnet_sdk_installation(net10.0)` → « Compatible SDK found »
- Aucun `global.json` dans le dépôt → aucune contrainte de version de SDK à lever

### Projets en périmètre
- `MDRead\MDRead.csproj` — WPF, `net8.0-windows`, SDK-style, `Nullable=enable`, publication self-contained win-x64 single-file
- `MDRead.Tests\MDRead.Tests.csproj` — xUnit, `net8.0-windows`, SDK-style, référence `MDRead`

### Base de référence (net8.0-windows, Debug)
- Compilation : **0 erreur, 0 avertissement**
- Tests : **81 réussis / 81**, 0 échec, 0 ignoré

### Decisions Made
- Aucune modification de fichier requise : la chaîne d'outils est déjà conforme et aucun `global.json` n'existe.

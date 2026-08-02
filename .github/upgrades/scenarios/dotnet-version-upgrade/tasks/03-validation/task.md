# 03-validation: Valider la solution migrée

Effectuer une compilation complète de la solution en configuration Release, exécuter l'intégralité de la suite de tests et comparer les résultats à la base de référence établie en tâche 01. Vérifier également que la publication de l'application WPF (single-file, self-contained, win-x64) produit un artefact fonctionnel sous .NET 10.

Documenter toute recommandation différée — notamment l'introduction éventuelle de la gestion centralisée des packages (`Directory.Packages.props`), pertinente maintenant que les deux projets sont sur un TFM unique.

**Done when**: La solution compile en Release sans erreur ni avertissement, tous les tests passent (parité ou mieux avec la base de référence), la publication produit un exécutable valide, et les recommandations différées sont consignées.

## Research Findings

### Résultats de validation
- Compilation Release (`net10.0-windows`) : **0 erreur, 0 avertissement**
- Tests Release : **81 réussis / 81** — parité exacte avec la base de référence .NET 8 (tâche 01)
- Publication `dotnet publish -c Release` : `MDRead.exe` généré (single-file, self-contained, win-x64), ~62,4 Mo

### Recommandations différées
- **Gestion centralisée des packages (CPM)** : les deux projets sont désormais SDK-style sur un TFM unique. L'ajout d'un `Directory.Packages.props` peut se faire proprement, sans `VersionOverride`. Bénéfice limité à ce stade (2 projets, 7 packages) — à envisager si la solution grandit.
- **xUnit v3** : `xunit` 2.9.3 est supporté mais la ligne v2 ne reçoit plus que des correctifs de sécurité. Une migration vers xUnit v3 (avec `xunit.runner.visualstudio` 4.x) est un chantier distinct à planifier.

### Decisions Made
- Aucune modification de code ou de projet dans cette tâche : validation uniquement.

# 03-validation: Valider la solution migrée

Effectuer une compilation complète de la solution en configuration Release, exécuter l'intégralité de la suite de tests et comparer les résultats à la base de référence établie en tâche 01. Vérifier également que la publication de l'application WPF (single-file, self-contained, win-x64) produit un artefact fonctionnel sous .NET 10.

Documenter toute recommandation différée — notamment l'introduction éventuelle de la gestion centralisée des packages (`Directory.Packages.props`), pertinente maintenant que les deux projets sont sur un TFM unique.

**Done when**: La solution compile en Release sans erreur ni avertissement, tous les tests passent (parité ou mieux avec la base de référence), la publication produit un exécutable valide, et les recommandations différées sont consignées.

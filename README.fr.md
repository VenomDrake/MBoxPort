# MboxPort

[English](README.md) · [Italiano](README.it.md) · [简体中文](README.zh-Hans.md) · [हिन्दी](README.hi.md) · [Español](README.es.md) · [العربية](README.ar.md) · [Français](README.fr.md) · [বাংলা](README.bn.md) · [Português](README.pt.md) · [Bahasa Indonesia](README.id.md)

**Conversion gratuite et locale de MBOX en PST pour Windows. Outlook n’est pas nécessaire.**

[Modifications](CHANGELOG.md) · [Licence](LICENSE) · [Mentions relatives aux tiers](THIRD_PARTY.md)

[Télécharger la dernière version](https://github.com/VenomDrake/MBoxPort/releases/latest)

MboxPort convertit une archive MBOX standard à la fois en fichiers PST Unicode. Les messages et les pièces jointes restent sur votre ordinateur. L’application Windows inclut son environnement d’exécution .NET.

## Utilisation

1. Extrayez entièrement l’archive `MboxPort-1.0.1-Portatile-x64.zip` et lancez `MboxPort.exe`, ou utilisez le programme d’installation lorsqu’il est fourni.
2. Choisissez un fichier ou déposez une archive MBOX dans la fenêtre. Les archives sans extension de nom de fichier sont prises en charge.
3. Sélectionnez un dossier de destination existant et la taille approximative des parties PST : 10, 20 ou 40 GiB.
4. Lancez la conversion. Chaque exécution crée son propre dossier de résultats.
5. Ouvrez les résultats et consultez le rapport avant de vous fier à une archive comportant des avertissements ou des messages ignorés.

Nécessite Windows 10/11 x64. L’exécutable n’est pas signé numériquement.

## Langues

Le sélecteur de langue propose **10 langues**, dont l’italien. La langue initiale correspond à celle du système lorsqu’elle est prise en charge ; sinon, l’anglais est utilisé. La langue choisie est mémorisée localement.

| Langue | Code |
| --- | --- |
| English | `en` |
| Italiano | `it` |
| 简体中文 | `zh-Hans` |
| हिन्दी | `hi` |
| Español | `es` |
| العربية | `ar` |
| Français | `fr` |
| বাংলা | `bn` |
| Português | `pt` |
| Bahasa Indonesia | `id` |

L’interface, les messages de l’application et le rapport de conversion destiné à la lecture utilisent la langue sélectionnée. L’arabe utilise une disposition de droite à gauche. Les détails des diagnostics de bas niveau du moteur et du système d’exploitation peuvent rester dans leur langue d’origine. Les noms des propriétés JSON et des fichiers de rapport restent identiques dans toutes les langues.

## Résultats et annulation

- Les fichiers PST sont fractionnés automatiquement. La taille sélectionnée est approximative : les données de structure et un message particulièrement volumineux peuvent la faire dépasser.
- `rapporto.txt` est le rapport destiné à la lecture ; `rapporto-dettagliato.json` contient les détails du moteur ; `sessione.json` enregistre l’état de l’exécution et les compteurs finaux.
- Une erreur de lecture du fichier source arrête la conversion. Les parties PST terminées peuvent être conservées, mais elles constituent des résultats partiels.
- L’annulation supprime le PST incomplet en cours et conserve les parties terminées précédemment. Le nombre de messages enregistrés ne concerne que les fichiers conservés ; les messages supprimés avec la partie incomplète sont indiqués séparément.
- L’application vérifie le décompte final des messages et rouvre les PST terminés pour vérifier le nombre d’éléments qu’ils contiennent. Ces contrôles ne garantissent pas le rendu visuel de chaque message dans Outlook.
- Chaque conversion lit le fichier d’origine sans le modifier. Il n’y a pas de reprise automatique.

## Compatibilité et limites

Cette interface importe uniquement les e-mails au format MBOX/MIME standard. Extrayez les exportations compressées avant de sélectionner le MBOX. Les variantes propres aux fournisseurs doivent être testées sur un échantillon. Un MBOX isolé ne contient pas nécessairement la structure des dossiers source, les étiquettes, les états de lecture, les contacts ou les calendriers.

Les fichiers d’entrée peuvent dépasser 20 GiB, mais une conversion complète de données réelles au-delà de 20 GiB n’a pas été validée. Les messages individuels de plus de 1900 MiB environ sont ignorés et signalés. L’analyse de messages volumineux peut nécessiter beaucoup de mémoire, et l’annulation peut attendre la fin de l’opération d’analyse en cours. Prévoyez de l’espace disque pour les fichiers PST produits et les pièces jointes temporaires. Utilisez un système de fichiers prenant en charge les fichiers volumineux.

## Compilation à partir des sources

Nécessite Windows x64, le SDK .NET **8.0.427** ou une version corrective ultérieure autorisée par `global.json`, ainsi qu’un accès à NuGet pour la première restauration.

```powershell
./build.ps1
```

Le script restaure les dépendances pour `win-x64` en respectant les fichiers de verrouillage, publie l’application autonome dans un dossier de préparation propre, exécute les tests de l’application et une sélection de tests de non-régression du moteur, puis convertit 240 messages synthétiques. Il crée les archives ZIP des sources et de la version portable, ainsi que `SHA256SUMS.txt`, dans `artifacts/`. Inno Setup est utilisé pour créer un programme d’installation lorsqu’il est disponible :

```powershell
./build.ps1 -InnoCompiler 'C:\path\to\Inno Setup 6\ISCC.exe'
```

Le nom et la version du produit sont définis dans `src/Directory.Build.props`. Le code source complet de ContinuMail et de son moteur PST est inclus. Les versions des dépendances NuGet sont enregistrées dans les fichiers de verrouillage. Les outils de compilation et les paquets NuGet ne sont pas fournis sous la forme d’un SDK hors ligne.

Après la restauration des dépendances par `build.ps1`, les contrôles individuels peuvent être exécutés comme suit :

```powershell
dotnet test tests/PostaBridge.Tests/PostaBridge.Tests.csproj -c Release -r win-x64 --no-restore
dotnet build third_party/ContinuMail/src/Mail2Pst.Cli/Mail2Pst.Cli.csproj -c Release --no-restore
dotnet test third_party/ContinuMail/tests/Mail2Pst.Core.Tests/Mail2Pst.Core.Tests.csproj -c Release -r win-x64 --no-restore
```

La compilation séparée de l’interface en ligne de commande fournit le chemin attendu par les tests de bout en bout du projet d’origine. Elle ne modifie pas la configuration de restauration de l’application. Consultez `VALIDAZIONE.txt` pour connaître les contrôles réellement effectués et leurs limites.

Un script facultatif de relecture indépendante est inclus dans `tools/verify_independent.py`. Il nécessite Python 3.12+ et `pypstreader==1.0.0` ; il utilise les résultats synthétiques de SmokeTest.

## Origine et licence

MboxPort s’appelait auparavant **PostaBridge**. Il s’agit d’une intégration indépendante basée sur [ContinuMail](https://github.com/ContinuMail/continumail-converter), avec [PSTFileFormat](https://github.com/ROM-Knowledgeware/PSTFileFormat). Le logiciel est distribué sous **GNU GPL v3 ou ultérieure**, sans garantie. La bibliothèque PST conserve ses conditions LGPL. Les mentions de droit d’auteur existantes, les licences et les attributions du code source sont conservées dans [THIRD_PARTY.md](THIRD_PARTY.md) et dans le code source inclus du projet d’origine.

Les archives des versions publiées incluent le code source correspondant et les mentions de licence.

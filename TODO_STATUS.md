# Statut de la TODO List - Projet Labyrinth

> Document genere automatiquement - Derniere mise a jour : 05/02/2026

---

## Legende

| Symbole | Signification |
|---------|---------------|
| [x] | Implemente |
| [~] | Partiellement implemente |
| [ ] | Non implemente |
| (branche) | Disponible sur une branche specifique |

---

## 0) Cadrage du repo

### Solution et projets

| Statut | Element | Details |
|--------|---------|---------|
| [~] | Solution .sln avec projets | **3 projets au lieu de 4 :** |
| | | - `Labyrinth` (Console) - equivalent de `Labyrinth.Client` |
| | | - `ApiTypes` (Class Library) - DTOs uniquement |
| | | - `LabyrinthTest` (NUnit) - equivalent de `Labyrinth.Tests` |
| [ ] | `Labyrinth.Core` (Class Library) | Non cree - la logique est dans le projet Console |
| [ ] | `Labyrinth.TrainingServer` (ASP.NET Web API) | Non cree |

### Conventions et configuration

| Statut | Element | Details |
|--------|---------|---------|
| [x] | Nullable enable | Configure dans les .csproj |
| [x] | ImplicitUsings enable | Configure |
| [ ] | Analyzers | Non configures |
| [~] | Structure dossiers | Existe mais non conforme au schema demande |
| [~] | Formatage/nommage | Conventions C# standard mais pas de `.editorconfig` |

### Documentation

| Statut | Element | Details |
|--------|---------|---------|
| [x] | README.md | Complet avec structure, installation, CI/CD |

---

## 1) Bibliotheque de classes Labyrinth.Core

> Note: Cette bibliotheque n'existe pas en tant que projet separe. Les elements ci-dessous sont dans le projet `Labyrinth` (Console).

### Modeles

| Statut | Modele | Localisation |
|--------|--------|--------------|
| [x] | `Tile` (Wall/Room/Door) | `Labyrinth/Tiles/*.cs` |
| [x] | `Position` | `Labyrinth/Pathfinding/Position.cs` |
| [x] | `Direction` | `Labyrinth/Crawl/Direction.cs` |
| [~] | `Maze` | `Labyrinth/Labyrinth.cs` - classe partielle |
| [x] | `Door` | `Labyrinth/Tiles/Door.cs` |
| [x] | `Key` | `Labyrinth/Items/Key.cs` |
| [x] | `Inventory` | `Labyrinth/Items/Inventory.cs` (abstract) |
| [~] | `CrawlerState` | Pas de classe dediee - etat dans ICrawler |

**Types de tuiles implementes:**
- `Wall` - mur non traversable (singleton)
- `Room` - piece traversable, peut contenir des items
- `Door` - porte verrouillable/deverrouillable
- `Outside` - limite du labyrinthe (singleton)
- `Unknown` - tuile non exploree

### Abstractions / Interfaces

| Statut | Interface | Details |
|--------|-----------|---------|
| [x] | `ICrawler` | `Labyrinth/Crawl/ICrawler.cs` |
| [ ] | `ICrawlerController` | Non implemente |
| [ ] | `IMazeClient` | Non implemente |
| [ ] | `IExplorationStrategy` | Non implemente |
| [ ] | `ISharedMap` / `SharedKnowledge` | Non implemente |
| [x] | `IBuilder` | `Labyrinth/Build/IBuilder.cs` |
| [x] | `ICollectable` | `Labyrinth/Items/ICollectable.cs` |
| [x] | `IPathfinder` | `Labyrinth/Pathfinding/IPathfinder.cs` |

### Representation du plan commun

| Statut | Element | Details |
|--------|---------|---------|
| [ ] | Carte partielle (cells inconnues/connues) | Non implemente |
| [ ] | Memo portes/cles/frontieres | Non implemente |
| [~] | Type `Unknown` pour tuiles non explorees | `Labyrinth/Tiles/Unknown.cs` existe |

### Pathfinding

| Statut | Element | Details |
|--------|---------|---------|
| [x] | BFS sur carte connue | `Labyrinth/Pathfinding/BfsPathfinder.cs` |
| [x] | Gestion "inconnu" dans pathfinding | Tuiles Unknown traitees comme frontieres |
| [~] | Exploration | `RandExplorer` (aleatoire) + `FindNearestUnknown` (BFS) |

### Concurrence

| Statut | Element | Details |
|--------|---------|---------|
| [ ] | Structures thread-safe | Non implemente |
| [ ] | Lock/ReaderWriterLockSlim | Non implemente |
| [ ] | Reservation/claim frontieres | Non implemente |
| [~] | Tests concurrence inventaire | Sur branche `feature/ParcoursAsynchrone` |

---

## 2) Client console Labyrinth.Client

> Note: Le projet s'appelle `Labyrinth` au lieu de `Labyrinth.Client`

### Entrees CLI

| Statut | Element | Details |
|--------|---------|---------|
| [~] | `--server <url>` | Via `launchSettings.json` mais pas CLI args parse |
| [~] | `--appKey <key>` | Via `launchSettings.json` mais pas CLI args parse |
| [ ] | `--mode local|training|competition` | Non implemente |
| [ ] | Logs lisibles (info/debug) | Pas de systeme de logging structure |

### Fonctionnel

| Statut | Element | Details |
|--------|---------|---------|
| [~] | Creation crawlers | 1 seul crawler supporte actuellement |
| [ ] | Support jusqu'a 3 crawlers | Non implemente |
| [ ] | Orchestrateur asynchrone (1 Task/crawler) | Non implemente |
| [ ] | CancellationToken propre | Non implemente |
| [~] | Actions async (look/move/pick/use) | Partiellement via `ClientCrawler` |

### Strategie d'exploration

| Statut | Element | Details |
|--------|---------|---------|
| [~] | Exploration individuelle | `RandExplorer` - aleatoire uniquement |
| [ ] | Frontier-based (DFS/BFS) | Non implemente |
| [ ] | Strategie collective | Non implemente |
| [ ] | Attribution zones/frontieres | Non implemente |
| [ ] | Partage plan entre crawlers | Non implemente |
| [ ] | Eviter doublons exploration | Non implemente |

### Gestion portes/clefs

| Statut | Element | Details |
|--------|---------|---------|
| [x] | Logique portes/clefs | Implemente dans `Door.cs` et `Key.cs` |
| [x] | Passage de porte avec cle | `Door.Pass(Inventory)` fonctionne |
| [ ] | Strategie "garder cle et ouvrir sa porte" | Non implemente |

### Historique et debug

| Statut | Element | Details |
|--------|---------|---------|
| [ ] | Export JSON/CSV des moves | Non implemente |
| [ ] | Export decouverte map | Non implemente |
| [ ] | Rendu ASCII de la carte | Non implemente |

### Client API distant

| Statut | Element | Details |
|--------|---------|---------|
| [x] | `ContestSession` | `Labyrinth/ApiClient/ContestSession.cs` |
| [x] | `ClientCrawler` | `Labyrinth/ApiClient/ClientCrawler.cs` |
| [x] | DTOs (ApiTypes) | Projet `ApiTypes/` complet |

---

## 3) Serveur d'entrainement Labyrinth.TrainingServer

| Statut | Element | Details |
|--------|---------|---------|
| [ ] | Projet ASP.NET Web API | **Projet non cree** |
| [ ] | Endpoints/DTO conformes a l'API | Non implemente |
| [ ] | Auth avec cle d'application | Non implemente |
| [ ] | Labyrinthe(s) predefinis | Non implemente (configs JSON existent) |
| [ ] | Concurrence cote serveur | Non implemente |
| [ ] | Ramassage atomique | Non implemente |
| [ ] | Swagger integre | Non implemente |

**Note:** Des fichiers de configuration de labyrinthes existent :
- `labyrinth9x7.json` - petit labyrinthe
- `labyrinth17x19.json` - labyrinthe moyen

---

## 4) Projet de tests Labyrinth.Tests

> Note: Le projet s'appelle `LabyrinthTest` au lieu de `Labyrinth.Tests`

### Configuration

| Statut | Element | Details |
|--------|---------|---------|
| [x] | Projet de test NUnit | `LabyrinthTest/` avec NUnit 4.4.0 |
| [x] | Mocking (Moq) | Moq 4.20.72 configure |

### Tests unitaires existants

| Fichier | Tests | Couverture |
|---------|-------|------------|
| `DirectionTest.cs` | 9 tests | North/South/East/West, TurnLeft/TurnRight |
| `LabyrinthCrawlerTest.cs` | 15 tests | Init, bordures, mouvements, items, portes |
| `ExplorerTest.cs` | 13 tests | RandExplorer, GetOut, portes/cles |
| `BfsPathfinderTest.cs` | 31 tests | Position, FindPath, FindNearestUnknown, Labyrinth 21x19 |

### Tests demandes

| Statut | Element | Details |
|--------|---------|---------|
| [x] | Pathfinding (BFS) | `LabyrinthTest/Pathfinding/BfsPathfinderTest.cs` |
| [~] | Mise a jour carte | Tests d'exploration basiques |
| [ ] | Reservation frontier | Non implemente |
| [x] | Logique portes/clefs | Teste dans `LabyrinthCrawlerTest.cs` |
| [ ] | Tests d'integration serveur | Non implemente (pas de serveur) |

---

## 5) Pipeline CI GitHub Actions

> CI/CD merge dans master via PR #1

### Workflow sur push + PR

| Statut | Element | Details |
|--------|---------|---------|
| [x] | restore | `dotnet restore` |
| [x] | build | `dotnet build --configuration Release` |
| [x] | test | `dotnet test` avec TRX logging |
| [x] | Upload artefacts tests | Test results en TRX |
| [x] | Upload artefacts build | Binaires Release |

### Release binaires

| Statut | Element | Details |
|--------|---------|---------|
| [x] | Trigger sur tag `v*` | Configure |
| [x] | Publish Linux x64 | Self-contained, single file |
| [x] | Publish Windows x64 | Self-contained, single file |
| [x] | Publish macOS x64 | Self-contained, single file |
| [x] | Creation release GitHub | Avec softprops/action-gh-release |

---

## Resume global

### Par section

| Section | Progres | Commentaire |
|---------|---------|-------------|
| 0) Cadrage repo | ~40% | README OK, structure incomplete |
| 1) Labyrinth.Core | ~45% | Modeles + Pathfinding BFS OK |
| 2) Labyrinth.Client | ~20% | Exploration basique, pas de multi-agents |
| 3) TrainingServer | 0% | **Non commence** |
| 4) Tests | ~65% | 68 tests (dont 31 pathfinding avec labyrinthe 21x19) |
| 5) CI/CD | **100%** | Merge dans master |

### Priorites recommandees

1. ~~Merger la branche `aurelien/cicd`~~ - **FAIT**
2. ~~Creer le README.md~~ - **FAIT**
3. ~~Implementer pathfinding BFS~~ - **FAIT** (+ tests labyrinthe 21x19)
4. **Restructurer en 4 projets** - Separer Core du Client
5. **Creer Labyrinth.TrainingServer** - Serveur local pour tests
6. **Implementer multi-agents** - Support 3 crawlers avec orchestration

---

## Fichiers du projet actuel

```
labyrinth-aurelien-florian-geoffrey-hugo/
├── Labyrinth.sln
├── README.md                    # Documentation du projet
├── LICENSE.txt
├── .gitignore
├── .gitattributes
│
├── .github/workflows/
│   └── dotnet.yml               # Pipeline CI/CD
│
├── ApiTypes/                    # DTOs pour l'API
│   ├── Crawler.cs
│   ├── Direction.cs
│   ├── InventoryItem.cs
│   ├── ItemType.cs
│   ├── Settings.cs
│   └── TileType.cs
│
├── Labyrinth/                   # Application console
│   ├── Program.cs
│   ├── Labyrinth.cs
│   ├── LabyrinthCrawler.cs
│   ├── RandExplorer.cs
│   ├── labyrinth9x7.json
│   ├── labyrinth17x19.json
│   ├── ApiClient/
│   ├── Build/
│   ├── Crawl/
│   ├── Items/
│   ├── Pathfinding/             # NOUVEAU - BFS pathfinder
│   │   ├── Position.cs
│   │   ├── PathResult.cs
│   │   ├── IPathfinder.cs
│   │   └── BfsPathfinder.cs
│   ├── Sys/
│   └── Tiles/
│
└── LabyrinthTest/               # Tests NUnit
    ├── ExplorerTest.cs
    ├── Crawl/
    │   ├── DirectionTest.cs
    │   └── LabyrinthCrawlerTest.cs
    └── Pathfinding/             # NOUVEAU - Tests pathfinding
        └── BfsPathfinderTest.cs
```

---

## Branches disponibles

| Branche | Contenu | Statut |
|---------|---------|--------|
| `master` | Branche principale avec CI/CD | Active |
| `aurelien/cicd` | Pipeline CI/CD GitHub Actions | Merge |
| `feature/ParcoursAsynchrone` | Tests concurrence inventaire | A merger |

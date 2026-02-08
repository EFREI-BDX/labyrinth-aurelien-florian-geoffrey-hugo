# TODO List - Projet Labyrinth

## Legende
- [x] Termine | [~] En cours | [ ] Non commence

---

## 1. Infrastructure

| # | Tache | Statut |
|---|-------|--------|
| 1.1 | Solution .NET avec projets (Labyrinth, ApiTypes, LabyrinthTest, TrainingServer) | [x] |
| 1.2 | Projet `Labyrinth.Core` (Class Library) separe | [ ] |
| 1.3 | Nullable/ImplicitUsings enable | [x] |
| 1.4 | Analyzers + `.editorconfig` | [ ] |
| 1.5 | README.md complet | [x] |
| 1.6 | **CI/CD GitHub Actions** (build, test, release multi-plateforme) | [x] |

---

## 2. Modeles & Abstractions

| # | Tache | Statut |
|---|-------|--------|
| 2.1 | Tiles: `Tile`, `Wall`, `Room`, `Door`, `Outside`, `Unknown` | [x] |
| 2.2 | Items: `Key`, `Inventory`, `ICollectable` | [x] |
| 2.3 | `Position`, `Direction` | [x] |
| 2.4 | Interfaces: `ICrawler`, `IBuilder`, `IPathfinder` | [x] |
| 2.5 | Interfaces manquantes: `IExplorationStrategy`, `ISharedMap`, `IMazeClient` | [ ] |

---

## 3. Pathfinding

| # | Tache | Statut |
|---|-------|--------|
| 3.1 | BFS sur carte connue | [x] |
| 3.2 | `FindPath(from, to)` chemin optimal | [x] |
| 3.3 | `FindNearestUnknown(from)` frontiere proche | [x] |
| 3.4 | Gestion tuiles Unknown comme frontieres | [x] |

---

## 4. Carte partagee & Concurrence

| # | Tache | Statut |
|---|-------|--------|
| 4.1 | Carte partielle (cells connues/inconnues) | [~] |
| 4.2 | Memo portes/cles/frontieres | [ ] |
| 4.3 | `ISharedMap` - systeme de partage carte commune | [ ] |
| 4.4 | Structures thread-safe (Lock/ReaderWriterLockSlim) | [ ] |
| 4.5 | Reservation/claim des frontieres | [ ] |
| 4.6 | Visualisation des crawlers sur SharedMap | [ ] |

---

## 5. Client Console

| # | Tache | Statut |
|---|-------|--------|
| 5.1 | Client API distant (`ContestSession`, `ClientCrawler`, DTOs) | [x] |
| 5.2 | Arguments CLI (`--server`, `--appKey`, `--crawlers`) | [x] |
| 5.3 | Logging structure (info/debug) | [ ] |
| 5.4 | Portes/cles - logique passage | [x] |
| 5.5 | Strategie "garder cle pour sa porte" | [ ] |
| 5.6 | Export debug (JSON/CSV mouvements, rendu ASCII) | [ ] |

---

## 6. Multi-Crawlers & Asynchronie

| # | Tache | Statut |
|---|-------|--------|
| 6.1 | Support jusqu'a 3 crawlers simultanement | [x] |
| 6.2 | Orchestrateur asynchrone (1 Task/crawler) | [x] |
| 6.3 | Refactoriser crawlers pour meilleure asynchronie | [x] |
| 6.4 | Support CancellationToken | [x] |
| 6.5 | Actions async (look/move/pick/use) | [~] |

---

## 7. Strategies d'exploration

| # | Tache | Statut |
|---|-------|--------|
| 7.1 | Exploration aleatoire (`RandExplorer`) | [x] |
| 7.2 | Frontier-based exploration (DFS/BFS) | [ ] |
| 7.3 | Strategie collective multi-agents | [ ] |
| 7.4 | Attribution zones/frontieres | [ ] |
| 7.5 | Eviter doublons exploration | [ ] |

---

## 8. Serveur d'entrainement (TrainingServer)

| # | Tache | Statut |
|---|-------|--------|
| 8.1 | Projet ASP.NET Web API + Swagger | [x] |
| 8.2 | Auth via appKey | [x] |
| 8.3 | API Crawlers (POST/PATCH/DELETE `/crawlers`) | [x] |
| 8.4 | API Inventaires (PUT `/crawlers/{id}/bag`, `/items`) | [x] |
| 8.5 | API Labyrinthe et Tiles | [x] |
| 8.6 | Concurrence thread-safe cote serveur | [x] |
| 8.7 | Routes API manquantes (enhancement) | [~] |

---

## 9. Tests

| # | Tache | Statut | Count |
|---|-------|--------|-------|
| 9.1 | Tests Direction | [x] | 9 |
| 9.2 | Tests LabyrinthCrawler | [x] | 15 |
| 9.3 | Tests Explorer | [x] | 16 |
| 9.4 | Tests BFS Pathfinder | [x] | 31 |
| 9.5 | Tests LabyrinthService | [x] | 14 |
| 9.6 | Tests LabyrinthMap | [x] | 16 |
| 9.7 | Tests CrawlerOrchestrator | [x] | 10 |
| 9.8 | Tests concurrence (multi-threads, reservation) | [ ] | - |
| 9.9 | Augmenter couverture tests | [~] | - |

---

## Priorites

### Haute
- [x] 6.1-6.4 Multi-Crawlers + CancellationToken
- [ ] 4.3-4.5 SharedMap + Thread-Safety
- [ ] 7.2-7.5 Strategies exploration avancees

### Moyenne
- [x] 5.2 CLI (--crawlers)
- [ ] 5.3 Logging
- [ ] 4.6 Visualisation SharedMap
- [ ] 9.8-9.9 Tests concurrence + couverture

### Basse
- [ ] 1.2 Labyrinth.Core separe
- [ ] 5.6 Export debug

---

## Commandes

```bash
dotnet build                                    # Build
dotnet test                                     # Tests (115 tests)
dotnet test --filter "FullyQualifiedName~TrainingServer"  # Tests serveur

# Serveur entrainement
cd Labyrinth.TrainingServer && dotnet run --urls "http://localhost:5123"

# Client avec multi-crawlers
dotnet run --project Labyrinth -- --crawlers 3
dotnet run --project Labyrinth -- https://server.example guid --crawlers 2
```

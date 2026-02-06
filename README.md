# Labyrinth

## Structure du projet

```
Labyrinth.sln
├── Labyrinth/                    # Application console (client)
├── ApiTypes/                     # Types et modèles partagés (DTOs)
├── Labyrinth.TrainingServer/     # Serveur d'entraînement local
└── LabyrinthTest/                # Tests unitaires (NUnit)
```

## Prérequis

- [.NET 10.0 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)

## Installation et exécution

```bash
# Restaurer les dépendances
dotnet restore

# Compiler le projet
dotnet build

# Lancer l'application client
dotnet run --project Labyrinth

# Lancer les tests
dotnet test
```

---

## Serveur d'Entraînement Local

Le projet inclut un serveur d'entraînement pour permettre le développement et les tests en local.

### Lancer le serveur

```bash
# Depuis la racine du projet
dotnet run --project Labyrinth.TrainingServer

# Ou avec une URL spécifique
dotnet run --project Labyrinth.TrainingServer --urls "http://localhost:5297"
```

Le serveur démarre sur `http://localhost:5297` par défaut.

### Endpoints disponibles

| Méthode | Route | Description |
|---------|-------|-------------|
| `GET` | `/crawlers?appKey={guid}` | Liste tous les crawlers de l'appKey |
| `POST` | `/crawlers?appKey={guid}` | Crée un nouveau crawler (max 3) |
| `GET` | `/crawlers/{id}?appKey={guid}` | Récupère un crawler spécifique |
| `PATCH` | `/crawlers/{id}?appKey={guid}` | Met à jour direction/mouvement |
| `DELETE` | `/crawlers/{id}?appKey={guid}` | Supprime un crawler |
| `GET` | `/crawlers/{id}/bag?appKey={guid}` | Récupère l'inventaire du bag |
| `PUT` | `/crawlers/{id}/bag?appKey={guid}` | Transfère items du bag vers le sol |
| `GET` | `/crawlers/{id}/items?appKey={guid}` | Récupère les items sur la tuile |
| `PUT` | `/crawlers/{id}/items?appKey={guid}` | Ramasse des items du sol |

### Tester avec curl

```bash
# Définir l'appKey (n'importe quel GUID valide)
APP_KEY="00000000-0000-0000-0000-000000000001"
BASE_URL="http://localhost:5297"

# Créer un crawler
curl -X POST "$BASE_URL/crawlers?appKey=$APP_KEY" \
     -H "Content-Type: application/json" \
     -d '{}'

# Lister tous les crawlers
curl "$BASE_URL/crawlers?appKey=$APP_KEY"

# Récupérer un crawler (remplacer {id} par l'ID retourné)
curl "$BASE_URL/crawlers/{id}?appKey=$APP_KEY"

# Tourner vers l'Est
curl -X PATCH "$BASE_URL/crawlers/{id}?appKey=$APP_KEY" \
     -H "Content-Type: application/json" \
     -d '{"direction": "East", "walking": false}'

# Avancer (walking = true)
curl -X PATCH "$BASE_URL/crawlers/{id}?appKey=$APP_KEY" \
     -H "Content-Type: application/json" \
     -d '{"direction": "East", "walking": true}'

# Ramasser une clé sur le sol
curl -X PUT "$BASE_URL/crawlers/{id}/items?appKey=$APP_KEY" \
     -H "Content-Type: application/json" \
     -d '[{"type": "Key", "move-required": true}]'

# Voir le contenu du bag
curl "$BASE_URL/crawlers/{id}/bag?appKey=$APP_KEY"

# Supprimer un crawler
curl -X DELETE "$BASE_URL/crawlers/{id}?appKey=$APP_KEY"
```

---

## CI/CD avec GitHub Actions

Le projet utilise GitHub Actions pour l'intégration et le déploiement continus.

### Workflow `.github/workflows/dotnet.yml`

#### Déclencheurs

| Événement | Branche/Tag | Action |
|-----------|-------------|--------|
| `push` | `main` | Build + Tests |
| `pull_request` | `main` | Build + Tests |
| `push` (tag) | `v*` | Build + Tests + Release |

#### Jobs

##### 1. Build & Test (`build-and-test`)

Ce job s'exécute sur chaque push et pull request vers `main`.

| Étape | Description |
|-------|-------------|
| Checkout | Clone le code source |
| Setup .NET | Installe le SDK .NET 10.0 |
| Restore | Restaure les packages NuGet |
| Build | Compile la solution en mode Release |
| Test | Exécute les tests NUnit |
| Upload test-results | Archive les résultats des tests (.trx) |
| Upload build artifacts | Archive les binaires compilés |

**Artefacts produits :**
- `test-results` : Résultats des tests au format TRX (rétention : 30 jours)
- `labyrinth-build` : Binaires de l'application (rétention : 30 jours)

##### 2. Release (`release`)

Ce job s'exécute uniquement lors d'un push de tag commençant par `v` (ex: `v1.0.0`).

| Étape | Description |
|-------|-------------|
| Checkout | Clone le code source |
| Setup .NET | Installe le SDK .NET 10.0 |
| Publish Linux | Crée un exécutable self-contained pour Linux x64 |
| Publish Windows | Crée un exécutable self-contained pour Windows x64 |
| Publish macOS | Crée un exécutable self-contained pour macOS x64 |
| Zip | Archive les binaires en fichiers ZIP |
| Create Release | Crée une release GitHub avec les archives |

**Artefacts de release :**

| Fichier | Plateforme | Description |
|---------|------------|-------------|
| `Labyrinth-linux-x64.zip` | Linux | Exécutable autonome |
| `Labyrinth-win-x64.zip` | Windows | Exécutable autonome |
| `Labyrinth-osx-x64.zip` | macOS | Exécutable autonome |

### Créer une nouvelle release

Pour créer une nouvelle release du projet :

```bash
# 1. S'assurer que tous les changements sont commités
git add .
git commit -m "Prepare release v1.0.0"

# 2. Créer un tag de version
git tag v1.0.0

# 3. Pousser le tag vers GitHub
git push origin v1.0.0
```


# Labyrinth

## Structure du projet

```
Labyrinth.sln
├── Labyrinth/          # Application console principale
├── ApiTypes/           # Types et modèles partagés
└── LabyrinthTest/      # Tests unitaires (NUnit)
```

## Prérequis

- [.NET 10.0 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)

## Installation et exécution

```bash
# Restaurer les dépendances
dotnet restore

# Compiler le projet
dotnet build

# Lancer l'application
dotnet run --project Labyrinth

# Lancer les tests
dotnet test
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

> **Note :** Les binaires sont self-contained, ce qui signifie qu'ils n'ont pas besoin de .NET installé sur la machine cible.

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

Le workflow GitHub Actions va automatiquement :
1. Compiler le projet
2. Exécuter les tests
3. Créer les binaires pour Linux, Windows et macOS
4. Publier une release GitHub avec les fichiers téléchargeables

### Visualiser les résultats

- **Statut des workflows** : Onglet "Actions" du dépôt GitHub
- **Artefacts de build** : Dans le détail de chaque run de workflow
- **Releases** : Onglet "Releases" du dépôt GitHub


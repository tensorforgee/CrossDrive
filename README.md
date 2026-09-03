# Crosswire

Crosswire is a top-down 2D multiplayer party game being built for the RevenueCat Shipaton 2026.

The project is currently in **Phase 0: foundation and documentation**. It intentionally contains no gameplay, networking, scoring, matchmaking, menu, monetization, or final-art implementation.

## Unity setup

- Unity version: **6000.3.18f1 (Unity 6.3 LTS)**
- Rendering: built-in 2D-capable Unity project; no scriptable render pipeline has been selected
- Packages: no optional packages are currently installed
- Targets: Android and iOS

Install the Android Build Support module to build for Android. iOS builds require the iOS Build Support module and a macOS/Xcode environment for the final Xcode build.

Open this repository folder directly from Unity Hub. Unity will generate local metadata and caches on first open; those generated files are excluded from version control.

## Project layout

```text
Assets/_Project/
├── Art/
├── Audio/
├── Prefabs/
├── Scenes/
├── Scripts/
│   ├── Core/
│   ├── Gameplay/
│   ├── Input/
│   ├── Networking/
│   ├── Scoring/
│   ├── UI/
│   └── Monetization/
└── Tests/
```

The empty folders contain `.gitkeep` placeholders until Unity assets are added.

## Documentation

- [Game specification](docs/GAME_SPEC.md)
- [Architecture](docs/ARCHITECTURE.md)
- [Phases](docs/PHASES.md)


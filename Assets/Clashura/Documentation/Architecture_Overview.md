# CLASHURA — 2D Architecture Overview
## Engine: Unity 6 (6000.0.61f1) — Pure 2D Game Architecture

---

### 1. Master Project Directory Organization
All custom game development content is organized strictly inside `Assets/Clashura/`:

```
Assets/
└── Clashura/
    ├── Art/
    │   ├── Characters/
    │   │   ├── Portraits/            # 18 2D character portrait icons (128x128)
    │   │   ├── SelectionArtwork/     # 18 High-resolution 2D selection artworks (256x384)
    │   │   ├── Sprites/              # 18 2D in-game character sprites (64x96)
    │   │   ├── SpriteSheets/         # Dedicated folder for upcoming 2D animation sprite sheets
    │   │   └── Icons/                # 72 2D ability icons (64x64)
    │   ├── UI/                       # 2D frames, borders, banners, backgrounds
    │   ├── Icons/                    # General game icons
    │   └── Materials/                # 2D UI & sprite materials
    │
    ├── Audio/
    │   ├── Music/                    # Selection screen and ambient soundtrack
    │   └── SFX/                      # UI navigation, select, error, confirm audio
    │
    ├── Data/
    │   ├── Characters/
    │   │   ├── ScriptableObjects/    # 18 CharacterDataSO assets & CharacterDatabaseSO
    │   │   └── AbilityData/          # 72 AbilityDataSO assets (4 per character)
    │   ├── Scenes/                   # Scene configuration data assets
    │   └── Game/                     # Game system configuration ScriptableObjects
    │
    ├── Prefabs/
    │   ├── Characters/               # 18 2D Character Prefabs (SpriteRenderer, BoxCollider2D, Rigidbody2D)
    │   ├── UI/                       # AbilitySlotUI, SelectedSlotUI, SelectionCanvas prefabs
    │   └── Managers/                 # SelectionManager prefabs
    │
    ├── Scenes/
    │   ├── CharacterSelection/       # CharacterSelection.unity (Main 2D selection screen)
    │   ├── CharacterPreviews/        # Reserved for dedicated preview scenes
    │   └── MainMenu/                 # Reserved for Main Menu
    │
    ├── Scripts/
    │   ├── Core/                     # Enums (CharacterRole, AbilityType) & CharacterStats
    │   ├── Characters/
    │   │   ├── Data/                 # CharacterDataSO.cs, AbilityDataSO.cs
    │   │   ├── Managers/             # CharacterDatabaseSO.cs
    │   │   └── Preview/              # CharacterVisual2DController.cs (Smooth 2D transitions)
    │   │
    │   ├── CharacterSelection/
    │   │   ├── Managers/             # CharacterSelectionManager.cs
    │   │   ├── UI/                   # CharacterSelectionUI.cs, SelectedSlotUI.cs, AbilitySlotUI.cs
    │   │   └── SaveSystem/           # ISelectionSaveSystem.cs, PlayerPrefsSelectionSaveSystem.cs, PlayerSelectionData.cs
    │   │
    │   ├── UI/
    │   │   └── Responsive/           # SafeAreaHandler.cs, ResponsiveLayoutController.cs
    │   └── Utilities/                # ClashuraAssetGenerator.cs, ClashuraSceneBuilder.cs
    │
    ├── Settings/                     # Project presets and graphics overrides
    └── Documentation/                # Architectural manuals and technical guides
```

---

### 2. 2D Visual System Architecture

- **2D Character Data (`CharacterDataSO`)**:
  - `Portrait`: Square 2D headshot sprite used in selection slots, HUD, and mini frames.
  - `SelectionArtwork`: Large 2D illustration displayed on the Character Selection Screen with smooth cross-fade transitions.
  - `CharacterSprite`: In-game 2D gameplay sprite assigned to the character's `SpriteRenderer`.
  - `CharacterPrefab2D`: Prefab equipped with `SpriteRenderer` (sorting layer: Default), `BoxCollider2D`, and kinematic `Rigidbody2D`.
  - `SpriteSheetFrames`: Placeholder frame array for future 2D frame-by-frame or skeletal animation.
  - All visual assets are completely decoupled from game logic, allowing developers to swap final art assets without touching C# code.

- **Smooth 2D Transitions (`CharacterVisual2DController`)**:
  - Employs double-buffered `CanvasGroup` components on UI Images to execute smooth cross-fade and slide interpolations when switching characters.
  - Aspect ratio preservation enabled by default.

- **Selection Logic & Persistence**:
  - `CharacterSelectionManager` enforces the 3-hero cap and prevents duplicates.
  - Persistent storage through `ISelectionSaveSystem` and `PlayerPrefsSelectionSaveSystem` (JSON format).

- **Responsive Multi-Platform Support**:
  - `CanvasScaler` configured with 1920×1080 reference resolution.
  - `SafeAreaHandler` protects against notches/islands across mobile aspect ratios.
  - `ResponsiveLayoutController` handles dynamic screen reorientation and resolutions from 720×1280 to 3440×1440 ultrawide.

# CLASHURA — 2D System & Integration Guide

### 1. 2D Character Selection Sequence

```mermaid
sequenceDiagram
    participant Player as Player Input
    participant UI as CharacterSelectionUI
    participant Mgr as CharacterSelectionManager
    participant Visual as CharacterVisual2DController
    participant Save as PlayerPrefsSelectionSaveSystem

    Note over Mgr: Game Starts (Awake / Start)
    Mgr->>Save: LoadSelection("Player_Local")
    Save-->>Mgr: Returns saved IDs (if any)
    Mgr->>UI: Broadcast OnCurrentCharacterChanged(char0)
    UI->>Visual: DisplayCharacterArtwork(char0.SelectionArtwork)
    Mgr->>UI: Broadcast OnSelectionsUpdated(savedList)

    Player->>UI: Click Next/Prev Arrow
    UI->>Mgr: NextCharacter() / PreviousCharacter()
    Mgr->>UI: Broadcast OnCurrentCharacterChanged
    UI->>Visual: Smooth Cross-fade 2D Transition to new Selection Artwork

    Player->>UI: Click "SELECT FOR TEAM"
    UI->>Mgr: ToggleCurrentCharacterSelection()
    alt Already Selected
        Mgr->>UI: OnSelectionError("Character already selected")
    else Team Full (3 heroes)
        Mgr->>UI: OnSelectionError("Team is full (3/3)")
    else Valid Selection
        Mgr->>UI: Broadcast OnSelectionsUpdated (Slot Filled)
    end

    Player->>UI: Click "CONFIRM TEAM" (Enabled at 3 heroes)
    UI->>Mgr: ConfirmSelection()
    Mgr->>Save: SaveSelection(PlayerSelectionData)
    Mgr->>UI: Broadcast OnSelectionConfirmed ("Team Confirmed & Saved")
```

---

### 2. Multi-Resolution Compatibility Matrix

| Screen Resolution | Aspect Ratio | Device Category | Scaling Behavior |
| :--- | :--- | :--- | :--- |
| **720 × 1280** | 9:16 (0.56) | Mobile Portrait (Compact) | PortraitMobile (Match Width 0.0) |
| **1080 × 1920** | 9:16 (0.56) | Mobile Portrait (FHD) | PortraitMobile (Match Width 0.0) |
| **1920 × 1080** | 16:9 (1.78) | Desktop Standard / Landscape Mobile | DesktopStandard (Match Height 1.0) |
| **2560 × 1440** | 16:9 (1.78) | Desktop 2K QHD | DesktopStandard (Match Height 1.0) |
| **3440 × 1440** | 21:9 (2.39) | Desktop Ultrawide Curved | UltrawideDesktop (Safe area anchored) |

---

### 3. Replacing Placeholder Art with Final Artwork

When final art assets are delivered:
1. **Portraits**: Place 128×128 (or higher) PNGs in `Assets/Clashura/Art/Characters/Portraits/`.
2. **Selection Artwork**: Place large illustrations in `Assets/Clashura/Art/Characters/SelectionArtwork/`.
3. **Gameplay Sprites**: Place 2D character sprites in `Assets/Clashura/Art/Characters/Sprites/`.
4. **Sprite Sheets**: Place 2D animation sheets in `Assets/Clashura/Art/Characters/SpriteSheets/`.
5. Open the respective `CharacterDataSO` asset in `Assets/Clashura/Data/Characters/ScriptableObjects/` and assign the imported sprite references.
6. Zero C# script changes are required when upgrading artwork.

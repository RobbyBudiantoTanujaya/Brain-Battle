# Kings Scene – Unity Editor Setup Guide

Unity 6.0.75f1 LTS · 2D URP · Brain Battle

---

## 1. Camera

Select **Main Camera** in the scene.

| Setting | Value |
|---|---|
| Projection | Orthographic |
| Size | 5 |
| Clear Flags | Solid Color |
| Background | #1A1A2E (dark navy) |
| Culling Mask | Everything |
| Tag | MainCamera |

Remove any default Directional Light — 2D URP doesn't need it.

---

## 2. Scene Hierarchy

Build this exact tree. Indentation = parenting. Active/inactive state must match — it matters for event wiring.

```
Canvas                                     ← Screen Space – Overlay, CanvasScaler
├── GridContainer                          ← RectTransform only (anchor center)
│
├── TutorialOverlay                        ← starts ACTIVE (hidden by TutorialController.Awake)
│   ├── StepText                           ← TextMeshProUGUI
│   ├── StepCounter                        ← TextMeshProUGUI
│   ├── NextButton                         ← Button
│   │   └── Label                          ← TextMeshProUGUI child (text = "Next")
│   └── SkipButton                         ← Button
│       └── Label                          ← TextMeshProUGUI child (text = "Skip")
│
├── TipsPanel                              ← starts ACTIVE (hidden by TipsButton.Awake)
│   ├── TipsText                           ← TextMeshProUGUI
│   └── CloseButton                        ← Button
│
├── RestartConfirmPanel                    ← starts ACTIVE (hidden by RestartButton.Awake)
│   ├── ConfirmButton                      ← Button  [see §4.4 for onClick wiring]
│   └── CancelButton                       ← Button  [see §4.4 for onClick wiring]
│
├── VictoryPanel                           ← starts ACTIVE  ⚠ see §7 mismatch note
│   └── VictoryContent                     ← starts INACTIVE (set by VictoryPanel.Awake)
│       ├── TimeText                       ← TextMeshProUGUI
│       ├── MoveCountText                  ← TextMeshProUGUI
│       ├── StarRatingText                 ← TextMeshProUGUI
│       ├── NextLevelButton                ← Button
│       ├── RestartButton                  ← Button
│       └── MainMenuButton                 ← Button
│
└── HUD                                    ← plain GameObject, anchored top
    ├── UndoButton                         ← Button + UndoButton.cs
    ├── RestartButton                      ← Button + RestartButton.cs
    ├── TipsButton                         ← Button + TipsButton.cs
    ├── TimerText                          ← TextMeshProUGUI  ⚠ see §7 mismatch note
    └── MoveCountText                      ← TextMeshProUGUI  ⚠ see §7 mismatch note

GameManager                                ← Empty GameObject
SceneBootstrap                             ← Empty GameObject
EventSystem                                ← auto-created with Canvas; keep it
```

---

## 3. Canvas Settings

Select **Canvas**.

| Component | Setting | Value |
|---|---|---|
| Canvas | Render Mode | Screen Space – Overlay |
| Canvas Scaler | UI Scale Mode | Scale With Screen Size |
| Canvas Scaler | Reference Resolution | 1170 × 2532 |
| Canvas Scaler | Match | 0.5 (Width–Height balance) |
| Graphic Raycaster | (keep defaults) | — |

---

## 4. GameObjects – Scripts and SerializeField Assignments

### 4.1 GameManager

Add three components to the **GameManager** empty GameObject:

#### KingsGameManager
| SerializeField | Assign |
|---|---|
| `_gridRenderer` | GridContainer (the KingsGridRenderer component on it) |
| `_currentGrid` | *(leave blank — set by code at runtime)* |
| `_tutorialController` | GameManager itself (the TutorialController component below) |

#### LevelLoader
| SerializeField | Assign |
|---|---|
| `_allLevels` | Size = 5, then drag the five LevelData assets (see §5) |

#### TutorialController
| SerializeField | Assign |
|---|---|
| `_overlayPanel` | Canvas → TutorialOverlay |
| `_stepText` | Canvas → TutorialOverlay → StepText |
| `_stepCounter` | Canvas → TutorialOverlay → StepCounter |
| `_nextButton` | Canvas → TutorialOverlay → NextButton |
| `_skipButton` | Canvas → TutorialOverlay → SkipButton |

> TutorialController lives on **GameManager** (not on TutorialOverlay) because its `Awake()` must run while the object is active. It calls `_overlayPanel.SetActive(false)` to hide TutorialOverlay on startup.

---

### 4.2 SceneBootstrap

Add one component to the **SceneBootstrap** empty GameObject:

#### KingsSceneBootstrap
| SerializeField | Assign |
|---|---|
| `_levelLoader` | GameManager (the LevelLoader component on it) |
| `_gameManager` | GameManager (the KingsGameManager component on it) |

---

### 4.3 GridContainer

Add one component to **GridContainer**:

#### KingsGridRenderer
| SerializeField | Value / Assign |
|---|---|
| `_dotSprite` | Dot sprite (see §6) |
| `_crownSprite` | Crown sprite (see §6) |
| `_padding` | `16` |
| `_borderThickness` | `2` |
| `_borderColor` | R:38 G:38 B:38 A:255 (near-black) |
| `_conflictColor` | R:255 G:60 B:60 A:255 (red) |
| `_pulseSpeed` | `0.3` |

Set **RectTransform**:
- Anchor: center-center (both min/max = 0.5)
- AnchoredPosition: (0, 0)
- SizeDelta: (900, 900) — renderer measures its own rect at runtime so any square is fine

---

### 4.4 RestartConfirmPanel buttons

RestartButton.cs exposes two **public** methods for Inspector-wired onClick events.

Select **ConfirmButton** → Button → OnClick (+) → drag **HUD → RestartButton** → choose `RestartButton.ConfirmRestart`.

Select **CancelButton** → Button → OnClick (+) → drag **HUD → RestartButton** → choose `RestartButton.CancelRestart`.

---

### 4.5 HUD / UndoButton

Add **UndoButton.cs** to the UndoButton GameObject.

| SerializeField | Assign |
|---|---|
| `_gameManager` | GameManager (KingsGameManager component) |
| `_button` | The Button component on this same GameObject |

---

### 4.6 HUD / RestartButton

Add **RestartButton.cs** to the RestartButton GameObject.

| SerializeField | Assign |
|---|---|
| `_gameManager` | GameManager (KingsGameManager component) |
| `_button` | The Button component on this same GameObject |
| `_confirmPanel` | Canvas → RestartConfirmPanel |

---

### 4.7 HUD / TipsButton

Add **TipsButton.cs** to the TipsButton GameObject.

| SerializeField | Assign |
|---|---|
| `_gameManager` | GameManager (KingsGameManager component) |
| `_button` | The Button component on this same GameObject |
| `_tipsPanel` | Canvas → TipsPanel |
| `_tipsText` | Canvas → TipsPanel → TipsText |
| `_closeButton` | Canvas → TipsPanel → CloseButton |

---

### 4.8 VictoryPanel

Add **VictoryPanel.cs** to the **VictoryPanel** GameObject (the one directly under Canvas — leave VictoryContent as its child).

| SerializeField | Assign |
|---|---|
| `_panel` | Canvas → VictoryPanel → VictoryContent |
| `_timeText` | Canvas → VictoryPanel → VictoryContent → TimeText |
| `_moveCountText` | Canvas → VictoryPanel → VictoryContent → MoveCountText |
| `_starRatingText` | Canvas → VictoryPanel → VictoryContent → StarRatingText |
| `_nextLevelButton` | Canvas → VictoryPanel → VictoryContent → NextLevelButton |
| `_restartButton` | Canvas → VictoryPanel → VictoryContent → RestartButton |
| `_mainMenuButton` | Canvas → VictoryPanel → VictoryContent → MainMenuButton |
| `_gameManager` | GameManager (KingsGameManager component) |
| `_sceneBootstrap` | SceneBootstrap (KingsSceneBootstrap component) |

---

## 5. LevelData ScriptableObjects

These are generated by the editor tool. Do **not** hand-author them.

1. Open **Tools → Brain Battle → Kings Level Generator** (KingsLevelGenerator.cs editor window).
2. Generate 5 levels. Assets land in `Assets/_Project/ScriptableObjects/`.
3. On **GameManager → LevelLoader → _allLevels**:
   - Set Size = 5
   - Slot 0 → Level_01 (4×4, difficulty = Easy)
   - Slot 1 → Level_02 (5×5, difficulty = Easy)
   - Slot 2 → Level_03 (6×6, difficulty = Medium)
   - Slot 3 → Level_04 (8×8, difficulty = Medium)
   - Slot 4 → Level_05 (10×10, difficulty = Hard)

`LevelLoader.GetLevel(n)` searches `_allLevels` by `_levelNumber` field, so array order doesn't affect lookup — but keeping it sorted avoids confusion.

---

## 6. Required Sprites

KingsGridRenderer draws **cells and borders procedurally** using Unity's Image component — no sprite is needed for those. Only two sprites are required.

### Crown Sprite
- Purpose: `KingsGridRenderer._crownSprite`
- Size: 128×128 px, transparent background
- Style: gold crown silhouette
- Import settings: Texture Type = Sprite (2D), Filter Mode = Bilinear, Max Size = 256

### Dot Sprite
- Purpose: `KingsGridRenderer._dotSprite`
- Size: 64×64 px, transparent background
- Style: small filled circle (marker / note dot)
- Import settings: same as crown

Place both in `Assets/_Project/Sprites/`. Assign to GridContainer → KingsGridRenderer in the Inspector.

> Cell background and region border sprites are **not needed** — the renderer creates colored Image components at runtime from region color data in LevelData.

---

## 7. Mismatch Report

The following discrepancies were found between the requested hierarchy and the actual scripts. Each is called out with the required fix.

---

### MISMATCH 1 — VictoryPanel active state

**Spec says:** `VictoryPanel (default inactive)`

**Reality:** `VictoryPanel.cs` subscribes to `KingsGameManager.OnGameComplete` in `OnEnable()`. If the VictoryPanel root GameObject is inactive at scene start, `OnEnable` never fires and the win screen never appears.

**Fix:** Keep **VictoryPanel root ACTIVE**. The visual content lives in the child `VictoryContent`, which `VictoryPanel.Awake()` sets inactive automatically (`_panel.SetActive(false)`). This is already reflected in §2 and §4.8.

---

### MISMATCH 2 — TutorialOverlay active state and TutorialController placement

**Spec says:** `TutorialOverlay (default inactive)` with the implication that TutorialController lives on TutorialOverlay.

**Reality:** `TutorialController.Awake()` wires `_nextButton.onClick` and `_skipButton.onClick`. If TutorialController is on an inactive GameObject, `Awake` never runs and button events are never registered. Placing TutorialController on TutorialOverlay then calling `SetActive(true)` later would trigger `Awake`, but `_overlayPanel.SetActive(false)` inside `Awake` would immediately hide it again — a one-frame flicker loop risk.

**Fix (used in this guide):** Place **TutorialController on GameManager** (always active). Assign `_overlayPanel` = Canvas → TutorialOverlay. `Awake()` hides TutorialOverlay immediately; `ShowTutorial()` re-activates it when KingsGameManager calls it after level load. TutorialOverlay can remain active in the hierarchy — it will be invisible until the tutorial starts.

---

### MISMATCH 3 — TimerText and MoveCountText are orphaned

**Spec says:** HUD contains `TimerText` and `MoveCountText`.

**Reality:** No script references these elements. `KingsGameManager` exposes `ElapsedSeconds` and `MoveCount` as public properties, but nothing polls them to drive UI text during play.

**Fix (deferred):** A `HudController` MonoBehaviour is needed. It would hold:
```csharp
[SerializeField] private KingsGameManager _gameManager;
[SerializeField] private TextMeshProUGUI  _timerText;
[SerializeField] private TextMeshProUGUI  _moveCountText;
```
and update them each frame in `Update()`. Until that script exists, leave `TimerText` and `MoveCountText` in the hierarchy but expect them to show no live values.

---

### MISMATCH 4 — Cell background sprite not needed

**Spec says:** "Cell background" is a required sprite asset.

**Reality:** `KingsGridRenderer.BuildCells()` creates `Image` components and sets `.color` directly from `RegionData.RegionColor`. No background sprite is sampled.

**Fix:** No cell background sprite is required. Region colors come from `LevelData` ScriptableObjects.

---

### MISMATCH 5 — Region border sprite not needed

**Spec says:** "Region border" is a required sprite asset.

**Reality:** `KingsGridRenderer.BuildBorders()` spawns thin `Image` GameObjects coloured by `_borderColor`. No sprite is used.

**Fix:** No border sprite is required. Adjust `_borderColor` and `_borderThickness` on KingsGridRenderer directly.

---

## 8. Play-mode Verification Checklist

Run these checks after wiring everything:

- [ ] **No missing reference warnings** in Console on scene load
- [ ] **Grid renders** immediately — a coloured N×N grid appears at scene centre
- [ ] **Tutorial overlay appears** on first play (PlayerPrefs key `Kings_TutorialSeen` absent)
- [ ] **Tutorial skips silently** on subsequent plays (key = 1)
- [ ] **Cell tap cycles** Empty → Dot → Crown → Empty
- [ ] **Undo button** starts greyed out; becomes enabled after first move
- [ ] **Restart button** prompts confirm panel only when MoveCount > 0
- [ ] **Tips button** shows hint text; 10-second cooldown prevents spam
- [ ] **Victory panel** (VictoryContent) animates in on win; star rating appears
- [ ] **Next Level button** loads the next LevelData without scene reload
- [ ] **PlayerPrefs** key `Kings_Level_1_Stars` is written after first win

# KingsSceneBuilder Manual Guide (Unity Manual Add/Edit)

Dokumen ini menjelaskan cara **membangun `SampleScene` secara manual dari Unity Editor** mengikuti perilaku `Assets/_Project/Editor/KingsSceneBuilder.cs`.

> Penting: alur resmi project tetap lewat menu **`BrainBattle -> Build Kings Scene`**. Manual setup dipakai untuk pemahaman/debug/emergency.

## 0) Scope dan aturan

- Scene target: `Assets/Scenes/SampleScene.unity`
- Jangan campur edit `LevelSelect` di task yang sama.
- Saat scene disimpan, **`VictoryContent` wajib inactive**.

---

## 1) Persiapan asset

Pastikan asset berikut ada:

- Sprites
  - `Assets/_Project/Resources/Sprites/victory_screen_bg.png`
  - `Assets/_Project/Resources/Sprites/dot.png`
  - `Assets/_Project/Resources/Sprites/crown.png` (pakai sub-sprite `crown_1`)
  - `Assets/_Project/Resources/Sprites/UIRoundedRect.png`
- Fonts
  - `Assets/_Project/Resources/Fonts/Outfit SDF.asset`
  - `Assets/_Project/Resources/Fonts/HUDIcons SDF.asset`
- Levels
  - Semua `LevelData` di `Assets/_Project/ScriptableObjects/Kings/Levels/`

Jika font/sprite belum siap, jalankan builder sekali untuk generate otomatis, lalu lanjut manual edit.

---

## 2) Root hierarchy (manual)

Di root scene, minimal harus ada:

- `Canvas`
- `GameManager`
- `SceneBootstrap`
- `EventSystem`

Jika sudah ada dan ingin rebuild manual, hapus root lama dulu agar wiring tidak tabrakan.

---

## 3) Build `Canvas`

Buat GameObject `Canvas`:

1. Tambah komponen:
   - `Canvas` (Render Mode: `Screen Space - Overlay`)
   - `CanvasScaler`
   - `GraphicRaycaster`
2. `CanvasScaler`:
   - UI Scale Mode: `Scale With Screen Size`
   - Reference Resolution: `1170 x 2532`
   - Match: `0.5`

---

## 4) Build `GameManager`

Buat GameObject `GameManager`, tambah komponen:

- `KingsGameManager`
- `LevelLoader`
- `TutorialController`

---

## 5) Build `SceneBootstrap`

Buat GameObject `SceneBootstrap`, tambah komponen:

- `KingsSceneBootstrap`

---

## 6) Build `EventSystem`

Pastikan ada `EventSystem`:

- Jika package Input System aktif, gunakan `InputSystemUIInputModule`.
- Jika tidak, gunakan `StandaloneInputModule`.
- Jangan punya dua input module sekaligus.

---

## 7) Canvas children (urutan dan detail)

Urutan child di bawah `Canvas`:

1. `Background`
2. `GridContainer`
3. `TimerBar`
4. `TutorialOverlay`
5. `TipsPanel`
6. `RestartConfirmPanel`
7. `HUD`
8. `VictoryPanel` (topmost)

### 7.1 `Background`

- Buat `UI/Image` bernama `Background`
- Stretch full canvas (`anchor min 0,0` / `anchor max 1,1`, offsets 0)
- Set sibling index ke `0`
- `Image`:
  - Type: `Simple`
  - Preserve Aspect: `false`
  - Color: `DesignSystem.Background`
  - Tanpa sprite background

### 7.2 `GridContainer`

- Buat `RectTransform` bernama `GridContainer`
- Tambah komponen `KingsGridRenderer`
- Anchors:
  - Min `(0,0)`, Max `(1,1)`, Pivot `(0.5,0.5)`
- Offsets:
  - `offsetMin = (0,120)`
  - `offsetMax = (0,0)`

### 7.3 `TimerBar`

- Buat `Image` bernama `TimerBar`
- Anchor top stretch:
  - Min `(0,1)`, Max `(1,1)`, Pivot `(0.5,1)`
  - Anchored Position `(0,0)`
  - Height `48` (`DesignSystem.TimerBarHeight`)
- Color: `DesignSystem.Background`

Child `TimerText`:
- `TextMeshProUGUI`
- Text awal: `00:00`
- Alignment: `Center`
- FontStyle: `Bold`
- FontSize: `44`
- Stretch full parent

### 7.4 `TutorialOverlay`

- Buat `Image` `TutorialOverlay`, stretch full
- Color: `DesignSystem.BorderRegion`
- **Biarkan active di scene** (runtime akan di-hide di `TutorialController.Awake`)

Children:
- `StepCounter` (TMP)
  - Text: `1 / 5`
  - Alignment: Center
  - Anchor: `(0.30,0.84)` ke `(0.70,0.92)`
- `StepText` (TMP)
  - Text: kosong
  - Alignment: Center
  - FontSize: `48`
  - Anchor: `(0.06,0.35)` ke `(0.94,0.78)`
- `SkipButton` (Button + Image + child `Label` TMP)
  - Label: `Skip`
  - Anchor: `(0.05,0.06)` ke `(0.32,0.14)`
- `NextButton` (Button + Image + child `Label` TMP)
  - Label: `Next`
  - Anchor: `(0.68,0.06)` ke `(0.95,0.14)`

### 7.5 `TipsPanel`

- Buat `Image` `TipsPanel`
- Anchor: `(0.08,0.28)` ke `(0.92,0.72)`
- Color: `RGBA(0.10, 0.10, 0.22, 0.97)`
- **Biarkan active di scene** (runtime di-hide `TipsButton.Awake`)

Children:
- `TipsText` (TMP)
  - Text: kosong
  - Alignment: TopLeft
  - FontSize: `36`
  - Anchor: `(0.05,0.20)` ke `(0.95,0.92)`
- `CloseButton` (Button + Label)
  - Label: `Close`
  - Anchor: `(0.35,0.04)` ke `(0.65,0.16)`

### 7.6 `RestartConfirmPanel`

- Buat `Image` `RestartConfirmPanel`
- Anchor: `(0.12,0.36)` ke `(0.88,0.64)`
- Color: `RGBA(0.10, 0.10, 0.22, 0.97)`
- **Biarkan active di scene** (runtime di-hide `RestartButton.Awake`)

Children:
- `PromptText` (TMP)
  - Text: `Restart this level?`
  - Alignment: Center
  - Anchor: `(0.05,0.56)` ke `(0.95,0.92)`
- `ConfirmButton` (Button + Label `Restart`)
  - Anchor: `(0.54,0.08)` ke `(0.93,0.46)`
- `CancelButton` (Button + Label `Cancel`)
  - Anchor: `(0.07,0.08)` ke `(0.46,0.46)`

### 7.7 `HUD`

- Buat `Image` `HUD`
- Tambah komponen `HUDController`
- Anchor bottom stretch:
  - Min `(0,0)`, Max `(1,0)`, Pivot `(0.5,0)`
  - `offsetMin = (0,0)`
  - `offsetMax = (0,120)`
- Color: `RGBA(0.05, 0.05, 0.10, 0.88)`

Child `TopBorder`:
- `Image`
- Anchor top stretch di HUD
- Height `1`
- Color `RGBA(1,1,1,0.08)`

Child `HUDRow`:
- `HorizontalLayoutGroup`
  - spacing `5`
  - childAlignment `MiddleCenter`
  - childControlWidth/Height `true`
  - childForceExpandWidth/Height `true`
  - padding left/right `8`, top/bottom `6`

Isi `HUDRow` (urut):
1. `UndoButton` (Image + Button + `UndoButton` script + Label)
2. `RestartButton` (Image + Button + `RestartButton` script + Label)
3. `MoveCounter` (Image + LayoutElement + `MoveCountText` TMP)
4. `MenuButton` (Image + Button + `MenuButton` script + Label)
5. `TipsButton` (Image + Button + `TipsButton` script + Label)

Catatan style HUD:
- Semua button HUD pakai sprite `UIRoundedRect.png` dengan `Image Type = Sliced`
- `LayoutElement.preferredWidth` button umum: `110`
- `MoveCounter` preferredWidth: `140`, flexibleWidth: `1.5`

### 7.8 `VictoryPanel`

- Buat root `VictoryPanel` (stretch full) + komponen `VictoryPanel` script
- Root **tetap active** supaya subscribe event jalan

Child `VictoryContent`:
- `Image`, stretch full
- Sprite: `victory_screen_bg.png`
- Set `SetActive(false)` di scene

Children `VictoryContent`:
- `Title` (TMP `VICTORY!`)
- `StarRatingText` (TMP)
- `TimeLabel`, `TimeText`
- `MovesLabel`, `MoveCountText`
- `MainMenuButton`
- `NextLevelButton`
- `RestartButton`

Tiga button victory pakai style pink (`DesignSystem.Primary` / #ff2d78).

---

## 8) Wiring SerializeField (manual drag-drop)

Lakukan drag-drop di Inspector persis berikut:

### `KingsGameManager` (GameManager)
- `_gridRenderer` -> `Canvas/GridContainer` (`KingsGridRenderer`)
- `_tutorialController` -> `GameManager` (`TutorialController`)

### `TutorialController` (GameManager)
- `_overlayPanel` -> `Canvas/TutorialOverlay`
- `_stepText` -> `TutorialOverlay/StepText`
- `_stepCounter` -> `TutorialOverlay/StepCounter`
- `_nextButton` -> `TutorialOverlay/NextButton`
- `_skipButton` -> `TutorialOverlay/SkipButton`

### `KingsSceneBootstrap` (SceneBootstrap)
- `_levelLoader` -> `GameManager` (`LevelLoader`)
- `_gameManager` -> `GameManager` (`KingsGameManager`)

### `LevelLoader` (GameManager)
- `_allLevels` -> isi semua `LevelData` dari folder levels (urut nama asset)

### `UndoButton` (HUD/UndoButton)
- `_gameManager` -> `GameManager` (`KingsGameManager`)
- `_button` -> `HUD/UndoButton` (`Button`)

### `RestartButton` (HUD/RestartButton)
- `_gameManager` -> `GameManager` (`KingsGameManager`)
- `_button` -> `HUD/RestartButton` (`Button`)
- `_confirmPanel` -> `Canvas/RestartConfirmPanel`

### `TipsButton` (HUD/TipsButton)
- `_gameManager` -> `GameManager` (`KingsGameManager`)
- `_button` -> `HUD/TipsButton` (`Button`)
- `_tipsPanel` -> `Canvas/TipsPanel`
- `_tipsText` -> `TipsPanel/TipsText`
- `_closeButton` -> `TipsPanel/CloseButton`

### `HUDController` (HUD)
- `_gameManager` -> `GameManager` (`KingsGameManager`)
- `_timerText` -> `TimerBar/TimerText`
- `_moveCountText` -> `HUD/MoveCounter/MoveCountText`

### `VictoryPanel` (Canvas/VictoryPanel)
- `_panel` -> `VictoryContent`
- `_hud` -> `Canvas/HUD`
- `_timeText` -> `VictoryContent/TimeText`
- `_moveCountText` -> `VictoryContent/MoveCountText`
- `_starRatingText` -> `VictoryContent/StarRatingText`
- `_nextLevelButton` -> `VictoryContent/NextLevelButton`
- `_restartButton` -> `VictoryContent/RestartButton`
- `_mainMenuButton` -> `VictoryContent/MainMenuButton`
- `_gameManager` -> `GameManager` (`KingsGameManager`)
- `_sceneBootstrap` -> `SceneBootstrap` (`KingsSceneBootstrap`)

---

## 9) Wiring Button events (manual)

Di `RestartConfirmPanel`:

- `ConfirmButton.onClick` -> object `HUD/RestartButton` -> method `RestartButton.ConfirmRestart()`
- `CancelButton.onClick` -> object `HUD/RestartButton` -> method `RestartButton.CancelRestart()`

Pastikan listener masuk sebagai persistent event di Inspector.

---

## 10) Sprite assignment detail

### `KingsGridRenderer` (`GridContainer`)
- `_dotSprite` -> `dot.png`
- `_crownSprite` -> pilih sub-sprite **`crown_1`** dari `crown.png`

### `LevelSelectButton` prefab (opsional sinkronisasi lintas scene)
Walau ini bukan SampleScene, builder juga mengisi sprite di prefab `Assets/_Project/Prefabs/LevelSelectButton.prefab`:
- `_spriteAvailable` -> `level_available.png`
- `_spriteCompleted` -> `level_completed.png`
- `_spriteActive` -> `level_active.png`
- `_spriteLocked` -> `level_lock.png`

---

## 11) Font assignment detail

Semua `TextMeshProUGUI` di `Canvas`:
- Font default -> `Outfit SDF.asset`

Khusus:
- `VictoryContent/StarRatingText` -> `HUDIcons SDF.asset`

Catatan Android:
- Kedua font harus `AtlasPopulationMode.Static`
- Font atlas harus sudah pre-baked
- Jangan ubah ke Dynamic

---

## 12) Checklist final sebelum save

1. Hierarchy sama seperti bagian 7.
2. Semua SerializeField di bagian 8 terisi.
3. `RestartConfirmPanel` button listeners terpasang.
4. `VictoryContent` inactive di scene.
5. EventSystem input module valid (satu saja).
6. Save scene.

---

## 13) Quick Checklist (1 halaman)

Gunakan checklist ini kalau mau setup cepat tanpa baca detail penuh.

### A. Root object

- [ ] Root ada: `Canvas`, `GameManager`, `SceneBootstrap`, `EventSystem`
- [ ] `EventSystem` pakai 1 input module saja (`InputSystemUIInputModule` **atau** `StandaloneInputModule`)

### B. Canvas setup

- [ ] `Canvas` -> Screen Space Overlay
- [ ] `CanvasScaler` -> `1170 x 2532`, Match `0.5`
- [ ] Child urutan: `Background`, `GridContainer`, `TimerBar`, `TutorialOverlay`, `TipsPanel`, `RestartConfirmPanel`, `HUD`, `VictoryPanel`

### C. Gameplay object

- [ ] `GameManager` punya: `KingsGameManager`, `LevelLoader`, `TutorialController`
- [ ] `SceneBootstrap` punya: `KingsSceneBootstrap`
- [ ] `GridContainer` punya: `KingsGridRenderer`

### D. Rect/layout penting

- [ ] `GridContainer` offsets: `offsetMin=(0,120)`, `offsetMax=(0,0)`
- [ ] `TimerBar` tinggi `48` (top stretch)
- [ ] `HUD` bottom stretch, tinggi `120`
- [ ] `VictoryContent` **inactive** saat save scene

### E. Wiring SerializeField

- [ ] `KingsGameManager`: `_gridRenderer`, `_tutorialController`
- [ ] `TutorialController`: `_overlayPanel`, `_stepText`, `_stepCounter`, `_nextButton`, `_skipButton`
- [ ] `KingsSceneBootstrap`: `_levelLoader`, `_gameManager`
- [ ] `LevelLoader`: `_allLevels` terisi semua asset `LevelData`
- [ ] `UndoButton`: `_gameManager`, `_button`
- [ ] `RestartButton`: `_gameManager`, `_button`, `_confirmPanel`
- [ ] `TipsButton`: `_gameManager`, `_button`, `_tipsPanel`, `_tipsText`, `_closeButton`
- [ ] `HUDController`: `_gameManager`, `_timerText`, `_moveCountText`
- [ ] `VictoryPanel`: `_panel`, `_hud`, `_timeText`, `_moveCountText`, `_starRatingText`, `_nextLevelButton`, `_restartButton`, `_mainMenuButton`, `_gameManager`, `_sceneBootstrap`

### F. Event wiring

- [ ] `ConfirmButton.onClick` -> `RestartButton.ConfirmRestart()`
- [ ] `CancelButton.onClick` -> `RestartButton.CancelRestart()`

### G. Asset assignment

- [ ] `KingsGridRenderer._dotSprite` -> `dot.png`
- [ ] `KingsGridRenderer._crownSprite` -> sub-sprite `crown_1`
- [ ] Semua TMP default font -> `Outfit SDF.asset`
- [ ] `VictoryContent/StarRatingText` font -> `HUDIcons SDF.asset`

### H. Final validation

- [ ] Review wiring dan hierarchy final
- [ ] Save scene

---

## 14) Troubleshooting cepat

- Tombol NRE saat klik -> ada field referensi null di script button.
- Crown kecil/salah sprite -> yang kepilih `crown_0`, ganti ke `crown_1`.
- Grid tidak muncul -> `_gridRenderer` di `KingsGameManager` belum terisi atau rect `GridContainer` salah.
- Victory langsung muncul saat start -> `VictoryContent` masih active saat save.
- Tap tidak terbaca -> cek `EventSystem` + input module.

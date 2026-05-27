# LevelSelectSceneBuilder Manual Guide (Unity Manual Add/Edit)

Dokumen ini menjelaskan cara **membangun `LevelSelect` secara manual dari Unity Editor** mengikuti perilaku `Assets/_Project/Editor/LevelSelectSceneBuilder.cs`.

> Penting: alur resmi project tetap lewat menu **`BrainBattle -> Build Level Select Scene`**. Manual setup dipakai untuk pemahaman/debug/emergency.

## 0) Scope dan aturan

- Scene target: `Assets/_Project/Scenes/LevelSelect.unity`
- Jangan campur edit `SampleScene` di task yang sama.
- Build order wajib:
  - index 0 -> `Assets/_Project/Scenes/LevelSelect.unity`
  - index 1 -> `Assets/Scenes/SampleScene.unity`
- Gunakan prefab: `Assets/_Project/Prefabs/LevelSelectButton.prefab`

---

## 1) Persiapan asset

Pastikan asset berikut ada:

- Sprites
  - `Assets/_Project/Resources/Sprites/level_available.png`
  - `Assets/_Project/Resources/Sprites/level_completed.png`
  - `Assets/_Project/Resources/Sprites/level_active.png`
  - `Assets/_Project/Resources/Sprites/level_lock.png`
- Prefab
  - `Assets/_Project/Prefabs/LevelSelectButton.prefab`
- Levels
  - Semua `LevelData` di `Assets/_Project/ScriptableObjects/Kings/Levels/`

Jika prefab/sprite belum siap, jalankan builder sekali untuk generate otomatis, lalu lanjut manual edit.

---

## 2) Root hierarchy (manual)

Di root scene, minimal harus ada:

- `Canvas`
- `LevelSelectController`
- `Main Camera`
- `EventSystem`

Jika sudah ada object lama dan ingin rebuild manual, hapus dulu agar wiring tidak tabrakan.

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

## 4) Build `LevelSelectController`

Buat GameObject `LevelSelectController`, tambah komponen:

- `LevelSelectController`

---

## 5) Build `Main Camera`

Buat GameObject `Main Camera`:

- Tag: `MainCamera`
- Komponen: `Camera`, `AudioListener`
- `Camera`:
  - Projection: `Orthographic`
  - Clear Flags: `Solid Color`
  - Background: `(0.06, 0.06, 0.10, 1)`
- Transform Position: `(0, 0, -10)`

---

## 6) Build `EventSystem`

Buat `EventSystem` baru:

- Komponen wajib: `EventSystem`
- Input module:
  - `InputSystemUIInputModule` jika package Input System tersedia
  - `StandaloneInputModule` jika tidak
- Pastikan hanya **satu** input module terpasang

---

## 7) Canvas children (urutan dan detail)

Urutan child di bawah `Canvas`:

1. `Background`
2. `Header`
3. `TabRow`
4. `ProgressRow`
5. `LevelScrollView`
6. `PlayButton`

### 7.1 `Background`

- Buat `UI/Image` bernama `Background`
- Stretch full canvas (`anchor min 0,0` / `anchor max 1,1`, offsets 0)
- `Image`:
  - Color: `(0.06, 0.06, 0.10, 1)`
  - Tanpa sprite background

### 7.2 `Header`

- Buat `Image` `Header`
- Anchor: `(0,0.90)` ke `(1,1.00)`
- Color: `(0.102, 0.102, 0.180, 0.97)`

Child `Title`:
- `TextMeshProUGUI`
- Text: `LEVEL SELECT`
- Alignment: `Center`
- FontSize: `86`
- FontStyle: `Bold`
- Stretch full parent

### 7.3 `TabRow`

- Buat `TabRow`
- Anchor: `(0,0.82)` ke `(1,0.90)`

Isi 3 tab flush (tanpa gap):
1. `BeginnerTab`
2. `ExpertTab`
3. `ImpossibleTab`

Setiap tab:
- Komponen: `Image`, `Button`, child `Label` (TMP)
- Label center full stretch
- FontSize: `45`
- Tab default active: `BeginnerTab`
  - background: `(1.00, 0.176, 0.471, 1)`
  - label: putih + bold
- Tab inactive:
  - background: `(0.165, 0.165, 0.243, 1)`
  - label: `(0.533, 0.533, 0.533, 1)`

### 7.4 `ProgressRow`

- Buat `ProgressRow`
- Anchor: `(0,0.775)` ke `(1,0.820)`

Isi 3 group:
1. `ProgressGroup0`
2. `ProgressGroup1`
3. `ProgressGroup2`

Per group:
- Root group anchor per 1/3 area (dengan margin horizontal kecil)
- Child `Track`:
  - tinggi `7`
  - center vertical
  - lebar 80% group
  - color `(0.165, 0.165, 0.243, 1)`
- Child `Fill` (di dalam `Track`):
  - `Image.Type = Filled`
  - `FillMethod = Horizontal`
  - `FillAmount = 0`
  - color accent pink
- Child `PctText{i}`:
  - anchor `(0.82,0)` ke `(1,1)`
  - Alignment: `MidlineRight`
  - FontSize: `33`
  - Text awal `0%`

### 7.5 `LevelScrollView`

Root `LevelScrollView`:
- Anchor: `(0.03,0.06)` ke `(0.97,0.775)`
- `Image` transparent
- Komponen `ScrollRect`:
  - `horizontal = false`
  - `vertical = true`
  - `scrollSensitivity = 36`

Child `Viewport`:
- Stretch full
- `Image.color = white`
- Tambah `Mask` (`showMaskGraphic = false`)

Child `Content` (di dalam Viewport):
- Anchor min `(0,1)`, max `(1,1)`, pivot `(0.5,1)`
- Tambah `GridLayoutGroup`:
  - padding `19,19,19,19`
  - cellSize `359 x 359`
  - spacing `14 x 14`
  - constraint `FixedColumnCount = 3`
- Tambah `ContentSizeFitter`:
  - vertical `PreferredSize`
  - horizontal `Unconstrained`

Koneksi `ScrollRect`:
- `viewport` -> `Viewport` RectTransform
- `content` -> `Content` RectTransform

### 7.6 `PlayButton`

- Buat button `PlayButton`
- Anchor min `(0,0)`, max `(1,0)`, pivot `(0.5,0)`
- Offsets:
  - `offsetMin = (19,19)`
  - `offsetMax = (-19,95)` (tinggi 76)
- Label: `PLAY`
  - FontSize `62`
  - FontStyle `Bold`
- Style pink (background accent)

---

## 8) Wiring SerializeField (manual drag-drop)

Lakukan drag-drop di Inspector persis berikut:

### `LevelSelectController` (root `LevelSelectController`)

- `_tabButtons[0]` -> `Canvas/TabRow/BeginnerTab` (`Button`)
- `_tabButtons[1]` -> `Canvas/TabRow/ExpertTab` (`Button`)
- `_tabButtons[2]` -> `Canvas/TabRow/ImpossibleTab` (`Button`)

- `_progressFills[0]` -> `Canvas/ProgressRow/ProgressGroup0/Track/Fill` (`Image`)
- `_progressFills[1]` -> `Canvas/ProgressRow/ProgressGroup1/Track/Fill` (`Image`)
- `_progressFills[2]` -> `Canvas/ProgressRow/ProgressGroup2/Track/Fill` (`Image`)

- `_progressTexts[0]` -> `Canvas/ProgressRow/ProgressGroup0/PctText0` (`TextMeshProUGUI`)
- `_progressTexts[1]` -> `Canvas/ProgressRow/ProgressGroup1/PctText1` (`TextMeshProUGUI`)
- `_progressTexts[2]` -> `Canvas/ProgressRow/ProgressGroup2/PctText2` (`TextMeshProUGUI`)

- `_gridContent` -> `Canvas/LevelScrollView/Viewport/Content` (`Transform`)
- `_levelButtonPrefab` -> `Assets/_Project/Prefabs/LevelSelectButton.prefab`
- `_playButton` -> `Canvas/PlayButton` (`Button`)
- `_allLevels` -> isi semua `LevelData` dari folder levels (urut nama asset)

---

## 9) Wiring Button events (manual)

Tidak perlu wiring `OnClick` manual di Inspector untuk tab/play di scene ini.
`LevelSelectController` mengelola binding button secara runtime dari referensi SerializeField.

---

## 10) Sprite assignment detail

### `Background`
- Tanpa sprite pada `Canvas/Background`

### `LevelSelectButton` prefab
Pada `Assets/_Project/Prefabs/LevelSelectButton.prefab`, field sprite wajib:
- `_spriteAvailable` -> `level_available.png`
- `_spriteCompleted` -> `level_completed.png`
- `_spriteActive` -> `level_active.png`
- `_spriteLocked` -> `level_lock.png`

---

## 11) Build Settings (wajib)

Atur `File -> Build Settings`:

1. Index 0: `Assets/_Project/Scenes/LevelSelect.unity`
2. Index 1: `Assets/Scenes/SampleScene.unity`

Hapus entry lama `LevelSelect`/`SampleScene` yang duplikat agar urutan bersih.

---

## 12) Checklist final sebelum save

1. Hierarchy sama seperti bagian 7.
2. Semua SerializeField di bagian 8 terisi.
3. `LevelScrollView` terhubung benar (`viewport` dan `content`).
4. Prefab `LevelSelectButton` sudah berisi 4 sprite state.
5. Build settings urutannya benar (`LevelSelect=0`, `SampleScene=1`).
6. Save scene.

---

## 13) Quick Checklist (1 halaman)

Gunakan checklist ini kalau mau setup cepat tanpa baca detail penuh.

### A. Root object

- [ ] Root ada: `Canvas`, `LevelSelectController`, `Main Camera`, `EventSystem`
- [ ] `Main Camera` orthographic + `AudioListener`
- [ ] `EventSystem` pakai 1 input module saja

### B. Canvas setup

- [ ] `Canvas` -> Screen Space Overlay
- [ ] `CanvasScaler` -> `1170 x 2532`, Match `0.5`
- [ ] Child urutan: `Background`, `Header`, `TabRow`, `ProgressRow`, `LevelScrollView`, `PlayButton`

### C. ScrollView

- [ ] `ScrollRect` vertical only
- [ ] `Viewport` punya `Mask(showMaskGraphic=false)`
- [ ] `Viewport` image putih
- [ ] `Content` pakai GridLayout 3 kolom, cell `359x359`, gap `14`, padding `19`

### D. Prefab dan sprite

- [ ] Prefab `LevelSelectButton.prefab` ada
- [ ] Child prefab: `SpriteImage`, `LevelLabel`, `Checkmark`, `LockOverlay`
- [ ] 4 sprite state prefab sudah terassign

### E. Wiring controller

- [ ] `_tabButtons`, `_progressFills`, `_progressTexts` terisi
- [ ] `_gridContent`, `_levelButtonPrefab`, `_playButton` terisi
- [ ] `_allLevels` terisi semua `LevelData`

### F. Build settings

- [ ] `LevelSelect` di index 0
- [ ] `SampleScene` di index 1

---

## 14) Troubleshooting cepat

- Tab tidak responsif -> cek `EventSystem` atau `_tabButtons` belum terisi.
- Grid kosong -> `_allLevels` kosong atau `_levelButtonPrefab` belum assign.
- Scroll tidak terklip -> `Viewport` tidak putih / `Mask` belum ada.
- State level tidak berubah benar -> 4 sprite state di prefab belum terisi.
- Build mulai dari scene salah -> urutan build settings tidak `LevelSelect=0`, `SampleScene=1`.

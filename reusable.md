# Brain Battle Reusable Components

Dokumen ini merangkum bagian project yang paling layak dipakai ulang untuk game berikutnya, plus cara pakainya.

## Ringkasan cepat

Yang paling reusable untuk game puzzle berikutnya:

1. Design system token terpusat (`Assets/_Project/Scripts/Shared/UI/BrainBattleDesignSystem.cs:5`)
2. Struktur data grid/region + validator murni (non-MonoBehaviour)
3. Pipeline level data (`LevelData` + `LevelLoader`)
4. UX shell umum (LevelSelect, VictoryPanel, tombol HUD)
5. Audio singleton lintas scene (`AudioManager`)
6. Editor tooling pattern (scene builder, validator, setup tool)

---

## 1) UI Design System (langsung reusable)

### Komponen
- `Assets/_Project/Scripts/Shared/UI/BrainBattleDesignSystem.cs:5`

### Kenapa reusable
- Semua token warna/spacing/typography/sizing sudah dipusatkan.
- Mengurangi hardcode nilai UI di script game berikutnya.

### Cara pakai di game berikutnya
- Tetap pakai namespace `BrainBattle.Shared.UI`.
- Di UI script baru, ambil token langsung dari `DesignSystem.*` saat runtime.
- Jika game baru perlu brand berbeda, ubah isi token, bukan hardcode di script UI.

### Batasan
- Token grid seperti `CrownSizeRatio` atau `DotSizeRatio` bersifat puzzle-specific; untuk game baru bisa ganti nama token baru.

---

## 2) Core Grid Domain (sangat reusable untuk puzzle berbasis grid)

### Komponen
- `Assets/_Project/Scripts/Core/Models/GridData.cs:12`
- `Assets/_Project/Scripts/Core/Models/CellData.cs`
- `Assets/_Project/Scripts/Core/Models/CellState.cs`
- `Assets/_Project/Scripts/Core/Models/RegionData.cs`
- `Assets/_Project/Scripts/Core/Engine/ConstraintValidator.cs:30`

### Kenapa reusable
- `GridData` sudah menangani runtime model + serialisasi flatten 2D array.
- `ConstraintValidator` pure logic (tidak tergantung scene/UI).

### Cara pakai di game berikutnya
- Pertahankan model `GridData`/`RegionData` sebagai fondasi papan.
- Untuk rules baru, jangan modifikasi validator Kings langsung.
  - Buat validator game baru, mis. `SudokuConstraintValidator` atau `NonogramConstraintValidator`.
- Tetap konsisten konvensi posisi: `Vector2Int(x=col, y=row)`.

### Batasan
- `ConstraintValidator` saat ini mengandung aturan Kings (row/col/region + adjacency), jadi perlu diganti/dipecah per game.

---

## 3) Level Data Pipeline (reusable dengan sedikit adaptasi)

### Komponen
- `Assets/_Project/Scripts/Games/Kings/Data/LevelData.cs:7`
- `Assets/_Project/Scripts/Games/Kings/Data/LevelLoader.cs:7`
- `Assets/_Project/Scripts/Core/Generators/LevelGeneratorService.cs:8`

### Kenapa reusable
- Pattern ScriptableObject level + loader ke `GridData` sudah rapi.
- Loader sudah punya utility progression (`GetNextLevelNumberInDifficulty`).

### Cara pakai di game berikutnya
- Paling aman: duplikasi `LevelData` ke tipe baru per game (mis. `SudokuLevelData`) ketimbang mencampur schema.
- Reuse alur:
  1. `LevelData` asset list di-loader oleh `LevelLoader`
  2. `BuildGridFromLevel` menghasilkan model runtime
  3. bootstrap kirim ke game manager
- Kalau butuh metadata baru (target score, hint budget), tambahkan di `LevelData` baru.

### Batasan
- `LevelData` saat ini menaruh `Solution` & region schema khusus Kings.

---

## 4) Shared UI Shell (reusable sebagian besar)

### Komponen
- `Assets/_Project/Scripts/Shared/UI/LevelSelectController.cs:12`
- `Assets/_Project/Scripts/Shared/UI/LevelSelectButton.cs:16`
- `Assets/_Project/Scripts/Shared/UI/VictoryPanel.cs:10`
- `Assets/_Project/Scripts/Shared/UI/MainMenuController.cs:11`
- `Assets/_Project/Scripts/Shared/UI/UndoButton.cs:7`
- `Assets/_Project/Scripts/Shared/UI/RestartButton.cs:7`
- `Assets/_Project/Scripts/Shared/UI/TipsButton.cs:8`

### Kenapa reusable
- Pola flow scene dan panel sudah matang: LevelSelect → Gameplay → Victory → Next/Menu.
- Banyak elemen hanya butuh ganti binding data dan label.

### Cara pakai di game berikutnya
- Reuse struktur prefab/layout + event flow.
- Pisahkan yang Kings-specific:
  - key `PlayerPrefs` (`Kings_*`)
  - scene name (`SampleScene`)
  - perhitungan stars/hints rule.
- Untuk game baru, buat wrapper baru (mis. `Puzzle2VictoryPanel`) yang tetap meniru lifecycle & animation pattern.

### Batasan
- Banyak literal/key bertema Kings, jadi jangan dipakai mentah tanpa rename dan mapping ulang.

---

## 5) Audio Framework (langsung reusable)

### Komponen
- `Assets/_Project/Scripts/Shared/Audio/AudioManager.cs:7`
- `Assets/_Project/Scripts/Shared/Audio/SFXType.cs`
- `Assets/_Project/Editor/AudioManagerSetup.cs:7`

### Kenapa reusable
- Singleton `DontDestroyOnLoad`, SFX pool, BGM control, volume/mute persistence sudah siap produksi.

### Cara pakai di game berikutnya
- Pertahankan `AudioManager` sebagai layanan global.
- Tambah/ubah clip map dan API `PlayXxx()` sesuai event game baru.
- Tetap pakai startup pattern saat ini:
  - AutoCreate `BeforeSceneLoad`
  - AutoStartBGM `AfterSceneLoad`
  - `sceneLoaded` untuk transisi berikutnya.

### Batasan
- Path resource clip saat ini hardcoded ke set audio Kings.

---

## 6) Editor Automation Pattern (highly reusable)

### Komponen
- `Assets/_Project/Editor/KingsSceneBuilder.cs:18`
- `Assets/_Project/Editor/LevelSelectSceneBuilder.cs:19`
- `Assets/_Project/Editor/KingsSceneValidator.cs:19`
- `Assets/_Project/Editor/KingsLevelGenerator.cs`
- `Assets/_Project/Editor/KingsLevelGeneratorWizard.cs`

### Kenapa reusable
- Builder + Validator mengurangi human error wiring scene.
- Cocok untuk semua game yang punya banyak SerializeField antar komponen UI.

### Cara pakai di game berikutnya
- Buat pasangan tool per game:
  - `Puzzle2SceneBuilder`
  - `Puzzle2SceneValidator`
- Simpan prinsip yang sama:
  - scene dibangun dari code
  - wiring otomatis
  - validator wajib sebelum build.

### Batasan
- Nama GO, prefab path, dan wiring map sekarang Kings-specific.

---

## Kandidat yang sebaiknya TIDAK direuse mentah

1. `KingsGameManager` (`Assets/_Project/Scripts/Games/Kings/Logic/KingsGameManager.cs:12`)
   - Reuse pola arsitektur (event, undo, timer), bukan logic langsung.
2. `KingsGridRenderer` (`Assets/_Project/Scripts/Games/Kings/UI/KingsGridRenderer.cs`)
   - Banyak behavior visual khusus crown/dot/region.
3. Solver/generator Kings (`KingsNQueensSolver`, `KingsRegionBuilder`, `KingsUniquenessVerifier`)
   - Khusus puzzle Queens/Kings.

---

## Template implementasi cepat untuk game berikutnya

Urutan paling aman:

1. Clone shared foundation
   - pakai `DesignSystem`, `AudioManager`, model grid dasar.
2. Buat data schema game baru
   - `NewGameLevelData` + `NewGameLevelLoader`.
3. Buat logic core game baru
   - validator + game manager baru (jangan edit Kings manager).
4. Adapt UI shell
   - clone `LevelSelectController`, `VictoryPanel`, tombol HUD dengan key/scene baru.
5. Buat editor builder + validator baru
   - agar wiring tetap auto dan konsisten.

---

## Checklist reusable extraction

- [ ] Tidak ada string `Kings_` yang tersisa di modul generic.
- [ ] Scene name tidak hardcoded ke `SampleScene`/`LevelSelect` tanpa config.
- [ ] PlayerPrefs key dipisahkan per game.
- [ ] Semua warna/size dari `DesignSystem`.
- [ ] Game-specific rules hanya ada di namespace game tersebut.
- [ ] Scene wiring diverifikasi lewat validator tool.

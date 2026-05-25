# Reusable Extraction Plan (Brain Battle)

Tujuan: mengekstrak komponen reusable untuk game berikutnya tanpa merusak game Kings yang sudah jalan.

## Prinsip utama

1. **Stabilitas dulu**: jangan memindahkan file Kings langsung; lakukan via adapter/wrapper.
2. **Extract by capability**: UI token, audio, progression shell, core grid model.
3. **No big-bang refactor**: lakukan bertahap, tiap fase harus playable.
4. **Backward compatible sementara**: namespace/path lama tetap hidup sampai fase cutover.

---

## Kandidat extraction prioritas

### P0 — Reuse langsung (minim perubahan)
- `Assets/_Project/Scripts/Shared/UI/BrainBattleDesignSystem.cs`
- `Assets/_Project/Scripts/Shared/Audio/AudioManager.cs`
- `Assets/_Project/Scripts/Shared/Audio/SFXType.cs`
- Pattern editor setup: `Assets/_Project/Editor/AudioManagerSetup.cs`

### P1 — Reuse dengan adapter
- `Assets/_Project/Scripts/Core/Models/GridData.cs`
- `Assets/_Project/Scripts/Core/Models/CellData.cs`
- `Assets/_Project/Scripts/Core/Models/CellState.cs`
- `Assets/_Project/Scripts/Core/Models/RegionData.cs`
- `Assets/_Project/Scripts/Games/Kings/Data/LevelLoader.cs`
- `Assets/_Project/Scripts/Shared/UI/LevelSelectController.cs`
- `Assets/_Project/Scripts/Shared/UI/LevelSelectButton.cs`
- `Assets/_Project/Scripts/Shared/UI/VictoryPanel.cs`

### P2 — Jangan extract mentah
- `Assets/_Project/Scripts/Games/Kings/Logic/KingsGameManager.cs`
- `Assets/_Project/Scripts/Games/Kings/UI/KingsGridRenderer.cs`
- `Assets/_Project/Scripts/Core/Generators/KingsNQueensSolver.cs`
- `Assets/_Project/Scripts/Core/Generators/KingsRegionBuilder.cs`
- `Assets/_Project/Scripts/Core/Generators/KingsUniquenessVerifier.cs`

---

## Target struktur reusable

```text
Assets/_Project/Scripts/
  Shared/
    Foundation/
      UI/            (Design tokens, UI constants)
      Audio/         (AudioManager, SFX abstractions)
      Progression/   (Level select shell, progress key policy)
      SceneFlow/     (menu→gameplay→victory flow helpers)
  Games/
    Kings/
    Game2/
```

> Catatan: folder/nama bisa disesuaikan, tapi boundary capability harus tetap.

---

## Fase implementasi

## Fase 0 — Baseline & guardrails

### Langkah
1. Bekukan kontrak perilaku Kings (scene flow, key PlayerPrefs, audio behavior).
2. Dokumentasikan dependency lintas script dari komponen target.
3. Tambahkan checklist regression manual untuk:
   - LevelSelect load
   - start level
   - win → victory panel
   - next level/menu/restart
   - audio BGM + SFX.

### Done criteria
- Semua behavior baseline tertulis dan bisa dicek ulang sebelum/after refactor.

---

## Fase 1 — UI foundation extraction

### Langkah
1. Jadikan `DesignSystem` sebagai foundation contract.
2. Pisahkan token generic vs Kings-specific token.
3. Tambah naming policy token agar game2 tidak memakai istilah Kings.

### Done criteria
- Script shared/game baru bisa consume token tanpa menambah hardcoded hex/size.

---

## Fase 2 — Audio extraction

### Langkah
1. Pertahankan `AudioManager` sebagai service global lintas game.
2. Tambah layer mapping event suara per game (mis. key enum/table), jangan hardcode path clip di logic game.
3. Biarkan API lama (`PlayTap`, dst) tetap ada sementara untuk Kings.

### Done criteria
- Kings tetap jalan tanpa perubahan behavior audio.
- Game baru bisa registrasi clip map sendiri tanpa edit core manager.

---

## Fase 3 — Progression & UI shell extraction

### Langkah
1. Extract logika umum LevelSelect:
   - tab state
   - progress fill
   - availability/locked/completed model.
2. Bungkus detail game-specific:
   - `PlayerPrefs` key prefix (`Kings_`)
   - scene name (`SampleScene`, `LevelSelect`)
   - star rating rules.
3. Terapkan config object (per game) untuk mengganti detail di atas.

### Done criteria
- `LevelSelectController` dan `VictoryPanel` bisa dipakai game2 lewat config, bukan copy penuh.

---

## Fase 4 — Level data pipeline extraction

### Langkah
1. Definisikan kontrak level data generic (grid size, id, difficulty, metadata).
2. Pertahankan `LevelData` Kings saat ini, lalu tambahkan level data untuk game2 terpisah.
3. Buat `ILevelLoader` atau pola loader sejenis agar bootstrap tidak tergantung kelas Kings.

### Done criteria
- Kings loader existing tetap bekerja.
- Game2 bisa load level tanpa menyentuh class Kings.

---

## Fase 5 — Editor tooling reuse

### Langkah
1. Pertahankan pattern `Builder + Validator` per game.
2. Extract helper generic (create canvas, event system, TMP wiring helper).
3. Implement builder/validator khusus game2 di atas helper generic.

### Done criteria
- Game2 punya SceneBuilder + SceneValidator sendiri.
- Tidak ada wiring manual untuk SerializeField kritikal.

---

## Strategi cutover aman

1. **Copy-first, switch-later**: buat reusable module baru dulu.
2. Pindahkan pemakaian Kings sedikit demi sedikit ke module baru.
3. Setiap batch perubahan harus lolos regression checklist.
4. Hapus code lama hanya setelah 100% callsite pindah.

---

## Risk register

1. **Hardcoded key/scene name bocor ke module reusable**  
   Mitigasi: semua key/scene masuk config per game.

2. **Refactor memutus wiring scene**  
   Mitigasi: builder + validator wajib dijalankan setiap fase.

3. **Audio behavior berubah diam-diam**  
   Mitigasi: checklist BGM/SFX di baseline sebelum merge.

4. **Scope creep (terlalu banyak generic abstraction)**  
   Mitigasi: extract hanya yang dipakai minimal 2 game.

---

## Deliverables akhir

1. `reusable.md` (inventaris reusable + batasan)
2. `reusable_extraction_plan.md` (dokumen ini)
3. Shared modules hasil extraction (foundation/audio/progression)
4. Builder + validator game2 berbasis pattern yang sama
5. Regression checklist lintas game

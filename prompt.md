# Prompt Claude Code — Task Terpisah

## Prompt 1 — Kerjakan Task 1 saja

```txt
Kerjakan HANYA Task 1 dari file `/Users/bytedance/Documents/go/Brain-Battle/design_levelselect.md`.

Context:
- Unity project: Brain-Battle
- Scene target: HANYA `LevelSelect`
- Jangan sentuh `SampleScene`
- Fokus visual/layout saja, jangan ubah game logic

Wajib patuhi aturan project:
1) Pakai token DesignSystem (`Assets/_Project/Scripts/Shared/UI/BrainBattleDesignSystem.cs`) untuk color/size/spacing yang sudah tersedia.
2) Jangan hardcode hex/pixel jika token sudah ada.
3) Jika perlu value baru yang dipakai >=2 tempat / branding-critical, tambahkan token baru dulu di DesignSystem lalu pakai token itu.
4) Jika menambah SerializeField pada script yang wired, update wiring di `LevelSelectSceneBuilder` lalu rebuild scene via `BrainBattle -> Build Level Select Scene`.
5) Jangan ubah scene lain.
6) Setelah selesai, update `Assets/_Project/Documentation/Milestones.md`.

Scope Task 1 (Baseline scene & layout root):
- Validasi/pastikan Canvas Scaler LevelSelect: reference 1170x2532, match 0.5.
- Rapikan root layout/safe-area supaya elemen utama tidak overlap notch/home indicator.
- Pastikan layout portrait stabil.

Langkah kerja:
- Baca dulu file terkait sebelum edit:
  - `/Users/bytedance/Documents/go/Brain-Battle/design_levelselect.md`
  - `/Users/bytedance/Documents/go/Brain-Battle/CLAUDE.md`
  - script/builder LevelSelect yang relevan
- Lakukan perubahan minimal sesuai scope Task 1.
- Jangan kerjakan Task 2 di prompt ini.

Output yang saya butuhkan:
- Daftar file yang diubah
- Ringkasan perubahan per file
- Checklist acceptance Task 1 (pass/fail)
- Catatan blocker (kalau ada)
```

## Prompt 2 — Kerjakan Task 2 saja

```txt
Kerjakan HANYA Task 2 dari file `/Users/bytedance/Documents/go/Brain-Battle/design_levelselect.md`.

Context:
- Unity project: Brain-Battle
- Scene target: HANYA `LevelSelect`
- Jangan sentuh `SampleScene`
- Fokus visual/layout saja, jangan ubah game logic

Wajib patuhi aturan project:
1) Pakai token DesignSystem (`Assets/_Project/Scripts/Shared/UI/BrainBattleDesignSystem.cs`) untuk color/size/spacing yang sudah tersedia.
2) Jangan hardcode hex/pixel jika token sudah ada.
3) Jika perlu value baru yang dipakai >=2 tempat / branding-critical, tambahkan token baru dulu di DesignSystem lalu pakai token itu.
4) Jika menambah SerializeField pada script yang wired, update wiring di `LevelSelectSceneBuilder` lalu rebuild scene via `BrainBattle -> Build Level Select Scene`.
5) Jangan ubah scene lain.
6) Setelah selesai, update `Assets/_Project/Documentation/Milestones.md`.

Scope Task 2 (Header section):
- Set eyebrow text `BRAIN BATTLE` dengan target visual 18.
- Set title `Level Select` dengan target visual 42 bold.
- Rapikan spacing vertikal eyebrow ↔ title ↔ tab row agar proporsional sesuai referensi desain.

Langkah kerja:
- Baca dulu file terkait sebelum edit:
  - `/Users/bytedance/Documents/go/Brain-Battle/design_levelselect.md`
  - `/Users/bytedance/Documents/go/Brain-Battle/CLAUDE.md`
  - script/builder LevelSelect yang relevan
- Lakukan perubahan minimal sesuai scope Task 2.
- Jangan kerjakan Task 1 di prompt ini.

Output yang saya butuhkan:
- Daftar file yang diubah
- Ringkasan perubahan per file
- Checklist acceptance Task 2 (pass/fail)
- Catatan blocker (kalau ada)
```

# Prompt Claude Code — LevelSelect Task 3 sampai 10 (Terpisah)

Gunakan prompt di bawah **satu per satu** (jangan digabung), agar eksekusi fokus, mudah diverifikasi, dan minim bug.

---

## Prompt Task 3 — Segmented tabs (Beginner / Expert / Impossible)

```txt
Kerjakan HANYA Task 3 dari `/Users/bytedance/Documents/go/Brain-Battle/design_levelselect.md`.

Tujuan task:
Merapikan segmented tabs Beginner/Expert/Impossible agar tinggi visual target, state aktif/inaktif jelas, dan switching tab tetap mulus tanpa glitch.

Scope ketat:
- Scene target: HANYA `Assets/_Project/Scenes/LevelSelect.unity`
- Jangan sentuh `SampleScene`
- Jangan ubah game rules/logic unlock level
- Fokus pada visual state tab + kestabilan tab switching

File yang WAJIB dibaca dulu:
1) `/Users/bytedance/Documents/go/Brain-Battle/design_levelselect.md`
2) `/Users/bytedance/Documents/go/Brain-Battle/CLAUDE.md`
3) `Assets/_Project/Scripts/Shared/UI/LevelSelectController.cs`
4) `Assets/_Project/Editor/LevelSelectSceneBuilder.cs`
5) `Assets/_Project/Scripts/Shared/UI/BrainBattleDesignSystem.cs`

Aturan wajib project:
- Semua color/size/spacing yang sudah punya token harus pakai `DesignSystem`.
- Dilarang hardcode hex/pixel jika token sudah ada.
- Jika butuh nilai baru yang dipakai >=2 tempat atau branding-critical, tambahkan token baru ke `BrainBattleDesignSystem.cs` dulu.
- Jika menambah SerializeField pada script wired, wajib update wiring di `LevelSelectSceneBuilder.cs` lalu rebuild via menu builder.
- Jangan manual wiring scene sebagai final fix.

Implementasi detail yang diminta:
1) Segmented container:
   - Pastikan TabRow tetap model segmented 3 bagian setara lebar.
   - Tiap tab tetap clickable penuh area.
2) Tinggi tab target 72:
   - Prioritaskan token DesignSystem. Jika belum ada token representatif, tambah token baru (mis: `LevelSelectTabHeight = 72f`) dan gunakan konsisten di builder/layout yang relevan.
3) Typography tab:
   - Font target 18 (gunakan token yang sudah sesuai; jika belum cocok dan dipakai berulang, tambahkan token).
   - Active: teks lebih tegas (bold) + kontras tinggi.
   - Inactive: teks low emphasis namun tetap terbaca.
4) State visual:
   - Active = pink fill (`DesignSystem.Primary`).
   - Inactive = dark surface (`DesignSystem.Surface` atau token setara).
5) Behavior switching:
   - Klik tab hanya mengganti difficulty aktif dan refresh grid/progress yang sudah ada.
   - Jangan ubah rule pemilihan level / unlock.
   - Pastikan tidak ada visual flicker/glitch akibat update style berulang.

Batasan anti-bug:
- Jangan mengubah signature public API yang dipakai scene builder kecuali benar-benar perlu.
- Jangan ubah alur `SelectTab -> RefreshProgress -> RebuildGrid` selain yang diperlukan task ini.
- Jangan tambahkan fitur di luar task.

Verifikasi wajib:
- Tab active/inactive terlihat jelas secara visual.
- Klik Beginner/Expert/Impossible berpindah mulus, grid ter-refresh benar.
- Tidak ada NullReferenceException dari wiring.

Setelah coding:
1) Update `Assets/_Project/Documentation/Milestones.md` (tambah bullet progress untuk Task 3).
2) Berikan output:
   - Daftar file yang diubah
   - Ringkasan perubahan per file
   - Checklist acceptance Task 3 (pass/fail)
   - Risiko/regresi yang perlu dipantau
```

---

## Prompt Task 4 — Progress row per difficulty

```txt
Kerjakan HANYA Task 4 dari `/Users/bytedance/Documents/go/Brain-Battle/design_levelselect.md`.

Tujuan task:
Merapikan progress row agar label progress difficulty aktif, progress bar tinggi 6, dan persentase sinkron dengan data aktual saat tab berganti.

Scope ketat:
- Scene target: `LevelSelect` saja
- Fokus visual + data binding progress
- Jangan ubah logic unlock level

File yang WAJIB dibaca dulu:
1) `/Users/bytedance/Documents/go/Brain-Battle/design_levelselect.md`
2) `Assets/_Project/Scripts/Shared/UI/LevelSelectController.cs`
3) `Assets/_Project/Editor/LevelSelectSceneBuilder.cs`
4) `Assets/_Project/Scripts/Shared/UI/BrainBattleDesignSystem.cs`
5) `/Users/bytedance/Documents/go/Brain-Battle/CLAUDE.md`

Implementasi detail:
1) Label progress difficulty aktif:
   - Tampilkan label konteks difficulty aktif (contoh: "Impossible progress") pada row progress.
   - Jika perlu elemen UI baru, tambahkan lewat `LevelSelectSceneBuilder` (bukan manual scene fix).
   - Wiring field baru wajib dimasukkan ke builder.
2) Progress bar:
   - Tinggi track/fill = 6 menggunakan token (`ProgressBarHeight`) jika tersedia.
   - Fill pakai `DesignSystem.Primary`; track pakai token surface/dark yang tepat.
3) Persentase:
   - Teks % di kanan tampil untuk difficulty terkait.
   - Nilai harus berasal dari data aktual completion per tab (stars > 0).
4) Sinkronisasi tab:
   - Saat switch tab, label + persen + fill amount update konsisten.
   - Hindari mismatch visual saat frame transisi.

Aturan tokenisasi:
- Hindari angka magic di script UI untuk style yang bisa ditokenisasi.
- Jika nilai baru dipakai berulang (misal ukuran teks label progress), tambahkan token.

Verifikasi wajib:
- Tiap tab menunjukkan progress yang benar.
- Progress bar dan teks % sinkron.
- Tidak ada null reference dari field baru.

Setelah coding:
1) Update `Assets/_Project/Documentation/Milestones.md` untuk Task 4.
2) Berikan output:
   - File changed
   - Ringkasan perubahan
   - Acceptance checklist Task 4
   - Catatan potensi regresi
```

---

## Prompt Task 5 — Level cell visual states (Completed / Current / Locked)

```txt
Kerjakan HANYA Task 5 dari `/Users/bytedance/Documents/go/Brain-Battle/design_levelselect.md`.

Tujuan task:
Memfinalkan visual state cell level (Completed/Available/Locked) dengan hierarki visual jelas dalam <=1 detik dipahami pemain.

Scope ketat:
- LevelSelect only
- Fokus visual komponen `LevelSelectButton`
- Jangan ubah aturan state unlock/completion

File yang WAJIB dibaca dulu:
1) `/Users/bytedance/Documents/go/Brain-Battle/design_levelselect.md`
2) `Assets/_Project/Scripts/Shared/UI/LevelSelectButton.cs`
3) `Assets/_Project/Scripts/Shared/UI/LevelSelectController.cs`
4) `Assets/_Project/Editor/LevelSelectSceneBuilder.cs`
5) `Assets/_Project/Scripts/Shared/UI/BrainBattleDesignSystem.cs`
6) `/Users/bytedance/Documents/go/Brain-Battle/CLAUDE.md`

Implementasi detail:
1) Ukuran cell target 300x300:
   - Terapkan lewat GridLayout builder/token agar konsisten.
2) Completed state:
   - Tampilkan check icon menonjol (target visual 52 bold) + nomor level tetap terbaca.
3) Current/Available state:
   - Nomor level utama target visual 38 bold.
   - Tampilkan label "Play" yang jelas pada state available/current.
4) Locked state:
   - Ikon lock target visual 32.
   - Nomor level low emphasis (jika ditampilkan) / sesuai desain final yang paling jelas.
5) Kontras dan keterbacaan:
   - Pastikan 3 state bisa dibedakan cepat tanpa legenda.

Catatan implementasi penting:
- Jika prefab structure perlu update, lakukan via `BuildLevelButtonPrefab()` di `LevelSelectSceneBuilder.cs`.
- Jangan patch prefab manual sebagai solusi utama.
- Jika tambah SerializeField di `LevelSelectButton`, update wiring serialized object builder.

Verifikasi wajib:
- Available/Completed/Locked berbeda jelas secara visual.
- Klik cell locked tetap non-interactable.
- Tidak ada error/warning wiring baru.

Setelah coding:
1) Update `Assets/_Project/Documentation/Milestones.md` untuk Task 5.
2) Berikan output standar (file changed, ringkasan, checklist pass/fail, risiko regresi).
```

---

## Prompt Task 6 — Grid layout & spacing

```txt
Kerjakan HANYA Task 6 dari `/Users/bytedance/Documents/go/Brain-Battle/design_levelselect.md`.

Tujuan task:
Merapikan grid 3 kolom agar ukuran cell, gap, padding, dan scroll behavior mengikuti densitas referensi tanpa clipping viewport.

Scope ketat:
- LevelSelect saja
- Fokus GridLayoutGroup + ScrollRect ergonomi
- Jangan ubah logic selection/unlock

File wajib dibaca:
1) `/Users/bytedance/Documents/go/Brain-Battle/design_levelselect.md`
2) `Assets/_Project/Editor/LevelSelectSceneBuilder.cs`
3) `Assets/_Project/Scripts/Shared/UI/LevelSelectController.cs`
4) `Assets/_Project/Scripts/Shared/UI/BrainBattleDesignSystem.cs`
5) `/Users/bytedance/Documents/go/Brain-Battle/CLAUDE.md`

Implementasi detail:
1) Grid 3 kolom:
   - Pertahankan fixed 3 columns.
2) Cell target:
   - Gunakan target 300x300 (dari design task) atau token level-select yang disepakati konsisten.
3) Gap horizontal/vertical:
   - Set agar densitas mirip referensi.
   - Pakai token jika tersedia / tambah token jika dipakai berulang.
4) Padding viewport/content:
   - Pastikan tidak clipping di sisi kiri/kanan/atas/bawah.
5) Scroll comfort:
   - Vertical scroll tetap nyaman; tidak terlalu sensitif/terlalu lambat.
   - Tidak ada dead-zone aneh pada awal/akhir list.

Batasan anti-bug:
- Jangan ubah hierarchy scene di luar area LevelScrollView/Content/grid-related.
- Jangan ubah perilaku tab atau play button di task ini.

Verifikasi wajib:
- Grid alignment rapi di 3 kolom.
- Tidak ada clipping dalam viewport.
- Scroll tetap smooth dan usable.

Setelah coding:
- Update `Assets/_Project/Documentation/Milestones.md` untuk Task 6.
- Output standar hasil + checklist acceptance Task 6.
```

---

## Prompt Task 7 — Bottom primary CTA (PLAY)

```txt
Kerjakan HANYA Task 7 dari `/Users/bytedance/Documents/go/Brain-Battle/design_levelselect.md`.

Tujuan task:
Membuat bottom CTA PLAY konsisten sebagai tombol utama: tinggi 96, full-width dalam container, visual primary benar, dan selalu menuju level aktif yang tepat.

Scope:
- LevelSelect only
- Fokus CTA visual + binding ke selected/active level
- Jangan ubah flow scene lain

File wajib dibaca:
1) `/Users/bytedance/Documents/go/Brain-Battle/design_levelselect.md`
2) `Assets/_Project/Scripts/Shared/UI/LevelSelectController.cs`
3) `Assets/_Project/Editor/LevelSelectSceneBuilder.cs`
4) `Assets/_Project/Scripts/Shared/UI/BrainBattleDesignSystem.cs`
5) `/Users/bytedance/Documents/go/Brain-Battle/CLAUDE.md`

Implementasi detail:
1) Layout CTA:
   - Tinggi tombol target 96.
   - Full width dalam container bawah dengan margin kiri/kanan yang konsisten.
2) Style CTA:
   - Primary pink (`DesignSystem.Primary`), teks uppercase tebal.
   - Kontras teks harus tinggi.
3) Binding aksi:
   - Tombol PLAY harus merefleksikan level aktif/selected yang semestinya dimainkan.
   - Jika behavior existing memilih first available di tab aktif, pastikan tetap konsisten dan benar.
4) Interaksi:
   - Tombol bisa ditekan konsisten di semua tab.
   - Tidak ada state salah target saat user ganti tab cepat.

Anti-bug:
- Jangan ubah signature method publik tanpa kebutuhan.
- Jangan ubah navigation key penting (`Kings_PendingLevel`) selain jika diperlukan bugfix langsung terkait task.

Verifikasi wajib:
- CTA visual sesuai target.
- PLAY selalu load level yang tepat dari kondisi aktif saat ini.
- Tidak ada null reference.

Setelah coding:
- Update `Assets/_Project/Documentation/Milestones.md` untuk Task 7.
- Output standar + acceptance checklist Task 7.
```

---

## Prompt Task 8 — Tokenisasi Design System

```txt
Kerjakan HANYA Task 8 dari `/Users/bytedance/Documents/go/Brain-Battle/design_levelselect.md`.

Tujuan task:
Audit dan migrasi seluruh hardcoded style value yang disentuh Task 3-7 ke `DesignSystem` token, termasuk menambah token baru yang memang layak.

Scope:
- Fokus pada file yang terkait LevelSelect task 3-7
- Jangan ubah behavior logic gameplay

File wajib dibaca:
1) `/Users/bytedance/Documents/go/Brain-Battle/design_levelselect.md`
2) `Assets/_Project/Scripts/Shared/UI/BrainBattleDesignSystem.cs`
3) `Assets/_Project/Editor/LevelSelectSceneBuilder.cs`
4) `Assets/_Project/Scripts/Shared/UI/LevelSelectController.cs`
5) `Assets/_Project/Scripts/Shared/UI/LevelSelectButton.cs`
6) `/Users/bytedance/Documents/go/Brain-Battle/CLAUDE.md`

Implementasi detail:
1) Audit hardcoded:
   - Inventaris angka style (warna/size/spacing/radius) yang masih hardcoded pada area yang disentuh.
2) Replace token existing:
   - Ganti ke token existing jika sudah tersedia.
3) Tambah token baru bila layak:
   - Hanya untuk nilai yang dipakai >=2 tempat atau branding-critical.
   - Nama token jelas dan konsisten dengan naming existing.
4) Pastikan pemanggilan token dilakukan runtime, bukan serialized default yang mudah stale.

Batasan:
- Jangan tokenisasi berlebihan untuk nilai one-off yang benar-benar unik dan non-branding.
- Jangan menyentuh scene selain LevelSelect.

Verifikasi wajib:
- Tidak ada hardcoded style value yang seharusnya bertoken di area task.
- Build script dan runtime script tetap kompilasi tanpa warning/error baru.

Setelah coding:
- Update `Assets/_Project/Documentation/Milestones.md` untuk Task 8.
- Output:
  - daftar token baru/yang dipakai
  - file changed
  - checklist acceptance Task 8
```

---

## Prompt Task 9 — Responsiveness & regression check

```txt
Kerjakan HANYA Task 9 dari `/Users/bytedance/Documents/go/Brain-Battle/design_levelselect.md`.

Tujuan task:
Melakukan pass responsiveness portrait (rasio pendek vs panjang), memperbaiki clipping/overlap jika ada, dan memastikan tidak ada regresi flow.

Scope:
- LevelSelect scene dan script terkait visual LevelSelect
- Jangan ubah flow scene lain kecuali fix regresi yang terbukti berasal dari perubahan LevelSelect

File wajib dibaca:
1) `/Users/bytedance/Documents/go/Brain-Battle/design_levelselect.md`
2) `Assets/_Project/Editor/LevelSelectSceneBuilder.cs`
3) `Assets/_Project/Scripts/Shared/UI/LevelSelectController.cs`
4) `Assets/_Project/Scripts/Shared/UI/LevelSelectButton.cs`
5) `/Users/bytedance/Documents/go/Brain-Battle/CLAUDE.md`

Implementasi & verifikasi detail:
1) Uji portrait ratio:
   - Simulasikan beberapa rasio portrait (tinggi pendek & tinggi panjang) via Game view/aspect presets.
2) Cek area kritikal:
   - Header, tabs, progress row, grid, bottom CTA.
   - Pastikan tidak clipping/overlap/terpotong safe area.
3) Perbaikan terukur:
   - Lakukan adjustment anchor/offset/padding/gap seperlunya, seminimal mungkin.
4) Regression guard:
   - Pastikan golden path LevelSelect tetap jalan: tab switch, pilih level available, PLAY navigasi benar.
   - Pastikan tidak mengubah flow logic SampleScene.

Catatan pembuktian:
- Untuk screenshot final, jangan mengandalkan MCP screenshot sebagai bukti final.
- Minta user capture screenshot Game view manual bila perlu approval visual akhir.

Setelah coding:
- Update `Assets/_Project/Documentation/Milestones.md` untuk Task 9.
- Output:
  - device ratio yang diuji
  - issue yang ditemukan + fix
  - checklist acceptance Task 9
```

---

## Prompt Task 10 — Final QA checklist

```txt
Kerjakan HANYA Task 10 dari `/Users/bytedance/Documents/go/Brain-Battle/design_levelselect.md`.

Tujuan task:
Menuntaskan final QA checklist LevelSelect untuk memastikan task 3-9 siap handoff tanpa bug fungsional utama.

Scope:
- QA + perbaikan minor langsung jika ditemukan isu dari checklist
- Jangan ekspansi fitur baru

File wajib dibaca:
1) `/Users/bytedance/Documents/go/Brain-Battle/design_levelselect.md`
2) File implementasi yang sudah disentuh task 3-9
3) `/Users/bytedance/Documents/go/Brain-Battle/CLAUDE.md`
4) `Assets/_Project/Documentation/Milestones.md`

Checklist yang WAJIB dibuktikan:
1) Tab state benar (active/inactive) di Beginner/Expert/Impossible.
2) Progress persen + bar benar per difficulty.
3) Level cell state benar (completed/current-available/locked).
4) Tombol PLAY memulai level yang tepat.
5) Tidak ada null reference dari wiring builder.

Langkah eksekusi:
1) Jalankan pengecekan end-to-end LevelSelect flow.
2) Jika ada bug ringan dari checklist, fix langsung di file yang relevan (tetap dalam scope).
3) Jika fix menyentuh SerializeField wired, update builder wiring dan rebuild scene via menu builder.
4) Re-test sampai semua checklist pass.

Output final wajib:
- Status pass/fail per item checklist (5 item)
- File yang diubah (jika ada fix QA)
- Ringkasan fix (jika ada)
- Sisa risiko/open issue (kalau ada)
- Konfirmasi `Assets/_Project/Documentation/Milestones.md` sudah diupdate untuk Task 10
```

---

## Catatan penggunaan

- Jalankan berurutan: Task 3 → 4 → 5 → 6 → 7 → 8 → 9 → 10.
- Jika satu task butuh perubahan struktural besar, selesaikan dan verifikasi task itu dulu sebelum lanjut task berikutnya.
- Setiap task wajib update `Assets/_Project/Documentation/Milestones.md`.

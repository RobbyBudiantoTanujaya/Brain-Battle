# Design Breakdown — Level Select Scene

## Tujuan
Menerjemahkan referensi desain Level Select menjadi task implementasi kecil, berurutan, dan siap eksekusi di Unity (`LevelSelect` scene).

## Acuan Visual (dari referensi)
- Reference frame: **1170 × 2532**
- Eyebrow text: **18px**
- Title: **42px bold**
- Tab: **height 72px**, font **18px**
- Progress bar: **height 6px**
- Level cell: **300 × 300**
- Check icon: **52px bold**
- Number: **38px bold**
- Lock icon: **32px**
- Bottom Play button: **height 96px**

> Catatan implementasi: jika value sudah punya `DesignSystem` token, wajib pakai token. Jika value baru dipakai di >=2 tempat atau termasuk keputusan branding, tambahkan token baru dulu.

---

## Breakdown Task Kecil

### 1) Baseline scene & layout root
- [ ] Konfirmasi pekerjaan hanya untuk `LevelSelect` scene.
- [ ] Pastikan Canvas scaler tetap konsisten (reference 1170×2532, match 0.5).
- [ ] Validasi safe-area/padding root agar layout tidak mentok notch/home indicator.

**Done jika:** root layout stabil di portrait dan tidak overlap elemen utama.

### 2) Header section (eyebrow + title)
- [ ] Set teks eyebrow (`BRAIN BATTLE`) dengan ukuran target visual 18.
- [ ] Set title `Level Select` dengan ukuran target visual 42 bold.
- [ ] Rapikan spacing vertikal eyebrow ↔ title ↔ tab row.

**Done jika:** hierarchy header terbaca jelas dan proporsinya match mockup.

### 3) Segmented tabs (Beginner / Expert / Impossible)
- [ ] Bentuk container tab model segmented control.
- [ ] Set tinggi tab 72, font 18.
- [ ] Definisikan state visual: active (pink fill) vs inactive (dark).
- [ ] Pastikan klik tab hanya mengubah difficulty aktif dan refresh grid.

**Done jika:** state aktif/inaktif jelas, switching tab mulus tanpa glitch.

### 4) Progress row per difficulty
- [ ] Tambahkan label progress sesuai difficulty aktif (contoh: `Impossible progress`).
- [ ] Set bar track + fill dengan tinggi 6.
- [ ] Tampilkan persentase progres di kanan (mis. 50%).
- [ ] Bind ke data progres aktual per tab.

**Done jika:** teks + bar sinkron saat tab berganti.

### 5) Level cell visual states (Completed / Current / Locked)
- [ ] Finalisasi ukuran cell 300×300 dan radius/border sesuai style.
- [ ] Completed state: tampil check icon (52 bold) + nomor level.
- [ ] Current/available state: nomor level utama (38 bold) + label `Play`.
- [ ] Locked state: ikon lock (32) + nomor level low emphasis.

**Done jika:** 3 state bisa dibedakan dalam 1 detik tanpa melihat legenda.

### 6) Grid layout & spacing
- [ ] Konfigurasi GridLayoutGroup (3 kolom) mengikuti ukuran cell target.
- [ ] Set gap horizontal/vertical agar densitas mirip referensi.
- [ ] Pastikan scroll behavior tetap nyaman saat konten panjang.

**Done jika:** alignment rapi, tidak ada clipping di viewport.

### 7) Bottom primary CTA (`PLAY`)
- [ ] Set tombol bawah tinggi 96, full-width dalam container.
- [ ] Terapkan visual primary button (pink, text uppercase tebal).
- [ ] Hubungkan tombol ke level yang sedang terpilih/aktif.

**Done jika:** CTA selalu merefleksikan pilihan level aktif dan bisa ditekan konsisten.

### 8) Tokenisasi Design System
- [ ] Audit semua hardcoded color/size/spacing yang tersentuh di task ini.
- [ ] Ganti ke `DesignSystem` token yang sudah ada.
- [ ] Tambahkan token baru bila nilai dipakai berulang/branding-critical.

**Done jika:** tidak ada hardcoded style value yang seharusnya bertoken.

### 9) Responsiveness & regression check
- [ ] Uji di beberapa rasio layar portrait (tinggi pendek vs tinggi panjang).
- [ ] Cek clipping teks, tab, progress row, grid, dan bottom CTA.
- [ ] Pastikan tidak mengubah flow logic di scene lain.

**Done jika:** golden path level select tetap berjalan dan visual tetap konsisten lintas device ratio.

### 10) Final QA checklist
- [ ] Tab state benar (active/inactive) di semua difficulty.
- [ ] Progress persentase + bar benar per difficulty.
- [ ] State cell benar (completed/current/locked).
- [ ] Tombol PLAY memulai level yang tepat.
- [ ] Tidak ada null reference dari field wiring scene builder.

**Done jika:** seluruh checklist lolos sebelum handoff.

---

## Urutan Eksekusi Disarankan (Cepat)
1. Baseline + Header
2. Tabs + Progress
3. Cell states + Grid
4. Bottom CTA
5. Tokenisasi
6. Responsive + QA

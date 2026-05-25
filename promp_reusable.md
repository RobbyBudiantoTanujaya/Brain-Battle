# Prompt Template — Generate `reusable.md` for Brain Battle-style Unity project

Gunakan prompt ini di Claude Code lain agar dia bisa menghasilkan `reusable.md` yang konsisten, actionable, dan langsung kepakai.

---

## Prompt

Anda adalah engineer yang diminta membuat dokumen `reusable.md` untuk project Unity.

Tujuan: identifikasi komponen yang bisa dipakai ulang untuk game berikutnya, jelaskan batasannya, dan cara pakainya.

### Aturan output
1. Output harus berupa file markdown bernama `reusable.md` di root project.
2. Bahasa Indonesia, ringkas tapi konkret.
3. Wajib menyertakan referensi file+line format `path:line` untuk setiap klaim penting.
4. Jangan menulis teori umum; fokus pada codebase yang sedang dibaca.
5. Pisahkan dengan jelas:
   - bisa direuse langsung
   - bisa direuse dengan adaptasi
   - jangan direuse mentah.
6. Untuk setiap item reusable, isi 4 bagian:
   - **Komponen**
   - **Kenapa reusable**
   - **Cara pakai di game berikutnya**
   - **Batasan/Risiko**

### Scope analisis minimum (WAJIB dibaca)
- `Assets/_Project/Scripts/Shared/**`
- `Assets/_Project/Scripts/Core/**`
- `Assets/_Project/Scripts/Games/**`
- `Assets/_Project/Editor/**`
- `CLAUDE.md`

### Fokus evaluasi
Nilai reusability untuk domain berikut:
1. Design system/token UI
2. Data model grid & validator logic
3. Pipeline level data (SO + loader + generator pattern)
4. UI shell (level select, victory, HUD button controllers)
5. Audio framework
6. Editor automation (scene builder/validator/setup)

### Kriteria penilaian reusable
Anggap reusable tinggi jika:
- tidak bergantung kuat pada nama game/scene tertentu,
- minim hardcoded key/path,
- pure logic dan low coupling,
- bisa dipindah dengan perubahan kecil.

Anggap reusable rendah jika:
- penuh literal `Kings_*`, `SampleScene`, path asset spesifik,
- logic rules puzzle sangat spesifik,
- wiring ketat ke hierarchy scene tertentu.

### Struktur output yang wajib
`reusable.md` harus berisi:
1. Ringkasan cepat (top reusable candidates)
2. Daftar reusable per kategori (6 kategori fokus di atas)
3. Bagian "Jangan direuse mentah"
4. Template langkah migrasi ke game baru (step-by-step)
5. Checklist verifikasi setelah reuse

### Larangan
- Jangan mengubah code C#.
- Jangan membuat asumsi file yang tidak ada.
- Jangan menghapus dokumen lain.

Terakhir, setelah menulis `reusable.md`, tampilkan ringkasan 5-8 poin tentang apa yang paling reusable dan apa yang paling berisiko saat direuse.

---

## Versi singkat (1 paragraf)

Baca script `Assets/_Project/Scripts/{Shared,Core,Games}` dan `Assets/_Project/Editor`, lalu buat `reusable.md` di root yang memetakan komponen mana yang reusable untuk game berikutnya (langsung/adaptasi/tidak), sertakan alasan, cara pakai, batasan, dan referensi `path:line`; fokus ke design system, core grid/validator, level pipeline, UI shell, audio, dan editor tooling; tutup dengan langkah migrasi + checklist validasi reuse.

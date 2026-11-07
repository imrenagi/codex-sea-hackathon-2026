# RoadSense · Handoff macOS

> Historical donor-package record for `bb63806`. The integrated release preserves this provenance; its exact source, tests, app path and native acceptance are recorded in the adjacent build manifest. The integrated first Play uses the guided crossing, successful completion exposes **Pilih perjalanan**, and subsequent Play opens that chooser. Normal S/↓ brakes then reverses; Space is brake-only.

Tim **We Will Handle**. Paket native **ARM64 / Mono** ada di `/Users/jonathan.gunawan/Desktop/RoadSense.app` dan sudah dibuka kembali setelah build berhasil.

## Revisi paket

- Source aplikasi: `bb638069b168b64770b3c0396d4f213bba2b8236`.
- Versi aplikasi di bundle: `0.1.0`, build number `0`. Nomor ini bukan SHA source atau versi Unity.
- Editor lokal untuk build: Unity `6000.6.8f1`, preview terisolasi dengan workaround font Standard. Source canonical tetap Unity `6000.3.27f1`.
- Commit dokumentasi setelah SHA tersebut tidak mengubah aplikasi terpasang. `codex/roadsense-macos`, PR #18 dan `codex/demo-ready` dapat memuat pembaruan dokumen yang lebih baru.
- Bundle ID `id.jalankita.hackathon` dan key save lama tetap. Versi aplikasi sebelumnya disimpan di `/Users/jonathan.gunawan/Desktop/.RoadSense-backup-a2f3ec4.app`.

## Masuk demo

**Play → kartu Mobil/Motor → Mode lainnya → Coba demo · Hak utama**. Alternatif: **F2** dari home atau chooser. **Mulai Perjalanan** memulai/melanjutkan perjalanan lengkap, bukan demo.

Pada entry training pertama per kendaraan dalam proses aplikasi, W memulai gerakan, lalu muncul **Tekan S untuk rem**. Tahan S saat bergerak sampai berhenti. Space saja tidak menyelesaikan langkah S. Salinan latihan tidak memberi penalti, progres checkpoint atau perubahan save. Setelah selesai, salinan dibuang dan sesi asli dipulihkan ke keadaan sebelum latihan. Lepas tombol sebelum melanjutkan; aturan normal kembali aktif. Ini bukan kekebalan permanen pada perjalanan asli.

Ambience dekoratif (chirp/drone) tetap diam selama onboarding aktif, termasuk langkah S, panduan manual dan latihan eksplorasi. Setelah onboarding, ambience masih menunggu delapan detik gerakan sebelum jadwal dekoratif berjalan. Audio keselamatan dan feedback tetap tersedia; preferensi mute/volume tidak diganti.

## Kontrol

| Tombol | Aksi |
|---|---|
| W / ↑; A/D / ←→ | Gas; kemudi |
| S / ↓ / Space | Rem; S wajib untuk menyelesaikan latihan awal |
| Q/E; C; L | Sein; kamera; lampu motor |
| Z/X; H; M | Lihat kiri/kanan; klakson; mute |
| F1; Tab/Esc | Panduan manual yang dijeda; jeda/lanjut |
| R; D pada feedback | Retry; lihat contoh |

## Batas verifikasi

Compile/build berhasil dan aplikasi native dibuka dari path Desktop. Gameplay terbaru, respons input, transisi W → S → sesi asli, penalti setelah grace, suara yang terdengar dan save antar-relaunch belum diverifikasi melalui tes pengguna. Tidak ada tes, smoke atau screenshot tambahan untuk paket ini. Rekaman demo belum dikonfirmasi tersedia.

Gunakan [runbook enam menit](ROAD_SENSE_DEMO.md). Log build lokal: `/private/tmp/roadsense-gui-build.log`, marker sukses pada baris29047. Bukti handoff lokal terbaru dicatat di `/private/tmp/roadsense-handoff-final.md`.

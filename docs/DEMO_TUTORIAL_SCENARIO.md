# Tutorial di dalam demo

1. Play membuka demo Pasar Bunga langsung dengan mobil. Tekan W untuk mendekati zebra cross; tahap pengenalan tidak memberi penalti atau progres.
2. Sebelum zebra cross, simulasi dijeda. Kartu di tengah layar menampilkan **Ada orang menyeberang** dan satu instruksi: **Tekan [S] untuk mengerem**.
3. Tekan S. Simulasi lanjut dan rem ditahan sampai mobil berhenti. Gas dan kemudi diabaikan selama langkah ini.
4. Kartu di tengah menampilkan **Tunggu sampai pejalan kaki selesai menyeberang**. Kendaraan tetap direm, tetapi simulasi pejalan kaki terus berjalan. Menahan W tidak melewati langkah ini.
5. Ketika aktor pejalan kaki benar-benar selesai menyeberang dan mobil berhenti, kartu berubah menjadi **Tekan [W] untuk melanjutkan** dan tetap tampil sampai W baru ditekan. Lepas lalu tekan W kembali untuk melanjutkan.
6. Pesan mengonfirmasi aturan normal aktif. Perjalanan demo berlanjut tanpa pindah ke tutorial terpisah.

Esc pada popup tutorial hanya menutup popup dan melanjutkan simulasi dengan rem otomatis; kendaraan tetap menunggu penyeberang sampai aman, lalu memerlukan W baru. Tombol jeda tidak ditampilkan selama demo. Esc/Tab saat berkendara tetap membuka jeda; R mengulang seluruh urutan, termasuk saat popup atau langkah menunggu aktif. Tidak ada timer buatan untuk menganggap penyeberangan aman. Mode demo tetap tidak menulis progres perjalanan.

Selama tutorial, teks tujuan, toast, dan hint kontrol disembunyikan supaya satu instruksi tetap terbaca. HUD biasa kembali setelah tutorial selesai.

Source disinkronkan ke project Unity terbuka; gameplay belum diuji otomatis. Perubahan berlaku saat pengguna memuat ulang sendiri; sesi Unity dan Play Mode tidak dikendalikan oleh agen. Tahan W selama menunggu untuk memastikan mobil tidak berjalan sendiri, lalu lepaskan dan tekan W saat petunjuk lanjut tampil.

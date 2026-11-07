namespace JalanKita
{
    // Summaries of UU 22/2009; fines read with UU 1/2026 Pasal II (3)-(4).
    // No fixed fine is inferred where the simulated event does not establish an offence.
    public readonly struct LawDetails
    {
        public const string Amendment = "https://peraturan.bpk.go.id/Details/337869/uu-no-1-tahun-2026";
        public readonly string Meaning, Sanction;
        LawDetails(string meaning,string sanction) { Meaning=meaning; Sanction=sanction; }
        static string Fine(string article,string amount) => article+" · Denda paling banyak Rp"+amount+".";
        public static LawDetails For(Rule rule) => rule switch
        {
            Rule.SeatBelt => new("Pengemudi dan penumpang yang duduk di samping pengemudi pada kendaraan beroda empat atau lebih wajib mengenakan sabuk keselamatan.",Fine("Pasal 289","250.000")),
            Rule.Helmet => new("Pengemudi dan penumpang sepeda motor wajib memakai helm yang memenuhi Standar Nasional Indonesia (SNI).",Fine("Pasal 291 ayat (1)–(2)","250.000")+" Berlaku untuk pengemudi tanpa helm atau pengemudi yang membiarkan penumpangnya tanpa helm."),
            Rule.Headlight => new("Selain kewajiban lampu pada malam hari dan kondisi tertentu, pengemudi sepeda motor wajib menyalakan lampu utama pada siang hari.",Fine("Pasal 293 ayat (2)","100.000")),
            Rule.NoStopping => new("Kendaraan tidak boleh berhenti di tempat yang ditandai rambu atau marka larangan berhenti (Pasal 118 huruf a). Permintaan menjemput penumpang tidak menghapus larangan tersebut.",Fine("Pasal 287 ayat (1) untuk pelanggaran rambu larangan","500.000")),
            Rule.Stop or Rule.NoEntry or Rule.MovementLane => new("Pengemudi wajib menaati rambu perintah atau larangan dan marka jalan. Pada latihan ini, rambu STOP, larangan masuk, panah lajur dan garis utuh menentukan gerakan yang diperbolehkan.",Fine("Pasal 287 ayat (1)","500.000")),
            Rule.Signal or Rule.RightArrow => new("Pengemudi wajib mematuhi alat pemberi isyarat lalu lintas (APILL). Pada simpang dengan isyarat terpisah, hijau untuk lurus tidak mengizinkan belok kanan ketika panah kanan masih merah.",Fine("Pasal 287 ayat (2)","500.000")),
            Rule.StopBox => new("Pengemudi wajib menaati marka dan lampu lalu lintas. Mobil menunggu di belakang garis henti mobil; ruang henti di depannya diperuntukkan bagi sepeda motor.",Fine("Pasal 287 ayat (1) untuk marka; ayat (2) untuk APILL","500.000")+" Penerapan pasal mengikuti pelanggarannya, bukan penjumlahan otomatis."),
            Rule.Junction => new("Di simpang tanpa lampu, kendaraan dari jalan lebih kecil memberi jalan kepada arus di jalan utama; pada simpang tiga tegak lurus, arus di jalan lurus didahulukan.","Pasal 113 tidak menetapkan nominal denda secara langsung. Latihan ini tidak menetapkan denda otomatis untuk kejadian ini."),
            Rule.Pedestrian => new("Pengemudi wajib mengutamakan keselamatan pejalan kaki dan pesepeda. Pejalan kaki mendapat prioritas ketika menyeberang di tempat penyeberangan.",Fine("Pasal 284","500.000")),
            Rule.KeepLeft => new("Pengguna jalan memakai jalur sebelah kiri. Jalur kanan dapat digunakan untuk mendahului atau atas perintah petugas dalam ketentuan Pasal 108.","Pasal 108 tidak menetapkan nominal denda langsung. Pelanggaran marka dapat dikenai Pasal 287 ayat (1), paling banyak Rp500.000, jika unsur pelanggaran marka terpenuhi."),
            Rule.Indicator => new("Sebelum pindah lajur atau bergerak ke samping, pengemudi wajib mengamati lalu lintas di depan, samping dan belakang serta memberikan isyarat.",Fine("Pasal 295 untuk perpindahan tanpa isyarat","250.000")),
            Rule.Gap => new("Pasal 112 ayat (2) mewajibkan pengamatan sebelum pindah lajur. Ukuran celah aman yang diperiksa game adalah ambang latihan, bukan jarak angka yang ditetapkan pasal tersebut.","Pasal 295 mengatur perpindahan tanpa isyarat (denda paling banyak Rp250.000). Celah sempit saja tidak otomatis membuktikan pelanggaran pasal sanksi itu."),
            Rule.TurnSignal => new("Sebelum berbelok atau berbalik arah, pengemudi wajib mengamati lalu lintas di depan, samping dan belakang serta memberi isyarat dengan lampu penunjuk arah atau tangan.",Fine("Pasal 294","250.000")),
            Rule.Speed or Rule.BoulevardSpeed => new("Pengemudi wajib mematuhi batas kecepatan maksimum dan minimum; mengemudi melampaui batas maksimum dilarang. Batas 30 atau 50 km/j yang terpasang pada ruas latihan ini harus dipatuhi.",Fine("Pasal 287 ayat (5)","500.000")),
            Rule.Phone => new("Pengemudi wajib berkendara secara wajar dan penuh konsentrasi. Penjelasan Pasal 106 ayat (1) mencakup gangguan akibat menggunakan telepon.",Fine("Pasal 283","750.000")),
            Rule.Railway => new("Di perlintasan sebidang, pengemudi wajib berhenti saat sinyal berbunyi, palang mulai ditutup atau ada isyarat lain, mendahulukan kereta, dan memberi hak utama kepada kendaraan yang lebih dahulu melintasi rel.",Fine("Pasal 296 untuk tidak berhenti saat peringatan","750.000")),
            Rule.Emergency => new("Ambulans yang mengangkut orang sakit mendapat hak utama. Kendaraan yang mendapat hak utama menggunakan pengawalan polisi dan/atau lampu isyarat merah atau biru serta sirene sesuai Pasal 135.",Fine("Pasal 287 ayat (4)","250.000")),
            _ => new("Ini adalah sasaran latihan atau saran keselamatan.","Tidak ada denda otomatis yang ditetapkan oleh latihan ini.")
        };
    }
}

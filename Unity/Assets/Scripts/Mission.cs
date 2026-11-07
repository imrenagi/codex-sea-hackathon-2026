namespace JalanKita
{
    public enum MissionKind { SkillTest, Scenario }

    // Missions select encounters from the shared district; their scores never replace the full journey.
    public sealed class Mission
    {
        public readonly string Id, Title, Brief, Focus, Duration;
        public readonly MissionKind Kind;
        public readonly int FirstStage, LastStage;
        public readonly float Finish;
        public bool IsVillage => Id == "village";
        public bool IsSchool => Id == "school";
        public bool IsMarket => Id == "market";

        Mission(string id, MissionKind kind, string title, string brief, string focus, string duration,
            int firstStage, int lastStage, float finish)
        {
            Id = id; Kind = kind; Title = title; Brief = brief; Focus = focus; Duration = duration;
            FirstStage = firstStage; LastStage = lastStage; Finish = finish;
        }

        public static readonly Mission[] Tests =
        {
            new("preparation", MissionKind.SkillTest, "Sebelum berangkat", "Siapkan perlengkapan pengemudi dan penumpang.",
                "Perlengkapan sesuai kendaraan", "1 menit", 0, 0, -12),
            new("signals", MissionKind.SkillTest, "Rambu & lampu", "Lewati rambu STOP dan persimpangan berlampu dengan benar.",
                "STOP / Hak utama / Lampu", "1–2 menit", 1, 1, 108),
            new("priority", MissionKind.SkillTest, "Hak utama", "Lewati penyeberangan dan arus jalan utama.",
                "Pejalan kaki / Arus utama", "1–2 menit", 2, 2, 204),
            new("lanes", MissionKind.SkillTest, "Arah & lajur", "Pindah ke kanan lalu kembali ke kiri sebelum jalan menyempit.",
                "Arah jalan / Sein / Celah aman", "1–2 menit", 3, 3, 298),
            new("focus", MissionKind.SkillTest, "Kecepatan & fokus", "Patuhi rambu 30 dan tanggapi panggilan dengan aman.",
                "Kecepatan / Konsentrasi", "1–2 menit", 4, 4, 382)
        };

        public static readonly Mission[] Scenarios =
        {
            new("village", MissionKind.Scenario, "Keluar dari Kampung",
                "Berangkat bersama penumpang menuju warung. Jalan keluar kampung memiliki rambu STOP dan arus silang.",
                "Persiapan / STOP / Hak utama", "1–2 menit", 0, 1, 56),
            new("school", MissionKind.Scenario, "Lewat Sekolah",
                "Lewati sekolah saat jam pulang. Seseorang akan menyeberang, lalu teleponmu berdering.",
                "Kecepatan / Pejalan kaki / Fokus", "1–2 menit", 4, 4, 382),
            new("market", MissionKind.Scenario, "Jemput di Pasar",
                "Penumpang meminta dijemput di area dilarang berhenti. Ikuti jalan satu arah menuju teluk jemput, lalu bergabung kembali.",
                "Arah jalan / Tempat berhenti / Sein & celah", "2–3 menit", 3, 3, 298)
        };

        public const float SchoolZebra = 324;
        public const float SchoolPhoneCheckpoint = 328;
        public const float PickupStart = 250, PickupEnd = 278, PickupPoint = 266;
        public const float NoStopStart = 229, NoStopEnd = 244;

        public JourneySave NewSave(JourneySave previous = null) => new()
        {
            next = FirstStage,
            completions = previous?.completions ?? 0,
            bestMistakes = previous?.bestMistakes ?? -1,
            variant = (previous?.variant ?? -1) + 1
        };

        public int CompletedSteps(Journey run)
        {
            if (run.Finished) return 3;
            if (IsVillage) return run.Stage == 0 ? 0 : run.FullStop ? 2 : 1;
            if (IsSchool) return run.Save.checkpoint;
            if (IsMarket) return run.PassengerPickedUp ? 2 : run.Save.checkpoint;
            return 0;
        }
    }
}

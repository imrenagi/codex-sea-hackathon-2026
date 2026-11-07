using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace JalanKita
{
    public sealed partial class JalanGame
    {
        void ShowCatalogue(MissionKind kind)
        {
            SetPage(Page.Catalogue);
            AddLabel(dialog, (mode == VehicleMode.Car ? "MOBIL" : "MOTOR") + "  /  PILIH KEGIATAN", "kicker");
            AddLabel(dialog, kind == MissionKind.Scenario ? "Misi sehari-hari" : "Uji keterampilan", "heading");
            AddLabel(dialog, kind == MissionKind.Scenario
                ? "Satu tujuan, beberapa keputusan. Ulangi bagian yang sulit dan coba variasi lalu lintas berikutnya."
                : "Pilih keterampilan yang ingin diuji. Hasil dan percobaan disimpan untuk tiap tes.", "copy");
            var list = new ScrollView(ScrollViewMode.Vertical);
            list.verticalScrollerVisibility = ScrollerVisibility.AlwaysVisible;
            list.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
            list.contentViewport.style.overflow = Overflow.Hidden;
            list.AddToClassList("mission-list");
            dialog.Add(list);
            foreach (var activity in kind == MissionKind.Scenario ? Mission.Scenarios : Mission.Tests)
            {
                var saved = JourneySave.Load(mode, activity);
                var card = Element(list.contentContainer, "mission-card");
                var row = Element(card, "row split");
                AddLabel(row, activity.Title, "subhead mission-title");
                AddLabel(row, activity.Duration, "kicker duration");
                AddLabel(card, activity.Focus, "copy");
                string status = saved == null ? "Belum dicoba"
                    : saved.next <= activity.LastStage ? "Sedang berlangsung  /  Percobaan " + saved.attempts
                    : "Selesai " + saved.completions + " kali  /  Kesalahan paling sedikit: " + saved.bestMistakes;
                AddLabel(card, status, "objective-detail");
                AddButton(card, saved != null && saved.next <= activity.LastStage ? "Lanjutkan" : "Pilih", () => ShowMissionBrief(activity), "primary");
            }
            var actions = Element(dialog, "row actions catalogue-footer");
            AddButton(actions, "Menu utama", BackToMenu, "quiet");
            AddButton(actions, kind == MissionKind.Scenario ? "Lihat uji keterampilan" : "Lihat misi sehari-hari",
                () => ShowCatalogue(kind == MissionKind.Scenario ? MissionKind.SkillTest : MissionKind.Scenario), "quiet");
        }

        void ShowMissionBrief(Mission activity)
        {
            SetPage(Page.Brief);
            var saved = JourneySave.Load(mode, activity);
            bool resume = saved != null && saved.next <= activity.LastStage;
            AddLabel(dialog, (activity.Kind == MissionKind.Scenario ? "MISI SEHARI-HARI" : "UJI KETERAMPILAN")
                + "  /  " + activity.Duration, "kicker");
            AddLabel(dialog, activity.Title, "heading");
            AddLabel(dialog, activity.Brief, "copy");
            var note = Element(dialog, "note");
            AddLabel(note, "FOKUS LATIHAN", "kicker");
            AddLabel(note, activity.Focus, "copy");
            AddLabel(dialog, activity.Kind == MissionKind.Scenario
                ? "Selesaikan tujuan dengan aman. Kemajuan tersimpan setelah tiap bagian; tidak ada bonus untuk terburu-buru."
                : "Selesaikan tes tanpa pelanggaran. Setelah kesalahan, baca penjelasan dan mulai percobaan berikutnya.", "copy");
            if (resume) AddLabel(dialog, "Kemajuan tersimpan tersedia untuk kendaraan ini.", "objective-detail");
            var actions = Element(dialog, "row actions");
            AddButton(actions, resume ? "Lanjutkan" : "Mulai", () => StartMission(activity, saved), "primary");
            AddButton(actions, "Kembali", () => ShowCatalogue(activity.Kind), "quiet");
        }

        void StartMission(Mission activity, JourneySave saved, bool restart = false)
        {
            var save = !restart && saved != null && saved.next <= activity.LastStage ? saved : activity.NewSave(saved);
            journey = new Journey(mode, save, mission: activity);
            save.Store(mode, activity);
            hood = false;
            district.Snap();
            if (journey.Stage == 0) ShowPreflight();
            else SetPage(Page.Drive);
        }

        void PickupPassenger()
        {
            if (page != Page.Drive || learner != null) return;
            journey.Pickup();
            if (journey.Failure.HasValue) ShowLesson(journey.Failure.Value);
            else if (journey.PassengerPickedUp)
            {
                toast.text = "PENUMPANG SEDANG NAIK  /  TETAP BERHENTI";
                toastUntil = Time.unscaledTime + 4;
            }
        }

        void ShowMissionProgress()
        {
            SetPage(Page.Progress);
            var activity = journey.Activity;
            bool scenario = activity.Kind == MissionKind.Scenario;
            AddLabel(dialog, journey.Finished ? scenario ? "MISI SELESAI" : "TES SELESAI" : "KEMAJUAN TERSIMPAN", "kicker");
            AddLabel(dialog, activity.Title, "heading");
            AddLabel(dialog, journey.Finished ? scenario ? "Tujuan tercapai dengan selamat." : "Percobaan terakhir lulus."
                : "Kamu bisa melanjutkan atau kembali memilih kegiatan.", "copy");
            AddLabel(dialog, "Percobaan: " + journey.Save.attempts + "  /  Total kesalahan selama latihan: " + journey.MistakeCount, "copy");
            if (journey.Finished)
                AddLabel(dialog, "Hasil terbaik: " + journey.Save.bestMistakes + " kesalahan  /  Selesai " + journey.Save.completions + " kali", "objective-detail");
            var note = Element(dialog, "note");
            AddLabel(note, journey.Finished ? "KETERAMPILAN YANG DIPRAKTIKKAN" : "FOKUS LATIHAN", "kicker");
            AddLabel(note, activity.Focus, "copy");
            var errors = Enumerable.Range(0, Lesson.All.Length).Where(i => journey.Save.mistakes[i] > 0)
                .OrderByDescending(i => journey.Save.mistakes[i]).Take(2).ToArray();
            foreach (int i in errors) AddLabel(note, journey.Save.mistakes[i] + "×  " + Lesson.All[i].Title, "objective-detail");
            if (journey.Finished && errors.Length == 0) AddLabel(note, "Selesai tanpa kesalahan pada sesi ini.", "objective-detail");
            var actions = Element(dialog, "row actions");
            if (!journey.Finished) AddButton(actions, "Lanjutkan", () =>
            {
                if (journey.Stage == 0) ShowPreflight();
                else if (journey.Failure.HasValue) ShowLesson(journey.Failure.Value);
                else ReturnToJourney();
            }, "primary");
            else AddButton(actions, scenario ? "Coba lagi dengan variasi" : "Ulangi tes",
                () => StartMission(activity, journey.Save, restart: true), "primary");
            AddButton(actions, scenario ? "Pilih misi" : "Pilih tes", () => ShowCatalogue(activity.Kind), "quiet");
            AddButton(actions, "Menu utama", BackToMenu, "quiet");
        }

        void RefreshMissionPrompt(ref string prompt, ref string signal, ref float target)
        {
            if (journey.Activity == null) return;
            title.text = journey.Activity.Title;
            if (journey.Activity.IsVillage && journey.Front > RouteStage.StopJunction + 5.5f)
            {
                prompt = "Warung sudah dekat. Lanjutkan ke penanda tujuan kuning.";
                signal = "MENUJU WARUNG";
                target = journey.StageEnd;
            }
            if (journey.IsSchool && !journey.CrossedZebra)
            {
                prompt = journey.Front < RouteStage.SchoolLimit ? "Jam pulang sekolah. Perhatikan rambu 30 dan penyeberangan."
                    : "Periksa penyeberangan sebelum melanjutkan.";
                target = journey.CrossingZebra - 2;
                if (journey.Pedestrian.HasValue) signal = "PEJALAN KAKI MENYEBERANG";
            }
            if (journey.IsMarket)
            {
                if (journey.PassengerPickedUp)
                {
                    prompt = "Beri sein kanan [E], periksa arus, lalu keluar dari teluk saat aman.";
                    if (journey.Position.x > -4.25f) prompt = "Penumpang sudah naik. Lanjutkan ke penanda tujuan kuning.";
                    target = journey.StageEnd;
                    if (journey.PickupTraffic.HasValue)
                    {
                        float gap = journey.PickupTraffic.Value.y - journey.Position.y;
                        signal = "MOBIL LAJUR KIRI  " + Mathf.RoundToInt(Mathf.Abs(gap)) + " M "
                            + (gap >= 0 ? "DI DEPAN" : "DI BELAKANG");
                    }
                    else signal = "PERIKSA ARUS SEBELUM BERGABUNG";
                    if (journey.IsBoarding)
                    {
                        prompt = "Penumpang sedang naik. Tetap berhenti, lalu periksa arus sebelum keluar.";
                        signal = "MENUNGGU PENUMPANG";
                    }
                }
                else
                {
                    prompt = journey.Position.y < Mission.PickupStart ? "Ikuti jalan satu arah. Lewati area larangan menuju teluk jemput kiri."
                        : "Beri sein kiri [Q], masuk teluk, lalu berhenti di dekat penumpang.";
                    target = Mission.PickupPoint;
                    signal = journey.CanPickup ? "TEKAN JEMPUT [F]"
                        : journey.Position.y <= Mission.NoStopEnd ? "PENUMPANG DIMINTA MENUNGGU DI TELUK" : "TELUK JEMPUT DI KIRI";
                }
            }
            if (journey.Activity.Kind == MissionKind.SkillTest && learner == null)
                prompt = journey.Activity.Brief;
        }
    }
}

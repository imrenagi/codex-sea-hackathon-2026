using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace JalanKita
{
    public sealed partial class JalanGame
    {
        LandmarkDrive hi;
        HiRoundaboutObjective hiObjective;
        bool hiActive, hiResultRecorded;
        HiMissionRecord hiRecord;
        const string HiTitle = "Melintasi Bundaran HI";
        const string HiFocus = "Masuk bundaran / Beri jalan / Keluar ke Thamrin";
        [Serializable] sealed class HiMissionRecord { public int attempts, completions, failures; }
        string HiSaveKey => "RoadSense.Mission.HI.v1." + mode;
        bool HiEnded => hiObjective != null && (hiObjective.Complete || hiObjective.Failed);
        float ExploreSpeed => hiActive ? hi.Speed : jaksa.Speed;
        int ExploreIndicator => hiActive ? hi.Indicator : jaksa.Indicator;

        HiMissionRecord ReadHiRecord()
        {
            try { return JsonUtility.FromJson<HiMissionRecord>(PlayerPrefs.GetString(HiSaveKey, "{}")) ?? new HiMissionRecord(); }
            catch { return new HiMissionRecord(); }
        }
        void StoreHiRecord() { PlayerPrefs.SetString(HiSaveKey, JsonUtility.ToJson(hiRecord)); PlayerPrefs.Save(); }
        void AddHiMissionCard(VisualElement list)
        {
            var saved = ReadHiRecord();
            var card = Element(list, "mission-card");
            var row = Element(card, "row split");
            AddLabel(row, HiTitle, "subhead mission-title");
            AddLabel(row, "2–4 menit", "kicker duration");
            AddLabel(card, HiFocus, "copy");
            AddLabel(card, saved.attempts == 0 ? "Belum dicoba" : "Percobaan " + saved.attempts + " / Selesai " + saved.completions + " kali", "objective-detail");
            AddButton(card, "Pilih", ShowHiBrief, "primary");
        }
        void ShowHiBrief()
        {
            catalogueKind = MissionKind.Scenario;
            SetPage(Page.Brief);
            AddLabel(dialog, "MISI SEHARI-HARI / 2–4 MENIT", "kicker");
            AddLabel(dialog, HiTitle, "heading");
            AddLabel(dialog, "Berangkat dari Sudirman, beri jalan kepada kendaraan di bundaran, lalu ikuti putaran dan keluar menuju Thamrin.", "copy");
            var note = Element(dialog, "note");
            AddLabel(note, "FOKUS LATIHAN", "kicker");
            AddLabel(note, HiFocus, "copy");
            AddLabel(dialog, "Perlambat sebelum masuk. Tunggu celah aman, lalu ikuti arahan di layar. Hasil disimpan terpisah; ulangi dari awal bila belum berhasil.", "copy");
            var actions = Element(dialog, "row actions");
            AddButton(actions, "Mulai", StartHiMission, "primary");
            AddButton(actions, "Kembali", () => ShowCatalogue(MissionKind.Scenario), "quiet");
        }
        void StartHiMission()
        {
            CloseCityPreview();
            pitchDemo = false; learner = null; practiceStart = -1;
            demoBrakeLatched = demoCrossingReady = demoActionReleased = false;
            trainingRoot.SetActive(false);
            if (jaksa != null) jaksa.Street.Root.SetActive(false);
            hi = new LandmarkDrive(transform, JakartaPlace.BundaranHI, forwardMissionTraffic: true);
            hi.Reset(mode);
            hiObjective = new HiRoundaboutObjective(hi);
            hiObjective.Reset();
            hiActive = true; hiResultRecorded = false;
            hiRecord = ReadHiRecord(); hiRecord.attempts++; StoreHiRecord();
            progress.tooltip = "Jeda / kemajuan [Tab]";
            root.Q<Label>(className: "controls").text = "";
            EnterHumanDrive(Page.Jaksa);
            Show(root.Q("steps"), false); Show(phone, false); Show(toast, false); Show(root.Q("pickup"), false);
        }
        void StepHi(float dt, bool throttle, bool brake, float steer, bool reverse)
        {
            if (HiEnded) return;
            hi.Step(dt, throttle, brake, steer, reverse);
            hiObjective.Step(dt);
            if (!HiEnded) return;
            if (!hiResultRecorded)
            {
                if (hiObjective.Complete) hiRecord.completions++; else hiRecord.failures++;
                StoreHiRecord(); hiResultRecorded = true;
            }
            ShowHiProgress();
        }
        void RefreshHiHud()
        {
            location.text = "BUNDARAN HI / JAKARTA PUSAT";
            title.text = HiTitle;
            instruction.text = hiObjective.Instruction;
            detail.text = hiObjective.Title;
            routeCount.text = "SUDIRMAN → BUNDARAN HI → THAMRIN";
            speed.text = (hi.Speed < -.05f ? "R " : "") + Mathf.RoundToInt(Mathf.Abs(hi.Speed) * 3.6f).ToString("00");
            speed.EnableInClassList("speed-alert", false);
            bool blink = Time.unscaledTime % .7f < .42f;
            signalLeft.On = hi.Indicator < 0 && blink; signalRight.On = hi.Indicator > 0 && blink;
            headlight.On = mode == VehicleMode.Motorcycle && hi.Headlight;
            Show(root.Q("practiceHint"), false); Show(root.Q("toggleMissions"), false);
            Show(toast, Time.unscaledTime < toastUntil);
        }
        void ShowHiProgress()
        {
            SetPage(Page.JaksaPause);
            var content = ScrollDialog();
            AddLabel(content, hiObjective.Complete ? "MISI SELESAI" : hiObjective.Failed ? "COBA LAGI" : "JEDA PERJALANAN", "kicker");
            AddLabel(content, HiTitle, "heading");
            AddLabel(content, hiObjective.Complete ? "Tujuan tercapai dengan selamat." : hiObjective.Failed ? hiObjective.FailureReason : hiObjective.Instruction, "copy");
            AddLabel(content, "Percobaan: " + hiRecord.attempts + " / Selesai: " + hiRecord.completions + " / Percobaan belum berhasil: " + hiRecord.failures, "copy");
            var note = Element(content, "note");
            AddLabel(note, "FOKUS LATIHAN", "kicker"); AddLabel(note, HiFocus, "copy");
            AddLabel(content, "Hasil tersimpan terpisah dari perjalanan utama. Percobaan yang ditinggalkan dimulai dari awal.", "objective-detail");
            AddControlsHelp(content);
            var actions = Element(content, "row actions");
            if (!HiEnded) AddButton(actions, "Lanjutkan", () => EnterHumanDrive(Page.Jaksa), "primary");
            AddButton(actions, "Ulangi misi", () => { ResetExplore(); EnterHumanDrive(Page.Jaksa); }, HiEnded ? "primary" : "quiet");
            AddButton(actions, "Pilih misi", LeaveHiForCatalogue, "quiet");
            AddButton(actions, "Menu utama", BackToMenu, "quiet");
        }
        void LeaveHiForCatalogue()
        {
            CloseHi(); trainingRoot.SetActive(true); district.RestoreAtmosphere();
            ShowCatalogue(MissionKind.Scenario);
        }
        void CloseHi()
        {
            hiActive = false;
            if (hi == null) return;
            hi.Environment.Water.SetAudio(0, false);
            hi.Mirrors.Dispose(); hi.Environment.Root.SetActive(false);
            if (Application.isPlaying) Destroy(hi.Environment.Root); else DestroyImmediate(hi.Environment.Root);
            hi = null; hiObjective = null;
        }
        void ResetExplore()
        {
            if (!hiActive) { jaksa.Reset(mode); return; }
            hi.Reset(mode); hiObjective.Reset(); hiResultRecorded = false;
            hiRecord.attempts++; StoreHiRecord();
        }
        void ToggleExploreCamera() { if (hiActive) hi.ToggleCamera(); else jaksa.ToggleCamera(); }
        void ToggleExploreIndicator(int value) { if (hiActive) hi.ToggleIndicator(value); else jaksa.ToggleIndicator(value); }
        void ToggleExploreHeadlight() { if (hiActive) hi.ToggleHeadlight(); else jaksa.ToggleHeadlight(); }
    }
}

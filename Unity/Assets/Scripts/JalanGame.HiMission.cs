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
        static string HiTitle => GameText.Get("hi.title");
        static string HiFocus => GameText.Get("hi.focus");
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
            AddLabel(row, GameText.Get("hi.duration"), "kicker duration");
            AddLabel(card, HiFocus, "copy");
            AddLabel(card, saved.attempts == 0 ? GameText.Get("text.not_attempted") : GameText.Get("hi.saved_status", saved.attempts, saved.completions), "objective-detail");
            AddButton(card, GameText.Get("text.select_2a7802"), ShowHiBrief, "primary");
        }
        void ShowHiBrief()
        {
            catalogueKind = MissionKind.Scenario;
            SetPage(Page.Brief);
            AddLabel(dialog, GameText.Get("hi.kicker"), "kicker");
            AddLabel(dialog, HiTitle, "heading");
            AddLabel(dialog, GameText.Get("hi.brief"), "copy");
            var note = Element(dialog, "note");
            AddLabel(note, GameText.Get("text.practice_focus"), "kicker");
            AddLabel(note, HiFocus, "copy");
            AddLabel(dialog, GameText.Get("hi.brief_detail"), "copy");
            var actions = Element(dialog, "row actions");
            AddButton(actions, GameText.Get("text.start"), StartHiMission, "primary");
            AddButton(actions, GameText.Get("text.back"), () => ShowCatalogue(MissionKind.Scenario), "quiet");
        }
        void StartHiMission()
        {
            CloseCityPreview();
            pitchDemo = false; learner = null; practiceStart = -1;
            demoTutorial = null;
            trainingRoot.SetActive(false);
            if (jaksa != null) jaksa.Street.Root.SetActive(false);
            hi = new LandmarkDrive(transform, JakartaPlace.BundaranHI, forwardMissionTraffic: true);
            hi.Reset(mode);
            hiObjective = new HiRoundaboutObjective(hi);
            hiObjective.Reset();
            hiActive = true; hiResultRecorded = false;
            hiRecord = ReadHiRecord(); hiRecord.attempts++; StoreHiRecord();
            progress.tooltip = GameText.Get("text.progress_tab");
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
            location.text = GameText.Get("hi.location");
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
            AddLabel(content, hiObjective.Complete ? GameText.Get("hi.completed") : hiObjective.Failed ? GameText.Get("hi.try_again") : GameText.Get("hi.paused"), "kicker");
            AddLabel(content, HiTitle, "heading");
            AddLabel(content, hiObjective.Complete ? GameText.Get("hi.success") : hiObjective.Failed ? hiObjective.FailureReason : hiObjective.Instruction, "copy");
            AddLabel(content, GameText.Get("hi.result_counts", hiRecord.attempts, hiRecord.completions, hiRecord.failures), "copy");
            var note = Element(content, "note");
            AddLabel(note, GameText.Get("text.practice_focus"), "kicker"); AddLabel(note, HiFocus, "copy");
            AddLabel(content, GameText.Get("hi.save_note"), "objective-detail");
            AddControlsHelp(content);
            var actions = Element(content, "row actions");
            if (!HiEnded) AddButton(actions, GameText.Get("text.continue"), () => EnterHumanDrive(Page.Jaksa), "primary");
            AddButton(actions, GameText.Get("hi.retry"), () => { ResetExplore(); EnterHumanDrive(Page.Jaksa); }, HiEnded ? "primary" : "quiet");
            AddButton(actions, GameText.Get("hi.choose"), LeaveHiForCatalogue, "quiet");
            AddButton(actions, GameText.Get("text.main_menu"), BackToMenu, "quiet");
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

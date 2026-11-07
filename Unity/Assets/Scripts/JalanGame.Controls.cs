using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace JalanKita
{
    public sealed partial class JalanGame
    {
        readonly HashSet<string> controlsSeen = new();
        readonly HashSet<string> cueActionsSeen = new();
        Page controlsOrigin, controlsDrivingPage;
        bool inputReleaseRequired;
        static readonly KeyCode[] DrivingInputKeys =
        {
            KeyCode.W, KeyCode.A, KeyCode.S, KeyCode.D, KeyCode.UpArrow, KeyCode.DownArrow,
            KeyCode.LeftArrow, KeyCode.RightArrow, KeyCode.Space, KeyCode.Q, KeyCode.E,
            KeyCode.C, KeyCode.L, KeyCode.R, KeyCode.H, KeyCode.F, KeyCode.G, KeyCode.Z,
            KeyCode.X, KeyCode.Return, KeyCode.KeypadEnter, KeyCode.F1, KeyCode.F2,
            KeyCode.Tab, KeyCode.Escape, KeyCode.P
        };

        void EnterHumanDrive(Page drivingPage)
        {
            SetPage(drivingPage);
            inputReleaseRequired = true;
        }

        void ShowControls(Page origin, Page drivingPage, bool firstEntry)
        {
            controlsOrigin = origin;
            controlsDrivingPage = drivingPage;
            SetPage(Page.Controls);
            AddLabel(dialog, mode == VehicleMode.Car ? "Kontrol mobil" : "Kontrol motor", "heading");
            AddLabel(dialog, learner != null ? "Contoh otomatis dijeda. Tutup panduan untuk melanjutkan contoh."
                : "Permainan tetap dijeda selama panduan ini terbuka.", "copy controls-intro");
            var body = new ScrollView(ScrollViewMode.Vertical)
            { horizontalScrollerVisibility = ScrollerVisibility.Hidden };
            body.AddToClassList("controls-body");
            dialog.Add(body);
            var groups = Element(body, "control-groups");
            var movement = Element(groups, "control-group");
            AddLabel(movement, "Bergerak & berhenti", "subhead");
            ControlKey(movement, "W / ↑", "Gas", "Tahan untuk menambah kecepatan.", DrivingGlyph.Kind.Throttle);
            ControlKey(movement, "S / ↓ / SPASI", "Rem", "Tahan untuk memperlambat atau berhenti.");
            ControlKey(movement, "A / D", "Kemudi kiri / kanan", "Bisa juga memakai panah ← / →.", DrivingGlyph.Kind.Steering);
            ControlKey(movement, "Z / X", "Lihat kiri / kanan", "Tahan untuk melihat. Lepas untuk kembali ke depan.");
            var decisions = Element(groups, "control-group");
            AddLabel(decisions, "Isyarat & pandangan", "subhead");
            ControlKey(decisions, "Q / E", "Sein kiri / kanan", "Tekan sekali. Tekan tombol yang sama untuk mematikan.");
            ControlKey(decisions, "C", "Ganti kamera", "Beralih antara kamera mengikuti dan pandangan pengemudi.", DrivingGlyph.Kind.Camera);
            if (mode == VehicleMode.Motorcycle)
                ControlKey(decisions, "L", "Lampu utama motor", "Tekan untuk menyalakan atau mematikan.");
            ControlKey(decisions, "ESC / TAB", "Jeda", "Buka kemajuan dan pilihan perjalanan.");
            Fold(body, "Kontrol lainnya", extra =>
            {
                ControlKey(extra, "R", drivingPage == Page.Jaksa ? "Ulangi ruas jalan" : "Ulangi dari checkpoint", "Mulai ulang bagian yang sedang dijalani.");
                ControlKey(extra, "H", "Klakson", "Tahan untuk membunyikan.");
                ControlKey(extra, "M", "Suara", "Tekan untuk mematikan atau mengaktifkan suara.");
                if (drivingPage == Page.Drive)
                    ControlKey(extra, "F / G", journey.IsMarket ? "Jemput / telepon" : "Telepon",
                        journey.IsMarket ? "F untuk menjemput saat berhenti di teluk. G untuk menjawab panggilan."
                        : "F mengabaikan panggilan. G menjawab; berhenti di tempat aman lebih dulu.");
                AddLabel(extra, "Di layar pelanggaran: R mencoba lagi, D menampilkan contoh cara benar.", "control-note");
            });
            var footer = Element(dialog, "controls-footer");
            string action = origin == Page.Progress || origin == Page.JaksaPause ? "Kembali ke jeda [Enter]"
                : origin == Page.Options ? "Kembali ke Options [Enter]"
                : origin == Page.PlayChooser ? "Kembali ke pilihan perjalanan [Enter]"
                : origin == Page.Menu ? "Kembali ke menu [Enter]"
                : learner != null ? "Lanjutkan contoh [Enter]" : "Kembali berkendara [Enter]";
            AddButton(footer, action, () => CloseControls(true), "primary tutorial-start");
            AddLabel(footer, "F1 membuka panduan saat berkendara. Esc menutup panduan.", "control-note");
        }

        static void ControlKey(VisualElement parent, string key, string title, string note, DrivingGlyph.Kind? glyph = null)
        {
            var row = Element(parent, "control-row");
            AddLabel(row, key, "keycap");
            var description = Element(row, "control-description");
            var heading = Element(description, "row");
            if (glyph.HasValue) heading.Add(new DrivingGlyph(glyph.Value));
            AddLabel(heading, title, "control-title");
            AddLabel(description, note, "control-note");
        }

        void CloseControls(bool accepted)
        {
            if (page != Page.Controls) return;
            inputReleaseRequired = false;
            if (controlsOrigin == Page.Progress) ShowProgress();
            else if (controlsOrigin == Page.JaksaPause) ShowJaksaPause();
            else if (controlsOrigin == Page.Menu) ShowMenu();
            else if (controlsOrigin == Page.PlayChooser) ShowPlayChooser();
            else if (controlsOrigin == Page.Options) ShowOptions();
            else
            {
                SetPage(controlsOrigin);
                inputReleaseRequired = true;
            }
        }

        bool DrivingInputsReleased()
        {
            if (!inputReleaseRequired) return true;
            foreach (var key in DrivingInputKeys) if (Input.GetKey(key)) return false;
            if (Input.GetMouseButton(0)) return false;
            inputReleaseRequired = false;
            return true;
        }

        void RefreshProgressiveCue()
        {
            var cue = root.Q("progressiveCue");
            Show(cue, false);
            if ((page != Page.Drive && page != Page.Jaksa) || learner != null
                || inputReleaseRequired || !Application.isFocused) return;
            if (!root.Q("hudHint").ClassListContains("hidden")) return;
            string context = (page == Page.Jaksa ? "explore:" : "training:") + mode;
            float drivingSpeed = page == Page.Jaksa ? jaksa.Speed : journey.Speed;
            if (!controlsSeen.Contains(context))
            {
                if (drivingSpeed > .2f && Pressed(KeyCode.W, KeyCode.UpArrow)) controlsSeen.Add(context);
                else { PresentCue("W A S D", "Coba gas dan kemudi."); return; }
            }
            if (page == Page.Jaksa || journey.Finished || journey.Failure.HasValue) return;
            float front = journey.Front, z = journey.Position.y;
            int stage = journey.Stage;
            string brake = null;
            bool Near(float line) => front >= line - 25 && front < line + 1;
            if (stage == 1 && !journey.FullStop && Near(RouteStage.StopLine)) brake = "stop";
            else if (stage == 1 && journey.LightPhase != 2 && Near(RouteStage.LightLine)) brake = "light";
            else if ((stage == 2 || journey.IsSchool) && journey.PedestrianBlocksPath && Near(journey.CrossingZebra - 2)) brake = "pedestrian";
            else if (stage == 2 && journey.CrossTraffic.HasValue && Near(RouteStage.YieldLine)) brake = "yield";
            else if (stage >= 5 && stage <= 7 && (!journey.MovementGreen || journey.BoulevardPedestrianBlocksPath))
            {
                float along = Boulevard.Local(stage, journey.Nose).y;
                if (along >= journey.JunctionStopLine - 25 && along < journey.JunctionStopLine + 1) brake = "junction";
            }
            else if (stage == 8 && journey.RailWarning && Near(Boulevard.RailLine)) brake = "rail";
            if (brake != null && drivingSpeed > .28f)
            {
                bool braking = Input.GetKey(KeyCode.Space) || Pressed(KeyCode.S, KeyCode.DownArrow);
                Suggest("brake:" + brake, "SPASI / S", "Rem sebelum area berhenti.", braking);
                return;
            }
            int direction = 0;
            if (stage == 3 && !journey.IsMarket && z >= RouteStage.OneWayStart - 12 && z < RouteStage.Merge && !journey.ReturnedLeft)
                direction = journey.ChangedRight ? -1 : 1;
            else if (stage == 6 || stage == 7)
            {
                float along = Boulevard.Local(stage, journey.Position).y;
                if (along >= -70 && along < -11) direction = stage == 6 ? -1 : 1;
            }
            else if (journey.IsMarket && z >= Mission.PickupStart - 15 && z < Mission.PickupEnd)
                direction = journey.PassengerPickedUp ? 1 : journey.Position.x >= -4.25f ? -1 : 0;
            else if (stage == 4 && journey.PhoneRinging && journey.Position.x >= -4.25f && z < RouteStage.PhoneEnd)
                direction = -1;
            if (direction != 0)
                Suggest("indicator:" + direction, direction < 0 ? "Q" : "E",
                    direction < 0 ? "Sein kiri sebelum bermanuver." : "Sein kanan sebelum bermanuver.", journey.Indicator == direction);

            void Suggest(string action, string keys, string copy, bool performed)
            {
                string id = context + ":" + stage + ":" + journey.Save.checkpoint + ":" + action;
                if (performed) cueActionsSeen.Add(id);
                if (!cueActionsSeen.Contains(id)) PresentCue(keys, copy);
            }
        }

        void PresentCue(string keys, string copy)
        {
            root.Q<Label>("cueKeys").text = keys;
            root.Q<Label>("cueCopy").text = copy;
            Show(root.Q("progressiveCue"), true);
        }

        void AddControlsHelp(VisualElement parent)
        {
            AddButton(parent, "Kontrol berkendara [F1]", () => ShowControls(page,
                page == Page.Jaksa || page == Page.JaksaPause ? Page.Jaksa : Page.Drive, false), "quiet help-action");
        }
    }
}

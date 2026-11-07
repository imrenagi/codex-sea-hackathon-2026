using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace JalanKita
{
    public sealed partial class JalanGame
    {
        readonly HashSet<string> controlsSeen = new();
        Page controlsOrigin, controlsDrivingPage;
        bool controlsFirstEntry, inputReleaseRequired;
        string controlsContext;
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
            string context = (drivingPage == Page.Jaksa ? "explore:" : "training:") + mode;
            if (controlsSeen.Contains(context))
            {
                SetPage(drivingPage);
                inputReleaseRequired = true;
            }
            else ShowControls(page, drivingPage, true);
        }

        void ShowControls(Page origin, Page drivingPage, bool firstEntry)
        {
            controlsOrigin = origin;
            controlsDrivingPage = drivingPage;
            controlsFirstEntry = firstEntry;
            controlsContext = (drivingPage == Page.Jaksa ? "explore:" : "training:") + mode;
            SetPage(Page.Controls);
            AddLabel(dialog, mode == VehicleMode.Car ? "Kontrol mobil" : "Kontrol motor", "heading");
            AddLabel(dialog, firstEntry ? "Permainan dijeda. Kenali tombolnya, lalu mulai saat siap."
                : learner != null ? "Contoh otomatis dijeda. Tutup panduan untuk melanjutkan contoh."
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
            string action = firstEntry ? "Siap, mulai [Enter]"
                : origin == Page.Progress || origin == Page.JaksaPause ? "Kembali ke jeda [Enter]"
                : origin == Page.Options ? "Kembali ke Options [Enter]"
                : origin == Page.PlayChooser ? "Kembali ke pilihan perjalanan [Enter]"
                : origin == Page.Menu ? "Kembali ke menu [Enter]"
                : learner != null ? "Lanjutkan contoh [Enter]" : "Kembali berkendara [Enter]";
            AddButton(footer, action, () => CloseControls(true), "primary tutorial-start");
            if (firstEntry) AddButton(footer, "Menu utama [Esc]", () => CloseControls(false), "quiet");
            AddLabel(footer, firstEntry ? "Progres perjalanan tidak diubah oleh panduan ini."
                : "F1 membuka panduan saat berkendara. Esc menutup panduan.", "control-note");
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
            if (controlsFirstEntry)
            {
                if (!accepted) { inputReleaseRequired = false; ShowMenu(); return; }
                controlsSeen.Add(controlsContext);
                SetPage(controlsDrivingPage);
                inputReleaseRequired = true;
                return;
            }
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

        void AddControlsHelp(VisualElement parent)
        {
            AddButton(parent, "Kontrol berkendara [F1]", () => ShowControls(page,
                page == Page.Jaksa || page == Page.JaksaPause ? Page.Jaksa : Page.Drive, false), "quiet help-action");
        }
    }
}

using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace JalanKita
{
    public sealed partial class JalanGame
    {
        VehiclePreview vehiclePreview;
        Slider previewAngle;
        Label previewName, previewDescription;
        Button previewAuto;

        void ShowVehiclePreview(bool visible)
        {
            var panel = root.Q("vehicleShowroom");
            Show(panel, visible);
            overlay.EnableInClassList("showroom", visible);
            vehiclePreview?.SetVisible(visible);
            if (!visible) return;
            vehiclePreview ??= new VehiclePreview(transform);
            vehiclePreview.SetVisible(true);
            panel.Clear();
            AddLabel(panel, "PILIH KENDARAAN", "kicker");
            previewName = AddLabel(panel, "", "subhead section");
            var image = new Image { name = "vehiclePreview", image = vehiclePreview.Texture, scaleMode = ScaleMode.ScaleToFit, focusable = true };
            image.AddToClassList("vehicle-preview");
            image.AddManipulator(new VehicleRotationDrag(vehiclePreview));
            image.RegisterCallback<KeyDownEvent>(e =>
            {
                if (e.keyCode != KeyCode.LeftArrow && e.keyCode != KeyCode.RightArrow) return;
                vehiclePreview.Rotate(e.keyCode == KeyCode.LeftArrow ? -15 : 15); e.StopPropagation();
            });
            panel.Add(image);
            AddLabel(panel, "Geser kendaraan untuk melihat semua sisi", "preview-hint");
            previewAngle = new Slider("Sudut", 0, 360) { name = "vehicleAngle" };
            previewAngle.AddToClassList("volume-control preview-angle");
            previewAngle.RegisterValueChangedCallback(e => vehiclePreview.SetAngle(e.newValue));
            panel.Add(previewAngle);
            var controls = Element(panel, "row preview-controls");
            AddButton(controls, "Putar kiri", () => vehiclePreview.Rotate(-45), "quiet tiny");
            previewAuto = AddButton(controls, "", () => vehiclePreview.AutoRotate = !vehiclePreview.AutoRotate, "quiet tiny");
            previewAuto.name = "vehicleAutoRotate";
            AddButton(controls, "Putar kanan", () => vehiclePreview.Rotate(45), "quiet tiny");
            var choices = Element(panel, "vehicle-choices");
            foreach (var choice in VehicleCatalogue.All.Where(v => v.Mode == mode))
            {
                var button = AddButton(choices, choice.Label, () =>
                {
                    VehicleCatalogue.Select(choice);
                    RefreshVehiclePreview(0);
                }, "vehicle-choice quiet");
                button.name = "vehicle-" + choice.Id; button.userData = choice;
                button.tooltip = choice.Description;
            }
            previewDescription = AddLabel(panel, "", "objective-detail");
            RefreshVehiclePreview(0);
        }

        void RefreshVehiclePreview(float dt)
        {
            if (vehiclePreview == null || !vehiclePreview.Visible) return;
            vehiclePreview.Present(mode, dt);
            var selected = VehicleCatalogue.Selected(mode);
            previewName.text = selected.Label; previewDescription.text = selected.Description;
            previewAngle.SetValueWithoutNotify(vehiclePreview.Angle);
            previewAuto.text = vehiclePreview.AutoRotate ? "Jeda putar" : "Putar otomatis";
            foreach (var choice in root.Q("vehicleShowroom").Query<Button>(className: "vehicle-choice").ToList())
            {
                bool active = ReferenceEquals(choice.userData, selected);
                choice.EnableInClassList("selected", active);
                choice.EnableInClassList("quiet", !active);
            }
        }

        sealed class VehicleRotationDrag : PointerManipulator
        {
            readonly VehiclePreview preview;
            int pointer = -1;
            float previous;
            public VehicleRotationDrag(VehiclePreview preview) { this.preview = preview; }
            protected override void RegisterCallbacksOnTarget()
            {
                target.RegisterCallback<PointerDownEvent>(Down); target.RegisterCallback<PointerMoveEvent>(Move);
                target.RegisterCallback<PointerUpEvent>(Up); target.RegisterCallback<PointerCaptureOutEvent>(Cancel);
                target.RegisterCallback<DetachFromPanelEvent>(Detach);
            }
            protected override void UnregisterCallbacksFromTarget()
            {
                target.UnregisterCallback<PointerDownEvent>(Down); target.UnregisterCallback<PointerMoveEvent>(Move);
                target.UnregisterCallback<PointerUpEvent>(Up); target.UnregisterCallback<PointerCaptureOutEvent>(Cancel);
                target.UnregisterCallback<DetachFromPanelEvent>(Detach);
            }
            void Down(PointerDownEvent e)
            {
                if (e.button != 0 || pointer >= 0) return;
                pointer = e.pointerId; previous = e.position.x; preview.AutoRotate = false;
                target.CapturePointer(pointer); target.Focus(); e.StopPropagation();
            }
            void Move(PointerMoveEvent e)
            {
                if (e.pointerId != pointer || !target.HasPointerCapture(pointer)) return;
                preview.Rotate((e.position.x - previous) * .6f); previous = e.position.x; e.StopPropagation();
            }
            void Up(PointerUpEvent e) { if (e.pointerId == pointer) { target.ReleasePointer(pointer); pointer = -1; e.StopPropagation(); } }
            void Cancel(PointerCaptureOutEvent e) { pointer = -1; }
            void Detach(DetachFromPanelEvent e) { if (pointer >= 0) target.ReleasePointer(pointer); pointer = -1; }
        }
    }
}

using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace JalanKita
{
    public sealed partial class JalanGame
    {
        VehiclePreview vehiclePreview;
        Slider previewAngle;
        Label previewDescription;

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
            AddLabel(panel, GameText.Get("vehicles.preview.title"), "kicker");
            var image = new Image { name = "vehiclePreview", image = vehiclePreview.Texture, scaleMode = ScaleMode.ScaleToFit, focusable = true };
            image.AddToClassList("vehicle-preview");
            image.AddManipulator(new VehicleRotationDrag(vehiclePreview));
            image.RegisterCallback<KeyDownEvent>(e =>
            {
                if (e.keyCode != KeyCode.LeftArrow && e.keyCode != KeyCode.RightArrow) return;
                vehiclePreview.Rotate(e.keyCode == KeyCode.LeftArrow ? -15 : 15); e.StopPropagation();
            });
            image.RegisterCallback<NavigationMoveEvent>(e =>
            {
                // Arrow keys rotate this focused image; their parallel navigation
                // event must not move focus to a different menu control.
                if (e.direction == NavigationMoveEvent.Direction.Left || e.direction == NavigationMoveEvent.Direction.Right)
                    ConsumeDrivingKey(e);
            });
            panel.Add(image);
            // Model selection stays beside its preview; drag or arrow keys control rotation.
            var choices = Element(panel, "vehicle-choices");
            foreach (var choice in VehicleCatalogue.All.Where(v => v.Mode == mode))
            {
                var button = AddButton(choices, choice.Label, () =>
                {
                    VehicleCatalogue.Select(choice);
                    RefreshVehiclePreview(0);
                }, "vehicle-choice vehicle-thumbnail-choice quiet");
                // Keep the native button's text/tooltip as its accessible name, while showing only art.
                button.style.fontSize = 0;
                button.name = "vehicle-" + choice.Id; button.userData = choice;
                button.tooltip = choice.Label + " · " + choice.Description;
                var thumbnail = Resources.Load<Texture2D>("UI/VehicleThumbnails/" + choice.Id);
                if (thumbnail)
                {
                    var sideView = new Image { image = thumbnail, scaleMode = ScaleMode.ScaleToFit,
                        pickingMode = PickingMode.Ignore };
                    sideView.AddToClassList("vehicle-choice-image");
                    button.Add(sideView);
                }
                else
                {
                    // Never display an empty target if an optional image is not installed yet.
                    button.Add(new DrivingGlyph(choice.Mode == VehicleMode.Car
                        ? DrivingGlyph.Kind.Steering : DrivingGlyph.Kind.Motorcycle));
                }
            }
            previewDescription = AddLabel(panel, "", "objective-detail");
            AddLabel(panel, GameText.Get("vehicles.preview.drag"), "preview-hint");
            previewAngle = new Slider(GameText.Get("vehicles.preview.angle"), 0, 360) { name = "vehicleAngle" };
            previewAngle.AddToClassList("volume-control preview-angle");
            previewAngle.RegisterValueChangedCallback(e => vehiclePreview.SetAngle(e.newValue));
            panel.Add(previewAngle);
            RefreshVehiclePreview(0);
        }

        void RefreshVehiclePreview(float dt)
        {
            if (vehiclePreview == null || !vehiclePreview.Visible) return;
            vehiclePreview.Present(mode, dt);
            var selected = VehicleCatalogue.Selected(mode);
            previewDescription.text = selected.Description;
            root.Q<Image>("vehiclePreview").tooltip = selected.Label + " · " + selected.Description;
            previewAngle.SetValueWithoutNotify(vehiclePreview.Angle);
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
                preview.Rotate((previous - e.position.x) * .6f); previous = e.position.x; e.StopPropagation();
            }
            void Up(PointerUpEvent e) { if (e.pointerId == pointer) { target.ReleasePointer(pointer); pointer = -1; e.StopPropagation(); } }
            void Cancel(PointerCaptureOutEvent e) { pointer = -1; }
            void Detach(DetachFromPanelEvent e) { if (pointer >= 0) target.ReleasePointer(pointer); pointer = -1; }
        }
    }
}

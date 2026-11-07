using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace JalanKita
{
    public sealed partial class JalanGame
    {
        void BindHudControls()
        {
            BindHudIcon("steeringHelp", DrivingGlyph.Kind.Steering, () => ShowControls(page, page, false));
            BindHudIcon("throttleHelp", DrivingGlyph.Kind.Throttle, () => ShowControls(page, page, false));
            BindHudIcon("cameraControl", DrivingGlyph.Kind.Camera, () =>
            {
                if (inputReleaseRequired || learner != null) return;
                if (page == Page.Jaksa) jaksa.ToggleCamera();
                else if (page == Page.Drive) { hood = !hood; district.Snap(); }
            });
            BindHudIcon("lampControl", DrivingGlyph.Kind.Lamp, () =>
            {
                if (inputReleaseRequired || learner != null || mode != VehicleMode.Motorcycle) return;
                if (page == Page.Jaksa) jaksa.ToggleHeadlight();
                else if (page == Page.Drive) journey.ToggleHeadlight();
            });
            BindHudIcon("lookLeft", DrivingGlyph.Kind.Left);
            BindHudIcon("lookRight", DrivingGlyph.Kind.Right);
            BindHudIcon("progress", DrivingGlyph.Kind.Pause);
        }

        void BindHudIcon(string id, DrivingGlyph.Kind kind, Action action = null)
        {
            var button = root.Q<Button>(id);
            button.text = "";
            button.Add(new DrivingGlyph(kind));
            if (action != null) button.clicked += action;
            void Hint(bool visible)
            {
                var hint = root.Q<Label>("hudHint");
                hint.text = button.tooltip;
                Show(hint, visible);
            }
            button.RegisterCallback<PointerEnterEvent>(_ => Hint(true));
            button.RegisterCallback<PointerLeaveEvent>(_ => Hint(button.focusController?.focusedElement == button));
            button.RegisterCallback<FocusInEvent>(_ => Hint(true));
            button.RegisterCallback<FocusOutEvent>(_ => Hint(false));
        }

        void RefreshHudControls()
        {
            Show(root.Q("lampControl"), mode == VehicleMode.Motorcycle && learner == null);
            Show(root.Q("cameraControl"), learner == null);
            Show(root.Q("throttleHelp"), learner == null);
            Show(root.Q("lookLeft"), learner == null);
            Show(root.Q("lookRight"), learner == null);
            Show(headlight, mode == VehicleMode.Motorcycle);
            Show(root.Q("hudHint"), false);
            if (page == Page.Jaksa) Show(root.Q("signalPanel"), false);
        }

        // Painter2D keeps every glyph independent of font coverage and display density.
        sealed class DrivingGlyph : VisualElement
        {
            public enum Kind { Steering, Throttle, Camera, Lamp, Left, Right, Pause }
            readonly Kind kind;
            public DrivingGlyph(Kind kind)
            {
                this.kind = kind;
                pickingMode = PickingMode.Ignore;
                AddToClassList("driving-glyph");
                generateVisualContent += Draw;
            }

            void Draw(MeshGenerationContext context)
            {
                float w = contentRect.width, h = contentRect.height;
                if (w < 1 || h < 1) return;
                var pen = context.painter2D;
                pen.strokeColor = resolvedStyle.color;
                pen.fillColor = resolvedStyle.color;
                pen.lineWidth = 2.3f;
                pen.lineCap = LineCap.Round;
                Vector2 P(float x, float y) => new Vector2(x * w, y * h);
                void Line(float x, float y, float a, float b)
                {
                    pen.BeginPath(); pen.MoveTo(P(x, y)); pen.LineTo(P(a, b)); pen.Stroke();
                }
                void Circle(float x, float y, float radius)
                {
                    pen.BeginPath(); pen.Arc(P(x, y), radius * Mathf.Min(w, h), 0f, 360f); pen.Stroke();
                }
                switch (kind)
                {
                    case Kind.Steering:
                        Circle(.5f, .5f, .37f);
                        Circle(.5f, .5f, .09f);
                        Line(.14f, .43f, .42f, .5f); Line(.86f, .43f, .58f, .5f); Line(.5f, .59f, .5f, .87f);
                        break;
                    case Kind.Throttle:
                        pen.BeginPath(); pen.MoveTo(P(.57f, .08f)); pen.LineTo(P(.23f, .55f));
                        pen.LineTo(P(.48f, .55f)); pen.LineTo(P(.4f, .93f)); pen.LineTo(P(.8f, .39f));
                        pen.LineTo(P(.56f, .39f)); pen.ClosePath(); pen.Fill();
                        break;
                    case Kind.Camera:
                        pen.BeginPath(); pen.MoveTo(P(.12f, .3f)); pen.LineTo(P(.33f, .3f));
                        pen.LineTo(P(.4f, .19f)); pen.LineTo(P(.62f, .19f)); pen.LineTo(P(.69f, .3f));
                        pen.LineTo(P(.88f, .3f)); pen.LineTo(P(.88f, .8f)); pen.LineTo(P(.12f, .8f));
                        pen.ClosePath(); pen.Stroke(); Circle(.5f, .55f, .17f);
                        break;
                    case Kind.Lamp:
                        pen.BeginPath(); pen.Arc(P(.57f, .5f), .31f * Mathf.Min(w, h), 270f, 450f);
                        pen.ClosePath(); pen.Stroke();
                        for (int i = -1; i <= 1; i++) Line(.12f, .5f + i * .22f, .4f, .5f + i * .22f);
                        break;
                    case Kind.Left:
                    case Kind.Right:
                        float d = kind == Kind.Left ? -1 : 1;
                        Line(.5f + d * .31f, .5f, .5f - d * .31f, .5f);
                        Line(.5f + d * .31f, .5f, .5f, .2f);
                        Line(.5f + d * .31f, .5f, .5f, .8f);
                        break;
                    case Kind.Pause:
                        pen.lineWidth = 5;
                        Line(.34f, .22f, .34f, .78f); Line(.66f, .22f, .66f, .78f);
                        break;
                }
            }
        }
    }
}

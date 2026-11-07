using UnityEngine;
using UnityEngine.UIElements;
using System;

namespace JalanKita
{
    public sealed partial class JalanGame
    {
        enum ActionKind { Retry, Back, Home, Play, Next, Check, Practice, Options, Sound, Muted, External, Mission, Credits, Exit, Gallery }
        static Button AddActionButton(VisualElement parent, ActionKind icon, string text, Action action, string classes)
        {
            var button = AddButton(parent, text, action, classes);
            button.AddToClassList("action-icon-button");
            button.style.paddingLeft = 40;
            button.style.paddingRight = 24;
            button.Add(new ActionGlyph(icon));
            return button;
        }
        static void SetActionIcon(Button button, ActionKind kind)
        {
            var icon = button?.Q<ActionGlyph>();
            if (icon == null) return;
            icon.Kind = kind;
            icon.MarkDirtyRepaint();
        }
        static void AddSelectionBadge(Button button, bool selected)
        {
            var badge = new ActionGlyph(ActionKind.Check) { name = "selectionCheck" };
            badge.style.left = StyleKeyword.Auto;
            badge.style.right = 10;
            badge.style.top = 10;
            badge.style.marginTop = 0;
            badge.style.width = 22;
            badge.style.height = 22;
            badge.style.color = new Color(1f, .81f, .3f);
            badge.style.backgroundColor = new Color(.06f, .09f, .13f, .94f);
            badge.style.borderTopLeftRadius = badge.style.borderTopRightRadius = 11;
            badge.style.borderBottomLeftRadius = badge.style.borderBottomRightRadius = 11;
            badge.style.display = selected ? DisplayStyle.Flex : DisplayStyle.None;
            button.Add(badge);
        }
        sealed class ActionGlyph : VisualElement
        {
            public ActionKind Kind;
            public ActionGlyph(ActionKind kind)
            {
                Kind = kind;
                pickingMode = PickingMode.Ignore;
                focusable = false;
                style.position = Position.Absolute;
                style.left = 12;
                style.top = Length.Percent(50);
                style.marginTop = -9;
                style.width = style.height = 18;
                generateVisualContent += Draw;
            }
            void Draw(MeshGenerationContext context)
            {
                float w = contentRect.width, h = contentRect.height;
                if (w < 1 || h < 1) return;
                var pen = context.painter2D;
                pen.strokeColor = resolvedStyle.color;
                pen.fillColor = resolvedStyle.color;
                pen.lineWidth = 1.7f;
                pen.lineCap = LineCap.Round;
                pen.lineJoin = LineJoin.Round;
                Vector2 P(float x, float y) => new Vector2(x*w, y*h);
                void Path(params float[] points)
                {
                    pen.BeginPath(); pen.MoveTo(P(points[0], points[1]));
                    for (int i=2; i<points.Length; i+=2) pen.LineTo(P(points[i], points[i+1]));
                    pen.Stroke();
                }
                void Circle(float radius)
                { pen.BeginPath(); pen.Arc(P(.5f,.5f), radius*Mathf.Min(w,h), 0f,360f); pen.Stroke(); }
                switch (Kind)
                {
                    case ActionKind.Retry:
                        pen.BeginPath(); pen.Arc(P(.5f,.5f), .32f*w, 225f, 520f); pen.Stroke();
                        Path(.14f,.14f,.14f,.4f,.4f,.4f); break;
                    case ActionKind.Back: Path(.65f,.2f,.35f,.5f,.65f,.8f); break;
                    case ActionKind.Home:
                        Path(.12f,.45f,.5f,.14f,.88f,.45f); Path(.23f,.4f,.23f,.84f,.77f,.84f,.77f,.4f);
                        Path(.42f,.84f,.42f,.6f,.58f,.6f,.58f,.84f); break;
                    case ActionKind.Play: Path(.3f,.18f,.78f,.5f,.3f,.82f,.3f,.18f); break;
                    case ActionKind.Next: Path(.15f,.5f,.84f,.5f); Path(.55f,.21f,.84f,.5f,.55f,.79f); break;
                    case ActionKind.Check: Path(.25f,.5f,.43f,.68f,.76f,.32f); break;
                    case ActionKind.Practice:
                        Path(.19f,.85f,.81f,.85f); Path(.28f,.8f,.44f,.14f,.56f,.14f,.72f,.8f);
                        Path(.38f,.4f,.62f,.4f); Path(.33f,.63f,.67f,.63f); break;
                    case ActionKind.Options:
                        Circle(.26f); Circle(.09f);
                        for(int i=0;i<8;i++) { float a=i*Mathf.PI/4; Path(.5f+Mathf.Cos(a)*.28f,.5f+Mathf.Sin(a)*.28f,.5f+Mathf.Cos(a)*.4f,.5f+Mathf.Sin(a)*.4f); }
                        break;
                    case ActionKind.Sound:
                    case ActionKind.Muted:
                        Path(.14f,.38f,.32f,.38f,.52f,.2f,.52f,.8f,.32f,.62f,.14f,.62f,.14f,.38f);
                        if (Kind == ActionKind.Muted) { Path(.65f,.36f,.88f,.64f); Path(.88f,.36f,.65f,.64f); }
                        else { pen.BeginPath(); pen.Arc(P(.49f,.5f), .34f*w, -50f,50f); pen.Stroke(); }
                        break;
                    case ActionKind.Mission:
                        Path(.24f,.88f,.24f,.14f);
                        Path(.24f,.17f,.79f,.17f,.66f,.35f,.79f,.53f,.24f,.53f);
                        break;
                    case ActionKind.Credits:
                        Circle(.37f);
                        Path(.5f,.43f,.5f,.7f);
                        Path(.5f,.28f,.5f,.29f);
                        break;
                    case ActionKind.Exit:
                        Path(.48f,.17f,.18f,.17f,.18f,.83f,.48f,.83f);
                        Path(.4f,.5f,.87f,.5f);
                        Path(.66f,.29f,.87f,.5f,.66f,.71f);
                        break;
                    case ActionKind.Gallery:
                        Path(.16f,.26f,.5f,.08f,.84f,.26f,.84f,.73f,.5f,.92f,.16f,.73f,.16f,.26f);
                        Path(.16f,.26f,.5f,.46f,.84f,.26f); Path(.5f,.46f,.5f,.92f); break;
                    case ActionKind.External:
                        Path(.43f,.2f,.18f,.2f,.18f,.82f,.8f,.82f,.8f,.57f);
                        Path(.48f,.52f,.84f,.16f); Path(.58f,.16f,.84f,.16f,.84f,.42f); break;
                }
            }
        }
    }
}

using UnityEngine;
using UnityEngine.UIElements;

namespace UjianSim.Game
{
    /// <summary>
    /// Indicator and headlight icons drawn as vector shapes in the element's USS colour, so the left and right arrows
    /// are exact mirror images (font glyphs like ◀ and ▶ differ in size between fonts).
    /// </summary>
    public class HudIcon : VisualElement
    {
        public enum Shape { Left, Right, Headlight }

        readonly Shape shape;
        bool on;

        public HudIcon(Shape shape)
        {
            this.shape = shape;
            AddToClassList("icon");
            if (shape == Shape.Headlight) AddToClassList("lamp");
            pickingMode = PickingMode.Ignore;
            generateVisualContent += Draw;
        }

        public bool On
        {
            set
            {
                if (on == value) return;
                on = value;
                EnableInClassList("on", value);
                MarkDirtyRepaint();
            }
        }

        void Draw(MeshGenerationContext ctx)
        {
            var g = ctx.painter2D;
            var r = contentRect;
            float w = r.width, h = r.height;
            if (w < 1f) return;
            var c = resolvedStyle.color;
            g.fillColor = c;
            g.strokeColor = c;
            if (shape == Shape.Headlight)
            {
                // A lamp (half disc) with three beams to its left.
                var centre = new Vector2(w * 0.68f, h * 0.5f);
                g.BeginPath();
                g.Arc(centre, h * 0.36f, 270f, 450f);
                g.ClosePath();
                g.Fill();
                g.lineWidth = 2.5f;
                g.lineCap = LineCap.Round;
                for (int i = -1; i <= 1; i++)
                {
                    g.BeginPath();
                    g.MoveTo(new Vector2(w * 0.5f, h * (0.5f + i * 0.24f)));
                    g.LineTo(new Vector2(w * 0.1f, h * (0.5f + i * 0.24f)));
                    g.Stroke();
                }
                return;
            }
            // Arrow: a triangular head and a short shaft, mirrored for the right indicator.
            float d = shape == Shape.Left ? -1f : 1f;
            Vector2 P(float x, float y) => new Vector2(w * (0.5f + d * (x - 0.5f)), h * y);
            g.BeginPath();
            g.MoveTo(P(0.98f, 0.5f));
            g.LineTo(P(0.52f, 0.08f));
            g.LineTo(P(0.52f, 0.3f));
            g.LineTo(P(0.04f, 0.3f));
            g.LineTo(P(0.04f, 0.7f));
            g.LineTo(P(0.52f, 0.7f));
            g.LineTo(P(0.52f, 0.92f));
            g.ClosePath();
            g.Fill();
        }
    }
}

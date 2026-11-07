using UnityEngine;
using UnityEngine.UIElements;

namespace JalanKita
{
    public sealed partial class JalanGame
    {
        void EnsureThumbnailStyles()
        {
            var sheet = Resources.Load<StyleSheet>("UI/SelectionThumbnails");
            if (sheet && !root.styleSheets.Contains(sheet)) root.styleSheets.Add(sheet);
        }

        void AddSelectionThumbnail(VisualElement parent, string asset, string caption)
        {
            EnsureThumbnailStyles();
            var frame = Element(parent, "selection-preview");
            frame.pickingMode = PickingMode.Ignore;
            var image = new Image
            {
                image = Resources.Load<Texture2D>("UI/Thumbnails/" + asset),
                scaleMode = ScaleMode.ScaleToFit,
                pickingMode = PickingMode.Ignore,
                tooltip = caption
            };
            image.AddToClassList("selection-preview-image");
            frame.Add(image);
            AddLabel(frame, caption, "selection-preview-caption").pickingMode = PickingMode.Ignore;
        }

        void AddTutorialThumbnail(VisualElement parent)
        {
            EnsureThumbnailStyles();
            string caption = GameText.Get("level.tutorial.start");
            // A real Button retains tab focus and keyboard submit, with one click target.
            var button = new Button(StartPitchDemo)
            {
                name = "tutorialThumbnail",
                tooltip = caption,
                focusable = true
            };
            button.AddToClassList("selection-preview");
            button.AddToClassList("thumbnail-play-button");
            parent.Add(button);
            var image = new Image
            {
                image = Resources.Load<Texture2D>("UI/Thumbnails/mission_tutorial"),
                scaleMode = ScaleMode.ScaleAndCrop,
                pickingMode = PickingMode.Ignore
            };
            image.AddToClassList("selection-preview-image");
            image.AddToClassList("thumbnail-play-image");
            button.Add(image);
            var overlay = Element(button, "thumbnail-play-overlay");
            overlay.pickingMode = PickingMode.Ignore;
            overlay.Add(new ThumbnailPlayGlyph());
            AddLabel(overlay, caption, "thumbnail-play-caption").pickingMode = PickingMode.Ignore;
        }

        // Draw the icon instead of relying on Unicode glyph availability in the live preview font.
        sealed class ThumbnailPlayGlyph : VisualElement
        {
            public ThumbnailPlayGlyph()
            {
                pickingMode = PickingMode.Ignore;
                AddToClassList("thumbnail-play-glyph");
                generateVisualContent += Draw;
            }
            void Draw(MeshGenerationContext context)
            {
                float w = contentRect.width, h = contentRect.height;
                if (w < 1 || h < 1) return;
                var pen = context.painter2D;
                pen.strokeColor = resolvedStyle.color;
                pen.fillColor = resolvedStyle.color;
                pen.lineWidth = 1.8f;
                pen.BeginPath();
                pen.Arc(new Vector2(w * .5f, h * .5f), Mathf.Min(w, h) * .43f, 0f, 360f);
                pen.Stroke();
                pen.BeginPath();
                pen.MoveTo(new Vector2(w * .42f, h * .32f));
                pen.LineTo(new Vector2(w * .68f, h * .5f));
                pen.LineTo(new Vector2(w * .42f, h * .68f));
                pen.ClosePath();
                pen.Fill();
            }
        }

        void AddCityMapChoices()
        {
            EnsureThumbnailStyles();
            dialog.AddToClassList("catalogue-card");
            var list = new ScrollView(ScrollViewMode.Vertical);
            list.verticalScrollerVisibility = ScrollerVisibility.AlwaysVisible;
            list.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
            list.AddToClassList("mission-list");
            list.AddToClassList("city-selection-list");
            dialog.Add(list);
            for (int i = 0; i < CityMapData.Ids.Length; i++)
            {
                int index = i;
                string id = CityMapData.Ids[i];
                var card = AddButton(list.contentContainer, "", () => OpenCityMap(index), "city-selection-card quiet");
                card.tooltip = GameText.Get("maps.open_preview", CityMapData.Names[i]);
                AddSelectionThumbnail(card, "city_" + id, GameText.Get("maps.thumbnail_caption"));
                AddLabel(card, CityMapData.Names[i], "subhead city-selection-title");
                AddLabel(card, GameText.Get("maps.summary." + id), "city-selection-description");
                AddLabel(card, GameText.Get("maps.view_preview"), "city-selection-action");
                foreach (var child in card.Query<VisualElement>().ToList()) child.pickingMode = PickingMode.Ignore;
            }
            AddLabel(dialog, GameText.Get("maps.thumbnail_attribution"), "rule-source");
        }
    }
}

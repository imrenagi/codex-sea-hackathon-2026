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

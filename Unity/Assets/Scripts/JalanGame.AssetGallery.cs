using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UjianSim.Game;

namespace JalanKita
{
    public sealed partial class JalanGame
    {
        readonly struct GalleryAsset
        {
            public readonly string File, Label, Category;
            public GalleryAsset(string file, string label, string category) { File = file; Label = label; Category = category; }
        }
        static readonly GalleryAsset[] GalleryAssets = {
            new("Warung", "Warung", "Buildings"), new("Ruko_A", "Ruko A", "Buildings"),
            new("Ruko_B", "Ruko B", "Buildings"), new("Ruko_C", "Ruko C", "Buildings"), new("Ruko_D", "Ruko D", "Buildings"),
            new("Car_MPV", "MPV", "Vehicles"), new("Car_City", "City car", "Vehicles"), new("Car_Sedan", "Sedan", "Vehicles"),
            new("Motorcycle", "Motorcycle", "Vehicles"), new("Angkot", "Angkot", "Vehicles"),
            new("Tree_Palm", "Palm", "Nature"), new("Tree_Shade_0", "Shade tree", "Nature"),
            new("StreetLamp", "Street lamp", "Street props"), new("PowerPole", "Power pole", "Street props"),
            new("TrafficLight", "Traffic light", "Street props"), new("Sign_Stop", "Stop sign", "Street props"),
            new("Sign_Crossing", "Crossing sign", "Street props"), new("RailCrossing", "Rail crossing", "Street props")
        };
        AssetGalleryPreview galleryPreview, galleryThumbnailStage;
        readonly List<RenderTexture> galleryThumbnails = new();
        readonly List<Image> galleryThumbnailImages = new();
        readonly List<Button> galleryButtons = new();
        Label galleryTitle, galleryMetadata;
        int galleryThumbnailIndex, galleryThumbnailReadyFrame;

        void ShowAssetGallery()
        {
            SetPage(Page.Gallery);
            var sheet = Resources.Load<StyleSheet>("UI/AssetGallery");
            if (sheet != null && !dialog.styleSheets.Contains(sheet)) dialog.styleSheets.Add(sheet);
            dialog.AddToClassList("asset-gallery-card");
            var header = Element(dialog, "gallery-header");
            AddLabel(header, "Asset gallery", "heading");
            AddActionButton(header, ActionKind.Back, "Back", ShowMenu, "quiet");
            var body = Element(dialog, "gallery-body");
            var list = new ScrollView { horizontalScrollerVisibility = ScrollerVisibility.Hidden };
            list.AddToClassList("gallery-list"); body.Add(list);
            var detail = Element(body, "gallery-detail");
            galleryTitle = AddLabel(detail, "", "heading");
            galleryMetadata = AddLabel(detail, "", "gallery-metadata");
            galleryPreview = new AssetGalleryPreview(transform, 900, 600, -3000);
            var image = new Image { image = galleryPreview.Texture, scaleMode = ScaleMode.ScaleToFit, focusable = true,
                tooltip = "Drag to rotate · Shift-drag or right-drag to pan · Scroll to zoom" };
            image.AddToClassList("gallery-viewer");
            image.AddManipulator(new GalleryDrag(galleryPreview)); detail.Add(image);
            image.RegisterCallback<WheelEvent>(e => { galleryPreview?.Zoom(e.delta.y); e.StopPropagation(); });
            image.RegisterCallback<KeyDownEvent>(e => {
                if (galleryPreview == null) return;
                switch (e.keyCode) {
                    case KeyCode.LeftArrow: galleryPreview.Orbit(-12, 0); break;
                    case KeyCode.RightArrow: galleryPreview.Orbit(12, 0); break;
                    case KeyCode.UpArrow: galleryPreview.Orbit(0, -8); break;
                    case KeyCode.DownArrow: galleryPreview.Orbit(0, 8); break;
                    case KeyCode.Equals: case KeyCode.KeypadPlus: galleryPreview.Zoom(-3); break;
                    case KeyCode.Minus: case KeyCode.KeypadMinus: galleryPreview.Zoom(3); break;
                    default: return;
                }
                e.StopPropagation();
            });
            image.RegisterCallback<NavigationMoveEvent>(e => ConsumeDrivingKey(e));
            var footer = Element(detail, "gallery-footer");
            AddLabel(footer, "Drag to rotate · Shift-drag to pan · Scroll to zoom", "gallery-hint");
            AddButton(footer, "Reset view", () => galleryPreview?.Reset(), "quiet");
            for (int i = 0; i < GalleryAssets.Length; i++)
            {
                int index = i;
                var item = GalleryAssets[i];
                var button = AddButton(list, "", () => SelectGalleryAsset(index), "quiet gallery-tile");
                button.tooltip = item.Label; button.name = "gallery-" + item.File;
                var thumb = new Image { scaleMode = ScaleMode.ScaleToFit, pickingMode = PickingMode.Ignore };
                thumb.AddToClassList("gallery-thumbnail"); button.Add(thumb);
                AddLabel(button, item.Label, "gallery-tile-label").pickingMode = PickingMode.Ignore;
                galleryThumbnailImages.Add(thumb); galleryButtons.Add(button);
            }
            SelectGalleryAsset(0);
            // A second tiny stage renders one thumbnail per frame, never 18 active cameras.
            galleryThumbnailStage = new AssetGalleryPreview(transform, 256, 192, -6000, 30);
            galleryThumbnailIndex = 0; galleryThumbnailStage.Select(GalleryAssets[0].File);
            galleryThumbnailReadyFrame = Time.frameCount + 1;
        }
        void SelectGalleryAsset(int index)
        {
            var item = GalleryAssets[index]; galleryPreview.Select(item.File);
            galleryTitle.text = item.Label;
            galleryMetadata.text = item.Category + " · " + item.File + ".fbx";
            for (int i = 0; i < galleryButtons.Count; i++) galleryButtons[i].EnableInClassList("selected", i == index);
        }
        void PresentAssetGallery()
        {
            galleryPreview?.Present(Time.unscaledDeltaTime);
            if (galleryThumbnailStage == null || Time.frameCount < galleryThumbnailReadyFrame) return;
            var thumbnail = new RenderTexture(256, 192, 0) { name = "Gallery " + GalleryAssets[galleryThumbnailIndex].File };
            thumbnail.Create(); Graphics.Blit(galleryThumbnailStage.Texture, thumbnail);
            galleryThumbnails.Add(thumbnail); galleryThumbnailImages[galleryThumbnailIndex].image = thumbnail;
            galleryThumbnailIndex++;
            if (galleryThumbnailIndex >= GalleryAssets.Length) { galleryThumbnailStage.Dispose(); galleryThumbnailStage = null; return; }
            galleryThumbnailStage.Select(GalleryAssets[galleryThumbnailIndex].File);
            galleryThumbnailReadyFrame = Time.frameCount + 1;
        }
        void CloseAssetGallery()
        {
            galleryPreview?.Dispose(); galleryPreview = null;
            galleryThumbnailStage?.Dispose(); galleryThumbnailStage = null;
            foreach (var thumbnail in galleryThumbnails) { thumbnail.Release(); Models.Kill(thumbnail); }
            galleryThumbnails.Clear(); galleryThumbnailImages.Clear(); galleryButtons.Clear();
            galleryTitle = null; galleryMetadata = null;
        }
        sealed class GalleryDrag : PointerManipulator
        {
            readonly AssetGalleryPreview preview;
            int pointer = -1; Vector2 previous; bool pan;
            public GalleryDrag(AssetGalleryPreview preview) { this.preview = preview; }
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
                if (pointer >= 0 || e.button > 2) return;
                pointer = e.pointerId; previous = e.position; pan = e.shiftKey || e.button != 0;
                preview.Orbit(0, 0); target.CapturePointer(pointer); target.Focus(); e.StopPropagation();
            }
            void Move(PointerMoveEvent e)
            {
                if (pointer != e.pointerId || !target.HasPointerCapture(pointer)) return;
                Vector2 now = e.position, delta = now - previous; previous = now;
                if (pan) preview.Pan(delta.x, delta.y); else preview.Orbit(-delta.x * .5f, delta.y * .4f);
                e.StopPropagation();
            }
            void Up(PointerUpEvent e) { if (e.pointerId != pointer) return; Release(); e.StopPropagation(); }
            void Cancel(PointerCaptureOutEvent e) { pointer = -1; }
            void Detach(DetachFromPanelEvent e) { Release(); }
            void Release() { int id = pointer; pointer = -1; if (id >= 0 && target.HasPointerCapture(id)) target.ReleasePointer(id); }
        }
    }
}

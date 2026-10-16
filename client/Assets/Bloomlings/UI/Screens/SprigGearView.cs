using System.Collections.Generic;
using Bloomlings.Client.Meta.Wardrobe;
using Bloomlings.Client.UI.Design;
using Bloomlings.Client.Art;
using Bloomlings.Client.Gameplay.Workers;
using UnityEngine;
using UnityEngine.UI;

namespace Bloomlings.Client.UI.Screens
{
    /// <summary>Six cropped shared sprites, loaded once; equipping creates no textures or outfit combinations.</summary>
    public sealed class SprigGearView
    {
        private static readonly Dictionary<string, Sprite?> Pictures = new Dictionary<string, Sprite?>();
        private readonly Image[] _parts = new Image[SprigGearArt.LayerCount];
        private Outfit? _outfit;
        private readonly Image _mask;
        private readonly Image _skin;
        public RectTransform Rect { get; }
        private static readonly Box Cell = new Box(0, 0, HeroMotion.CellWidth, HeroMotion.CellHeight);
        private SprigGearView(Transform parent)
        {
            Rect = UiFactory.CreateRect("SprigGear", parent);
            UiFactory.Stretch(Rect);
            _mask = UiFactory.CreateImage("SkinMask", Rect, null, Color.white);
            _mask.raycastTarget = false;
            _mask.gameObject.AddComponent<Mask>().showMaskGraphic = false;
            _skin = UiFactory.CreateImage("Skin", _mask.transform, null, Color.white);
            _skin.raycastTarget = false;
            UiFactory.Stretch(_skin.rectTransform);
            for (int i = 0; i < _parts.Length; i++)
            {
                _parts[i] = UiFactory.CreateImage("Layer" + i, Rect, null, Color.white);
                _parts[i].raycastTarget = false;
                _parts[i].preserveAspect = false;
            }
            _mask.rectTransform.SetSiblingIndex(1);
        }
        public static SprigGearView Create(Transform parent) => new SprigGearView(parent);
        public static Sprite? Picture(string name)
        {
            if (!Pictures.TryGetValue(name, out Sprite? sprite))
            {
                sprite = Resources.Load<Sprite>(HeroMotion.Folder + "/" + name);
                Pictures[name] = sprite;
            }
            return sprite;
        }
        public void Wear(Outfit? outfit)
        {
            _outfit = outfit;
            for (int i = 0; i < _parts.Length; i++)
            {
                HeroFrame? frame = SprigGearArt.Layer(outfit, i);
                _parts[i].gameObject.SetActive(frame.HasValue);
                if (frame.HasValue) _parts[i].sprite = Picture(frame.Value.Name);
            }
            _mask.gameObject.SetActive(outfit?.Skin != null);
            if (outfit?.Skin != null)
            {
                _mask.sprite = _parts[0].sprite;
                _skin.sprite = ProceduralSprites.SkinPatternFull(outfit.Skin.Shape);
                Color tint = BloomlingFigure.Tint(outfit.Skin); tint.a = CosmeticCatalog.SkinOpacity; _skin.color = tint;
            }
            Place(Cell);
        }
        public void Tick(HeroPose pose) => Place(SprigGearArt.PoseCell(Cell, pose));
        private void Place(Box cell)
        {
            for (int i = 0; i < _parts.Length; i++)
            {
                HeroFrame? frame = SprigGearArt.Layer(_outfit, i);
                if (!frame.HasValue) continue;
                Box box = HeroMotion.PictureBox(cell, frame.Value);
                UiFactory.Place(_parts[i].rectTransform, box.Left / Cell.Width, 1 - box.Bottom / Cell.Height,
                    box.Right / Cell.Width, 1 - box.Top / Cell.Height);
                if (i == 0) UiFactory.Place(_mask.rectTransform, box.Left / Cell.Width, 1 - box.Bottom / Cell.Height,
                    box.Right / Cell.Width, 1 - box.Top / Cell.Height);
            }
        }
    }
}

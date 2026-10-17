using System;
using System.Collections.Generic;
using Bloomlings.Client.App.Progression;
using Bloomlings.Client.Meta.Wardrobe;
using Bloomlings.Core.Variants;

namespace Bloomlings.Client.UI.Design
{
    /// <summary>Session-local family/category selection and scroll memory, shared by both clients.</summary>
    public sealed class WardrobeBrowseState
    {
        private readonly Dictionary<(Family, CosmeticKind), float> _offsets = new Dictionary<(Family, CosmeticKind), float>();
        public Family Family { get; private set; } = Family.Sprig;
        public CosmeticKind Kind { get; private set; } = CosmeticKind.Hat;
        public float Offset => _offsets.TryGetValue((Family, Kind), out float value) ? value : 0;
        public void Select(Family family) => Family = family;
        public void Select(CosmeticKind kind)
        {
            if (!CosmeticCatalog.IsWornKind(kind)) throw new ArgumentOutOfRangeException(nameof(kind));
            Kind = kind;
        }
        public void SetOffset(float offset, float maximum) =>
            _offsets[(Family, Kind)] = Math.Max(0, Math.Min(Math.Max(0, maximum), offset));
        public void Drag(float delta, float maximum) => SetOffset(Offset - delta, maximum);
    }

    public enum WardrobeItemState { Default, Owned, ForSale, Reward }
    public sealed record WardrobeShelfItem(CosmeticItem? Item, WardrobeItemState State, int? RewardCadence);

    public static class WardrobeShelfCatalog
    {
        /// <summary>Stable catalog order: equipping or buying never moves the tapped item to another row.</summary>
        public static IReadOnlyList<WardrobeShelfItem> Build(WardrobeService wardrobe, CosmeticKind kind, Family? family = null)
        {
            var result = new List<WardrobeShelfItem> { new WardrobeShelfItem(null, WardrobeItemState.Default, null) };
            var ordered=new List<CosmeticItem>();
            foreach(var item in wardrobe.Catalog.Items)if(item.Free&&item.MetaOnly)ordered.Add(item);
            foreach(var item in wardrobe.Catalog.Items)if(!(item.Free&&item.MetaOnly))ordered.Add(item);
            foreach (CosmeticItem item in ordered)
            {
                if (item.Kind != kind && !(kind == CosmeticKind.Hat && (item.Kind == CosmeticKind.Glasses || item.Kind == CosmeticKind.Backpack || item.Kind == CosmeticKind.Scarf))) continue;
                if (!HeroGearArt.CanPreview(item) || (family.HasValue && !item.Fits(family.Value))) continue;
                WardrobeItemState state = wardrobe.Owns(item) ? WardrobeItemState.Owned :
                    item.ForSale ? WardrobeItemState.ForSale : WardrobeItemState.Reward;
                int? cadence = null;
                foreach (MilestoneCadence reward in MilestoneTable.Default.Cadences)
                {
                    foreach (string id in reward.Items)
                    {
                        if (id == item.Id && (!cadence.HasValue || reward.Every < cadence.Value)) cadence = reward.Every;
                    }
                }
                result.Add(new WardrobeShelfItem(item, state, cadence));
            }
            return result;
        }

        public static Outfit Preview(Outfit outfit, CosmeticKind kind, CosmeticItem? item) => (item?.Kind ?? kind) switch
        {
            CosmeticKind.Skin => outfit with { Skin = item },
            CosmeticKind.Hat => item == null ? outfit with { Hat = null, Glasses = null, Backpack = null, Scarf = null } : outfit with { Hat = item },
            CosmeticKind.Trail => outfit with { Trail = item },
            CosmeticKind.Expression => outfit with { Expression = item },
            CosmeticKind.Glasses => outfit with { Glasses = item },
            CosmeticKind.Backpack => outfit with { Backpack = item },
            CosmeticKind.Scarf => outfit with { Scarf = item },
            _ => outfit,
        };
        public static bool Chosen(WardrobeService wardrobe, Family family, CosmeticKind filter, CosmeticItem? item)
        {
            if (item == null && filter == CosmeticKind.Hat)
            {
                Outfit shown = HeroGearArt.DisplayOutfit(wardrobe.OutfitOf(family))!;
                return shown.Hat == null && shown.Glasses == null && shown.Backpack == null && shown.Scarf == null;
            }
            return wardrobe.EquippedFor(family, item?.Kind ?? filter)?.Id == item?.Id;
        }
    }

    public sealed record WardrobeShelfRegions(Box Safe, float W, PageHeader Header, Box Stage, Box Hero,
        Box Name, Box Previous, Box Next, Box Families, Box Categories, Box Area, Box Viewport, float NavTop)
    {
        public const int Columns = 3;
        public float RowHeight => Area.Width * .49f;
        public Box Row(int row) => new Box(Area.Left, Area.Top + row * RowHeight, Area.Right, Area.Top + (row + 1) * RowHeight);
        public Box Cell(int index)
        {
            Box row = Row(index / Columns);
            float width = row.Width * .283f;
            float center = row.Left + row.Width * (.195f + (index % Columns) * .305f);
            return new Box(center - width * .5f, row.Top + RowHeight * .055f, center + width * .5f, row.Bottom - RowHeight * .10f);
        }
        public float ContentHeight(int count) => Math.Max(Area.Height, Math.Max(1, (count + Columns - 1) / Columns) * RowHeight + W * .025f);
        public float Maximum(int count) => Math.Max(0, ContentHeight(count) - Area.Height);
        /// <summary>Only visible rows plus one row on each side are instantiated, even for thousands of items.</summary>
        public (int First, int Last) Visible(int count, float offset)
        {
            if (count <= 0) return (0, -1);
            int first = Math.Max(0, (int)Math.Floor(offset / RowHeight) - 1) * Columns;
            int last = Math.Min(count - 1, ((int)Math.Ceiling((offset + Area.Height) / RowHeight) + 1) * Columns - 1);
            return (first, last);
        }
        public Box Track => new Box(Area.Right - W * .013f, Area.Top + W * .018f, Area.Right - W * .003f, Area.Bottom - W * .018f);
        public Box Thumb(int count, float offset)
        {
            Box track = Track;
            float height = Math.Min(track.Height, Math.Max(W * .065f, track.Height * Area.Height / ContentHeight(count)));
            float y = track.Top + (track.Height - height) * (Maximum(count) > 0 ? Math.Max(0, Math.Min(1, offset / Maximum(count))) : 0);
            return new Box(track.Left, y, track.Right, y + height);
        }
    }

    /// <summary>Compact fixed fitting stage and filters above three-column scrolling shelves.</summary>
    public static class WardrobeShelfDesign
    {
        public static readonly TypeStyle ItemName = new TypeStyle("type.shop.wardrobe_item", 30, true, false, 0, 16);
        public static readonly TypeStyle Status = new TypeStyle("type.shop.wardrobe_status", 26, true, false, 0, 16);
        public static readonly TypeStyle FamilyName = new TypeStyle("type.shop.wardrobe_family", 44, true, false, 0, 18);
        public static WardrobeShelfRegions Page(float width, float height, Insets insets)
        {
            var page = ScreenLayout.ReferenceStore(width, height, insets, false, false);
            Box safe = page.Safe;
            float w = page.W, top = page.Header.Row.Bottom + w * .012f;
            float navTop = ScreenLayout.BottomNavTop(width, height, insets);
            float available = navTop - top;
            float stageHeight = Math.Min(w * .62f, available * .30f);
            Box stage = Box.FromCenter(safe.CenterX, top + stageHeight * .5f, stageHeight * 1.55f, stageHeight);
            Box hero = In(stage, .33f, .18f, .67f, .705f);
            Box name = In(stage, .345f, .795f, .655f, .885f);
            float arrow = DesignTokens.Size.TouchMin * DesignTokens.ScaleFor(width, height);
            Box previous = Box.FromCenter(stage.Left + stage.Width * .085f, stage.CenterY, arrow, arrow);
            Box next = Box.FromCenter(stage.Right - stage.Width * .085f, stage.CenterY, arrow, arrow);
            float left = safe.Left + w * .025f, right = safe.Right - w * .025f;
            Box families = new Box(left, stage.Bottom + w * .01f, right, stage.Bottom + w * .01f + Math.Max(arrow,w*.105f));
            Box categories = new Box(left, families.Bottom + w * .012f, right, families.Bottom + w * .012f + Math.Max(arrow,w*.09f));
            Box area = new Box(left, categories.Bottom + w * .008f, right, navTop - w * .012f);
            Box viewport = new Box(left, categories.Top + categories.Height * .08f, right, safe.Bottom);
            return new WardrobeShelfRegions(safe, w, page.Header, stage, hero, name, previous, next, families, categories, area, viewport, navTop);
        }
        public static Box Picture(Box cell) => In(cell, .02f, .025f, .98f, .53f);
        public static Box Pedestal(Box cell) => In(cell, .015f, .43f, .985f, .62f);
        public static Box Caption(Box cell) => In(cell, .015f, .54f, .985f, .70f);
        public static Box Action(Box cell) => In(cell, .015f, .70f, .985f, 1);
        public static Box Check(Box cell) => Box.FromCenter(cell.Right - cell.Width * .07f, cell.Top + cell.Height * .45f, cell.Width * .23f, cell.Width * .23f);
        public static Box In(Box b, float l, float t, float r, float bottom) => new Box(b.Left + b.Width*l, b.Top + b.Height*t, b.Left + b.Width*r, b.Top + b.Height*bottom);
    }
}

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
        /// <summary>Only this stand may show an unowned item. It never enters WardrobeService or the save.</summary>
        public CosmeticItem? PreviewItem { get; private set; }
        public void ResetPreview() => PreviewItem = null;
        public void Select(Family family)
        {
            if (family != Family) ResetPreview();
            Family = family;
        }
        public void Select(CosmeticKind kind)
        {
            if (!ContainsCategory(kind)) throw new ArgumentOutOfRangeException(nameof(kind));
            if (kind != Kind) ResetPreview();
            Kind = kind;
        }
        public Outfit ShownOutfit(WardrobeService wardrobe) => PreviewItem == null
            ? wardrobe.OutfitOf(Family) : WardrobeShelfCatalog.Preview(wardrobe.OutfitOf(Family), Kind, PreviewItem);
        public bool Selected(WardrobeService wardrobe, CosmeticItem? item) => PreviewItem != null
            ? PreviewItem.Id == item?.Id : WardrobeShelfCatalog.Chosen(wardrobe, Family, Kind, item);
        public void TryOn(WardrobeService wardrobe, CosmeticItem? item)
        {
            if (item != null && (item.Kind != Kind || !item.Fits(Family))) return;
            if (item != null && !wardrobe.Owns(item)) { PreviewItem = item; return; }
            ResetPreview();
            if (item == null) wardrobe.Unequip(Family, Kind);
            else if (wardrobe.EquippedFor(Family, Kind)?.Id != item.Id) wardrobe.Equip(Family, item.Id);
        }
        private static bool ContainsCategory(CosmeticKind kind)
        { foreach (var category in CosmeticCatalog.WornKinds) if (category == kind) return true; return false; }
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
            var ordered=new List<(CosmeticItem Item,int Position)>();
            foreach(var item in wardrobe.Catalog.Items)
                if(item.Kind==kind&&HeroGearArt.CanPreview(item)&&(!family.HasValue||item.Fits(family.Value)))
                    ordered.Add((item,ordered.Count));
            ordered.Sort((a,b)=>
            {
                int group=EventRank(a.Item.WardrobeEvent).CompareTo(EventRank(b.Item.WardrobeEvent));
                if(group==0)group=StringComparer.Ordinal.Compare(a.Item.WardrobeEvent??"",b.Item.WardrobeEvent??"");
                if(group==0)group=ClassRank(a.Item.WardrobeClass).CompareTo(ClassRank(b.Item.WardrobeClass));
                if(group==0)group=StringComparer.Ordinal.Compare(a.Item.WardrobeClass??"",b.Item.WardrobeClass??"");
                return group!=0?group:a.Position.CompareTo(b.Position);
            });
            foreach (var entry in ordered)
            {
                CosmeticItem item=entry.Item;
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

        // Everyday classes first; seasonal/event collections remain together after them.
        // Unknown classes/events sort deterministically and never disappear from the catalog.
        private static int EventRank(string? key) => key switch
        {
            null => 0, "" => 0, "autumn" => 10, "winter" => 20, "new_year" => 30, _ => 100,
        };
        private static int ClassRank(string? key) => key switch
        {
            "hair" => 0, "panama" => 10, "cap" => 20, "beret" => 30, "straw_hat" => 40,
            "acorn_hat" => 50, "mushroom_hat" => 60, "crown" => 70, "nightcap" => 80,
            "earmuffs" => 90, "holiday_hat" => 100,
            "bowler" => 35, "explorer" => 45, "flower_hat" => 55, "headscarf" => 65,
            "glasses" => 0, "overalls" => 0, "top" => 3, "dress" => 5, "vest" => 6, "coat" => 10, "jacket" => 20,
            "striped_scarf" => 0, "knit_scarf" => 10, _ => 1000,
        };

        /// <summary>Legacy isolated-outfit helper for reference fixtures; live item cards use ItemPicture instead.</summary>
        public static Outfit ShelfPreview(CosmeticKind kind, CosmeticItem? item) => Preview(Outfit.None, kind, item);

        public static Outfit Preview(Outfit outfit, CosmeticKind kind, CosmeticItem? item) => (item?.Kind ?? kind) switch
        {
            CosmeticKind.Skin => outfit with { Skin = item },
            CosmeticKind.Hat => outfit with { Hat = item },
            CosmeticKind.Trail => outfit with { Trail = item },
            CosmeticKind.Expression => outfit with { Expression = item },
            CosmeticKind.Glasses => outfit with { Glasses = item },
            CosmeticKind.Backpack => outfit with { Backpack = item },
            CosmeticKind.Scarf => outfit with { Scarf = item },
            CosmeticKind.Clothes => outfit with { Clothes = item },
            _ => outfit,
        };
        public static bool Chosen(WardrobeService wardrobe, Family family, CosmeticKind filter, CosmeticItem? item)
        {
            return wardrobe.EquippedFor(family, item?.Kind ?? filter)?.Id == item?.Id;
        }
    }

    public sealed record WardrobeShelfRegions(Box Safe, float W, PageHeader Header, Box Stage, Box Hero,
        Box Name, Box Previous, Box Next, Box Families, Box Categories, Box Area, Box Viewport, float NavTop)
    {
        public const int Columns = 3;
        public const float RowShare = .275f;
        public float RowHeight => Area.Width * RowShare;
        public Box Row(int row) => new Box(Area.Left, Area.Top + row * RowHeight, Area.Right, Area.Top + (row + 1) * RowHeight);
        public Box Cell(int index)
        {
            Box row = Row(index / Columns);
            float width = row.Width * .312f;
            float center = row.Left + row.Width * (.162f + (index % Columns) * .329f);
            return new Box(center - width * .5f, row.Top + RowHeight * .018f, center + width * .5f, row.Bottom - RowHeight * .025f);
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

    /// <summary>Large fixed fitting stand and filters above three-column item-only cards.</summary>
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
            // Keep the hero large. Short phones show fewer inventory rows, not smaller tap targets.
            float stageHeight = Math.Min(w * .60f, available * .39f);
            float arrow = DesignTokens.Size.TouchMin * DesignTokens.ScaleFor(width, height);
            float filterHeight = Math.Max(arrow,w*.105f) + Math.Max(arrow,w*.09f);
            float threeRowsStage = available - filterHeight - w*.042f - 3*w*.95f*WardrobeShelfRegions.RowShare;
            if (threeRowsStage >= w*.52f) stageHeight = Math.Min(stageHeight, threeRowsStage);
            Box fitting = new Box(safe.Left, top, safe.Right, top + stageHeight);
            Box stage = In(fitting, .15f, .77f, .85f, 1);
            Box hero = Box.FromCenter(safe.CenterX, top + stageHeight * .39f, w * .64f, stageHeight * .98f);
            Box name = In(stage, .25f, .51f, .75f, .97f);
            // Family tabs replace the redundant arrows around the stand.
            Box previous = Box.FromCenter(stage.Left, stage.CenterY, arrow, arrow);
            Box next = Box.FromCenter(stage.Right, stage.CenterY, arrow, arrow);
            float left = safe.Left + w * .025f, right = safe.Right - w * .025f;
            Box families = new Box(left, stage.Bottom + w * .008f, right, stage.Bottom + w * .008f + Math.Max(arrow,w*.105f));
            Box categories = new Box(left, families.Bottom + w * .010f, right, families.Bottom + w * .010f + Math.Max(arrow,w*.09f));
            Box area = new Box(left, categories.Bottom + w * .012f, right, navTop - w * .012f);
            Box viewport = area; // Nothing scrolls behind the fixed filters or the opaque bottom menu.
            return new WardrobeShelfRegions(safe, w, page.Header, stage, hero, name, previous, next, families, categories, area, viewport, navTop);
        }
        public static Box Picture(Box cell, bool hasAction = true) => In(cell, .055f, .035f, .945f, hasAction ? .59f : .79f);
        public static Box Pedestal(Box cell) => In(cell, .015f, .43f, .985f, .62f);
        public static Box Caption(Box cell, bool hasAction = true) => In(cell, .05f, hasAction ? .59f : .80f, .95f, hasAction ? .71f : .965f);
        public static Box Action(Box cell) => In(cell, .07f, .69f, .93f, .99f);
        public static Box PurchaseTouch(Box cell) => In(cell, .025f, .505f, .975f, .995f);
        public static Box Check(Box cell) => Box.FromCenter(cell.Right - cell.Width * .115f, cell.Top + cell.Width * .115f, cell.Width * .17f, cell.Width * .17f);
        public static byte[] CardSurface(int width,int height,bool selected) =>
            ShopBodyRaster.Framed(width,height,.115f,selected?ShopCatalogDesign.ChosenRim:ShopBodyDesign.PetalsBorder,ShopBodyDesign.Card,.025f,false);
        public static string? ItemPicture(Family family, CosmeticKind kind, CosmeticItem? item)
        {
            // Default Head restores each hero's stock hair/hat. Other Default tiles mean an empty slot.
            string? id = item?.Id ?? (kind == CosmeticKind.Hat ? family switch
            {
                Family.Sprig => SprigV2Art.LeafyId, Family.Bloom => BloomV2Art.PetalsId,
                Family.Drop => DropV2Art.NativeId, _ => TwigV2Art.AcornId,
            } : null);
            if (id == null) return null;
            HeroFrame? frame = HeroGearArt.Icon(id);
            return frame.HasValue ? "heromotion/" + frame.Value.Name : ShopCatalogDesign.Accessory(id);
        }
        public static Box In(Box b, float l, float t, float r, float bottom) => new Box(b.Left + b.Width*l, b.Top + b.Height*t, b.Left + b.Width*r, b.Top + b.Height*bottom);
    }
}

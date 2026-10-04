using System;
using System.Collections.Generic;
using Bloomlings.Client.Meta.Wardrobe;
using Bloomlings.Client.Services.Feedback;
using Bloomlings.Client.UI.Design;
using Bloomlings.Core.Variants;
using C = Bloomlings.Client.UI.Design.DesignTokens.Colors;
using T = Bloomlings.Client.UI.Design.DesignTokens.Type;

namespace Bloomlings.Playtest.Design
{
    /// <summary>
    /// The Wardrobe (preview frame 27; spec 005 FR-025) in the reference layout (contracts/look.md §6.5,
    /// <see cref="ScreenLayout.ReferenceWardrobe"/>), opened from the bottom menu's Wardrobe (open from L40):
    /// <list type="bullet">
    /// <item><description>the page header on one line (<see cref="Kit.PageHeader"/>, shared with the Store page): the
    /// cream round back button, the wooden "Wardrobe" banner with ivy and the Petals pill (its "+" opens the Store page,
    /// whose back returns here);</description></item>
    /// <item><description>the chosen family's 3D hero in its outfit on the stone pedestal, with cream ‹ › arrows to
    /// the other families, over the Wardrobe garden (the owner's picture B7, else the warm Home
    /// garden);</description></item>
    /// <item><description>the parchment name card: the name on its tab, the family's role and a line about
    /// it;</description></item>
    /// <item><description>the four family tabs joined to the lighter panel below;</description></item>
    /// <item><description>on the panel, three outfit cards a page, each the family's hero wearing the item: "Default"
    /// (nothing worn) first, then the owned items (a tap wears or takes off), the items for sale with their cost pills
    /// (a tap buys and wears), and the ones earned later with the padlock; the worn ones are green with the
    /// check;</description></item>
    /// <item><description>the footer "Earn special outfits as you play!" between the page arrows;</description></item>
    /// <item><description>the bottom menu over the panel's foot, the Wardrobe in its medallion (spec 005 FR-030); the
    /// cards and the footer stand above it.</description></item>
    /// </list>
    /// Before the Wardrobe unlocks (L40) the bottom menu's Wardrobe still opens the page, locked (<see cref="Locked"/>):
    /// the same garden and header, and the page's lighter panel holding the locked notice ("Available from level 40")
    /// instead of the hero, the name card, the tabs, the cards and the footer.
    /// Equipping and buying go through the Unity client's shared <see cref="WardrobeService"/> (and its economy); the
    /// playtest keeps no rule of its own. The profile items (frames, badges, markers) stay in the Store.
    /// </summary>
    public static class WardrobeScreen
    {
        /// <summary>The outfit cards to a page (one row, as the reference's).</summary>
        public const int PerPage = ReferenceWardrobeRegions.Columns;

        /// <summary>The name card's and the footer's largest text size, as a share of the safe width (the reference's).</summary>
        private const float TextShare = 0.044f;

        /// <summary>The share of the description's box its two lines may fill (the reference's balanced lines).</summary>
        private const float AboutShare = 0.75f;

        /// <summary>The moment the drifting petals around the hero are drawn at (the screen does not animate).</summary>
        private const float PetalMoment = 2.4f;

        /// <summary>What an outfit card offers.</summary>
        public enum CardKind
        {
            /// <summary>Nothing worn (the family's own look).</summary>
            Default,

            /// <summary>An owned item: a tap wears it, or takes it off when worn.</summary>
            Owned,

            /// <summary>An item for sale: its cost pill; a tap buys it with Petals and wears it.</summary>
            ForSale,

            /// <summary>An item earned later (milestones): faded, with the padlock; no tap.</summary>
            Locked,
        }

        public static void Draw(IPainter p, DesignApp app)
        {
            PlaytestMeta meta = app.Meta;
            HomeLook look = HomeScreen.Look(app);
            if (!BottomNav.IsOpen(NavPlace.Wardrobe, look))
            {
                Locked(p, app, look);
                return;
            }

            WardrobeService wardrobe = meta.Wardrobe;
            ReferenceWardrobeRegions r = ScreenLayout.ReferenceWardrobe(p.Width, p.Height, p.Insets);
            IReadOnlyList<Family> families = WardrobeService.Families;
            int selected = Wrap(app.WardrobeFamily, families.Count);
            Family family = families[selected];
            Outfit outfit = wardrobe.OutfitOf(family);
            DesignApp.DrawBackdrop(p, BackdropScene.Home, meta.CurrentLevel, OwnerPictures.Wardrobe);

            // The hero in its outfit, standing on the middle of the pedestal's top, with a few petals drifting around it.
            Box top = Kit.StonePedestal(p, r.Pedestal);
            Kit.FallingPetals(p, new Box(r.Safe.Left, r.Banner.Bottom, r.Safe.Right, r.NameCard.Top), PetalMoment);
            Box hero = HomeStage.Figure(r.Hero.CenterX, top.CenterY, r.Hero.Height);
            Visuals.Hero(p, hero, family, outfit);
            void Select(int index)
            {
                app.WardrobeFamily = Wrap(index, families.Count);
                app.WardrobePage = 0;
            }

            Kit.ArrowButton(p, r.Previous.CenterX, r.Previous.CenterY, r.Previous.Width, next: false, () => Select(selected - 1));
            Kit.ArrowButton(p, r.Next.CenterX, r.Next.CenterY, r.Next.Width, next: true, () => Select(selected + 1));

            // The name card: the name on its tab, the role, a line about the family.
            string key = WardrobeService.FamilyKey(family);
            Kit.NameCard(p, r.NameCard, r.NameTab, PlaytestText.T("family." + key));
            // The role and the description in the reference's size (about 0.8 of their line's height, at most 0.044 W),
            // the description in two balanced lines about 0.6 W wide, as the reference's.
            float textSize = r.W * TextShare;
            float roleScale = Math.Min(r.Role.Height * 0.8f, textSize) / p.U(T.Body.Size);
            p.Text(PlaytestText.T("wardrobe.role." + key), r.Role.CenterX, r.Role.CenterY, T.Body, C.InkBrownSoft, r.Role.Width, roleScale, TextLook.Plain(C.InkBrownSoft));
            float lineHeight = r.About.Height / 2f;
            float aboutScale = Math.Min(lineHeight * 0.8f, textSize) / p.U(T.Body.Size);
            List<string> about = EndCards.Lines(p, PlaytestText.T("wardrobe.about." + key), T.Body, r.About.Width * AboutShare / aboutScale, 2);
            float y = r.About.CenterY - ((about.Count - 1) * lineHeight / 2f);
            foreach (string line in about)
            {
                p.Text(line, r.About.CenterX, y, T.Body, C.InkBrownSoft, r.About.Width, aboutScale);
                y += lineHeight;
            }

            // The family tabs over the lighter panel, the selected one joined to it.
            var names = new string[families.Count];
            var cells = new Box[families.Count];
            for (int i = 0; i < families.Count; i++)
            {
                names[i] = PlaytestText.T("family." + WardrobeService.FamilyKey(families[i]));
                cells[i] = r.Tab(i, families.Count);
            }

            Kit.FamilyTabs(p, r.Tabs, r.Panel, families, names, selected, Select, f => wardrobe.OutfitOf(f), cells);

            // The outfit cards of the page.
            IReadOnlyList<(CosmeticItem? Item, CardKind Kind)> cards = Cards(wardrobe);
            int pages = Math.Max(1, (cards.Count + PerPage - 1) / PerPage);
            int page = Math.Max(0, Math.Min(pages - 1, app.WardrobePage));
            // A page with an item for sale keeps room for the cost pills under all its cards, so the row lines up.
            bool pillRoom = false;
            for (int index = page * PerPage; index < Math.Min(cards.Count, (page + 1) * PerPage); index++)
            {
                pillRoom |= cards[index].Kind == CardKind.ForSale;
            }

            for (int slot = 0; slot < PerPage; slot++)
            {
                int index = (page * PerPage) + slot;
                if (index < cards.Count)
                {
                    Card(p, app, r.Card(slot), family, outfit, cards[index], pillRoom);
                }
            }

            // The footer between the page arrows.
            float footerScale = Math.Min(r.Footer.Height * 0.55f, textSize) / p.U(T.Body.Size);
            p.Text(PlaytestText.T("wardrobe.footer"), r.Footer.CenterX, r.Footer.CenterY, T.Body, C.InkBrownSoft, r.Footer.Width, footerScale);
            if (pages > 1)
            {
                Kit.ArrowButton(p, r.PagePrevious.CenterX, r.PagePrevious.CenterY, r.PagePrevious.Width, next: false, page > 0 ? () => app.WardrobePage = page - 1 : (Action?)null);
                Kit.ArrowButton(p, r.PageNext.CenterX, r.PageNext.CenterY, r.PageNext.Width, next: true, page < pages - 1 ? () => app.WardrobePage = page + 1 : (Action?)null);
            }

            // The bottom menu, the Wardrobe in its medallion (FR-030).
            Kit.BottomNav(p, HomeScreen.Nav(p, NavPlace.Wardrobe), look, app.Navigate);

            // The header last, on one line (the Store page's too): the back button, the banner with ivy, the Petals pill,
            // whose "+" opens the Store page (its back returns here).
            Kit.PageHeader(p, r.Header, PlaytestText.T("wardrobe.title"), app.CloseWardrobe, app.ShownPetals, look.Store ? app.OpenStore : (Action?)null);

            string? toast = app.HomeToastText;
            if (toast != null)
            {
                Kit.Toast(p, new Box(r.Safe.Left, r.Hero.Top, r.Safe.Right, r.Pedestal.Bottom), toast);
            }
        }

        /// <summary>
        /// The locked Wardrobe (spec 005 FR-030, contracts/look.md §6.7; <see cref="ScreenLayout.LockedPage"/>): the
        /// Wardrobe's garden, the page's lighter panel (as under its family tabs) from under the header to the bottom of
        /// the screen holding the locked notice (<see cref="Kit.LockedNotice"/>: the Wardrobe's icon with its padlock,
        /// "Available from level N" from the roadmap), the bottom menu with the Wardrobe raised, and the header as usual
        /// (the Petals pill's "+" opens the Store page once it is open).
        /// </summary>
        private static void Locked(IPainter p, DesignApp app, HomeLook look)
        {
            LockedPageRegions r = ScreenLayout.LockedPage(p.Width, p.Height, p.Insets);
            DesignApp.DrawBackdrop(p, BackdropScene.Home, app.Meta.CurrentLevel, OwnerPictures.Wardrobe);
            Kit.Panel(p, new Box(r.Panel.Left, r.Panel.Top, r.Panel.Right, r.Panel.Bottom + p.U(Kit.PanelRadius)));
            Kit.LockedNotice(p, r.Notice, NavPlace.Wardrobe, app.UnlockLevel(NavPlace.Wardrobe));
            Kit.BottomNav(p, HomeScreen.Nav(p, NavPlace.Wardrobe), look, app.Navigate);
            Kit.PageHeader(p, r.Header, PlaytestText.T("wardrobe.title"), app.CloseWardrobe, app.ShownPetals, look.Store ? app.OpenStore : (Action?)null);

            string? toast = app.HomeToastText;
            if (toast != null)
            {
                Kit.Toast(p, new Box(r.Safe.Left, r.Panel.Top, r.Safe.Right, r.Notice.Bottom), toast);
            }
        }

        /// <summary>
        /// The outfit cards in their order (the same for every family): "Default", then the owned worn items, the worn items
        /// for sale, and the worn items earned later, each in catalog order.
        /// </summary>
        public static IReadOnlyList<(CosmeticItem? Item, CardKind Kind)> Cards(WardrobeService wardrobe)
        {
            var cards = new List<(CosmeticItem?, CardKind)> { (null, CardKind.Default) };
            var owned = new HashSet<string>(StringComparer.Ordinal);
            foreach (CosmeticItem item in wardrobe.Owned)
            {
                owned.Add(item.Id);
                if (item.IsWorn)
                {
                    cards.Add((item, CardKind.Owned));
                }
            }

            foreach (CosmeticItem item in wardrobe.ForSale)
            {
                if (item.IsWorn)
                {
                    cards.Add((item, CardKind.ForSale));
                }
            }

            foreach (CosmeticItem item in wardrobe.Catalog.Items)
            {
                if (item.IsWorn && !item.ForSale && !owned.Contains(item.Id))
                {
                    cards.Add((item, CardKind.Locked));
                }
            }

            return cards;
        }

        /// <summary>One outfit card: the family's hero in its outfit with the item in its kind's place, and its tap.</summary>
        private static void Card(IPainter p, DesignApp app, Box box, Family family, Outfit outfit, (CosmeticItem? Item, CardKind Kind) card, bool pillRoom)
        {
            WardrobeService wardrobe = app.Meta.Wardrobe;
            CosmeticItem? item = card.Item;
            if (item == null)
            {
                // Default: nothing worn; a tap takes everything off.
                Action? undress = outfit.IsEmpty ? null : () =>
                {
                    foreach (CosmeticKind kind in CosmeticCatalog.WornKinds)
                    {
                        wardrobe.Unequip(family, kind);
                    }

                    app.Sound.Play(SoundCue.Click);
                };
                Kit.OutfitCard(p, box, PlaytestText.T("wardrobe.default"), outfit.IsEmpty, well => Preview(p, well, family, Outfit.None), action: undress, pillRoom: pillRoom);
                return;
            }

            string id = item.Id;
            Outfit dressed = With(outfit, item);
            string name = MetaCards.ItemName(item);
            switch (card.Kind)
            {
                case CardKind.Owned:
                    bool worn = wardrobe.EquippedFor(family, item.Kind)?.Id == id;
                    Kit.OutfitCard(p, box, name, worn, well => Preview(p, well, family, dressed), action: () =>
                    {
                        if (worn)
                        {
                            wardrobe.Unequip(family, item.Kind);
                        }
                        else
                        {
                            wardrobe.Equip(family, id);
                        }

                        app.Sound.Play(SoundCue.Click);
                    }, pillRoom: pillRoom);
                    break;
                case CardKind.ForSale:
                    Kit.OutfitCard(p, box, name, false, well => Preview(p, well, family, dressed), Cost.Petals(item.Price), () =>
                    {
                        if (wardrobe.TryBuy(id))
                        {
                            wardrobe.Equip(family, id);
                            app.Sound.Play(SoundCue.Click);
                        }
                        else
                        {
                            app.Sound.Play(SoundCue.Refused);
                            app.HomeToast(PlaytestText.T("gameplay.not_enough_petals"));
                        }
                    });
                    break;
                default:
                    Kit.OutfitCard(p, box, name, false, well => Preview(p, well, family, dressed), pillRoom: pillRoom, locked: true);
                    break;
            }
        }

        /// <summary>
        /// An outfit card's picture: the hero filling the well with its feet near the bottom; with a hat, a little smaller
        /// and lower so the hat stays inside the well (as the Store's outfit cards).
        /// </summary>
        private static void Preview(IPainter p, Box well, Family family, Outfit outfit)
        {
            bool hat = outfit.Hat != null;
            float height = well.Height * (hat ? 0.96f : 1.1f);
            Visuals.Hero(p, HomeStage.Figure(well.CenterX, well.Bottom - (well.Height * (hat ? 0.02f : 0.05f)), height), family, outfit);
        }

        /// <summary>The outfit with <paramref name="item"/> in its kind's place.</summary>
        private static Outfit With(Outfit outfit, CosmeticItem item) => item.Kind switch
        {
            CosmeticKind.Skin => outfit with { Skin = item },
            CosmeticKind.Hat => outfit with { Hat = item },
            CosmeticKind.Trail => outfit with { Trail = item },
            CosmeticKind.Expression => outfit with { Expression = item },
            _ => outfit,
        };

        private static int Wrap(int index, int count) => count <= 0 ? 0 : ((index % count) + count) % count;
    }
}

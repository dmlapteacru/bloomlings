using System;
using System.Collections.Generic;
using Bloomlings.Client.Meta.Profile;
using Bloomlings.Client.Meta.Wardrobe;
using Bloomlings.Client.UI.Design;
using C = Bloomlings.Client.UI.Design.DesignTokens.Colors;
using T = Bloomlings.Client.UI.Design.DesignTokens.Type;

namespace Bloomlings.Playtest.Design
{
    /// <summary>
    /// The profile page (preview frame 39; spec 005 FR-037, after the reference game's profile in the garden look) and its
    /// "Edit profile" card (frames 40 and 41), on <see cref="ScreenLayout.ReferenceProfile"/> and
    /// <see cref="ScreenLayout.ProfileEdit"/>; Home's avatar opens the page, its back (and the system back) returns Home:
    /// <list type="bullet">
    /// <item><description>the Wardrobe's garden, the page header (back, the "Profile" banner, the Petals pill) and the
    /// parchment panel;</description></item>
    /// <item><description>the player's card: the round avatar in its frame and badge (a tap opens the card on Avatar),
    /// the name with the pencil (the card on Name), the short ID, "Playing since 10/2026" and the wooden "Level N"
    /// plaque;</description></item>
    /// <item><description>a 3 × 3 grid of illustrated achievements per page, with progress toward the next tier.</description></item>
    /// </list>
    /// The card lists the 14 avatars (free ones first, the others with their price; the picked one checked), the owned
    /// frames and badges (once the Wardrobe is open) and the name (the host's text dialog); its button saves, or buys the
    /// picked avatar. Unity's twins are <c>ProfileScreen</c> and <c>ProfileEditCard</c>.
    /// </summary>
    public static class ProfileScreen
    {
        public static void Draw(IPainter p, DesignApp app)
        {
            HomeLook look = HomeScreen.Look(app);
            PlaytestMeta meta = app.Meta;
            ProfileService profile = meta.Profile;
            ReferenceProfileRegions r = ScreenLayout.ReferenceProfile(p.Width, p.Height, p.Insets);
            DesignApp.DrawBackdrop(p, BackdropScene.Home, meta.CurrentLevel, OwnerPictures.Wardrobe);
            float radius = r.PanelRadius(p.Scale);
            Kit.CardFrame(p, new Box(r.Panel.Left, r.Panel.Top, r.Panel.Right, r.Panel.Bottom + radius));
            float grow = r.Card.Width / (0.88f * r.W);
            bool covered = app.Overlays.Count > 0;

            // The player's card.
            Kit.Row(p, r.Card, false);
            ProfileLook decor = meta.Wardrobe.Profile;
            Kit.Avatar(p, r.Avatar, profile.Avatar, decor.Frame, decor.Badge, covered ? null : () => app.OpenProfileEdit(ProfileTab.Avatar), HomeScreen.OutfitsOf(app)?.Invoke(profile.Avatar.Family));
            p.TextLeft(NameOf(profile), r.Name.Left, r.Name.CenterY, T.Title, C.InkTitle, r.Name.Width, grow, TextLook.Plain(C.InkTitle));
            p.Mark("ui.edit");
            Kit.RoundButton(p, r.Edit.CenterX, r.Edit.CenterY, r.Edit.Width, "ui.edit", covered ? null : () => app.OpenProfileEdit(ProfileTab.Name));
            p.TextLeft(PlaytestText.F("profile.id", profile.ShortId), r.Id.Left, r.Id.CenterY, T.Body, C.InkBrownSoft, r.Id.Width, grow);
            p.TextLeft(PlaytestText.F("profile.joined", profile.JoinedMonth), r.Joined.Left, r.Joined.CenterY, T.Body, C.InkBrownSoft, r.Joined.Width, grow);
            // No flowers in the page's content (spec 005 FR-049, the owner, 2026-10-08): the plaque, the title sign, the avatar
            // and the tiles stand plain; only the page's frame keeps its corner flowers, as every page's.
            Kit.WoodSign(p, r.Plaque, PlaytestText.F("common.level", NumberText.Group(meta.CurrentLevel)), T.LevelPill);

            // The illustrated achievements, nine per page.
            string title = PlaytestText.T("profile.achievements");
            Kit.WoodSign(p, CardLook.TitleSign(r.AchievementsTitle, p.MeasureText(title, T.Title, grow)), title, T.Title);
            IReadOnlyList<AchievementState> achievements = Achievements.Of(meta.Save);
            int pageCount = (achievements.Count + r.Achievements.Count - 1) / r.Achievements.Count;
            int page = Math.Max(0, Math.Min(pageCount - 1, app.ProfileAchievementPage));
            Box previous = Box.FromCenter(r.AchievementsTitle.Left + r.AchievementsTitle.Height * 0.5f, r.AchievementsTitle.CenterY, r.AchievementsTitle.Height, r.AchievementsTitle.Height);
            Box next = Box.FromCenter(r.AchievementsTitle.Right - r.AchievementsTitle.Height * 0.5f, r.AchievementsTitle.CenterY, r.AchievementsTitle.Height, r.AchievementsTitle.Height);
            Kit.ArrowButton(p, previous.CenterX, previous.CenterY, previous.Width, next: false,
                covered ? null : () => app.ProfileAchievementPage = (page + pageCount - 1) % pageCount);
            Kit.ArrowButton(p, next.CenterX, next.CenterY, next.Width, next: true,
                covered ? null : () => app.ProfileAchievementPage = (page + 1) % pageCount);
            for (int i = 0; i < r.Achievements.Count; i++)
            {
                int index = page * r.Achievements.Count + i;
                if (index >= achievements.Count) break;
                AchievementState state = achievements[index];
                Achievement(p, r.Achievements[i], state, grow, covered ? null : () =>
                {
                    if (state.Tier > 0 && meta.Wardrobe.Show(state.Def.BadgeId)) app.HomeToast(PlaytestText.T("profile.badge_equipped"));
                    else app.HomeToast(PlaytestText.T("profile.achievement_help." + state.Def.Id));
                });
            }

            p.Text(PlaytestText.F("profile.achievements_page", page + 1, pageCount), r.AchievementsNote.CenterX, r.AchievementsNote.CenterY, T.Caption, C.InkBrownSoft, r.AchievementsNote.Width, grow);

            Kit.PageFlowers(p, r.Panel, r.Panel.Bottom);
            Kit.PageHeader(p, r.Header, PlaytestText.T("profile.title"), app.CloseProfile, app.ShownPetals, look.Store ? app.OpenStore : (Action?)null);
            string? toast = app.HomeToastText;
            if (toast != null && !covered)
            {
                Kit.Toast(p, new Box(r.Safe.Left, r.Panel.Top, r.Safe.Right, r.Card.Bottom), toast);
            }
        }

        /// <summary>
        /// An achievement's tile (<c>ui.achievement</c>): a cream tile in a thin wooden rim (spec 005 FR-047; no flowers since
        /// FR-049) holding the trophy in its tier's medal color
        /// (faded under the padlock badge before the bronze tier) and the count toward the next tier ("37/50"; the count
        /// alone with the check badge once gold), the name under it.
        /// </summary>
        public static void Achievement(IPainter p, Box tile, AchievementState state, float grow, Action? action = null)
        {
            p.Mark("ui.achievement");
            Box well = ReferenceProfileRegions.AchievementWell(tile);
            Kit.FramedTile(p, well, 0.24f);
            Box trophy = ReferenceProfileRegions.AchievementTrophy(well);
            string image = PainterBase.BadgePrefix + state.Def.Id;
            if (state.Tier == 0) p.PushAlpha(0.42f);
            if (p.HasSprite(image)) p.Sprite(image, trophy);
            else p.Shape("ui.trophy", trophy, C.InkBrownSoft);
            if (state.Tier == 0) p.PopAlpha();

            Box count = ReferenceProfileRegions.AchievementProgress(well);
            string text = state.Complete ? NumberText.Group(state.Value) : PlaytestText.F("profile.achievement_progress", NumberText.Group(state.Shown), NumberText.Group(state.Goal));
            p.Text(text, count.CenterX, count.CenterY, T.Caption, C.InkBrown, count.Width, grow, TextLook.Plain(C.InkBrown));
            if (state.Tier == 0)
            {
                Kit.LockBadge(p, well.Right - (well.Width * 0.14f), well.Bottom - (well.Height * 0.14f), well.Width * 0.3f);
            }
            else if (state.Complete)
            {
                Kit.CheckBadge(p, well.Right - (well.Width * 0.14f), well.Bottom - (well.Height * 0.14f), well.Width * 0.3f);
            }

            Box label = ReferenceProfileRegions.AchievementLabel(tile);
            Rgba ink = state.Tier == 0 ? C.InkBrownSoft : C.InkBrown;
            p.Text(PlaytestText.T(state.Def.NameKey), label.CenterX, label.CenterY, T.Caption, ink, label.Width, grow);
            if (action != null) p.Hit(tile, action);
        }

        /// <summary>The name the page shows: the chosen one, else "Gardener 4821" (<see cref="ProfileService.DefaultNumber"/>).</summary>
        public static string NameOf(ProfileService profile) => profile.Name ?? PlaytestText.F("profile.default_name", profile.DefaultNumber);

        /// <summary>The "Edit profile" card over the page (frames 40 and 41).</summary>
        public static void Edit(IPainter p, DesignApp app, float since)
        {
            ProfileEditor? editor = app.ProfileEditor;
            if (editor == null)
            {
                return;
            }

            PlaytestMeta meta = app.Meta;
            ProfileEditRegions r = ScreenLayout.ProfileEdit(p.Width, p.Height, p.Insets);
            Kit.Card(p, r.ContentUnits, PlaytestText.T("profile.edit_title"), app.CardClose, Kit.Pop(since), T.Title);
            Action? Act(Action action) => app.DrawingCovered ? null : action;

            // The tabs, then the preview: the picked avatar in the picked frame and badge, and the name.
            var labels = new List<string>();
            foreach (ProfileTab tab in ProfileEditor.Tabs)
            {
                labels.Add(PlaytestText.T(TabKey(tab)));
            }

            Kit.Tabs(p, r.Tabs, labels, (int)editor.Tab, i => { editor.Tab = ProfileEditor.Tabs[i]; app.ProfileBadgePage = 0; });
            Kit.Avatar(p, r.Preview, editor.Avatar, editor.Frame, editor.Badge, null);
            string name = editor.Name ?? PlaytestText.F("profile.default_name", meta.Profile.DefaultNumber);
            p.TextLeft(name, r.PreviewName.Left, r.PreviewName.CenterY, T.Title, C.InkTitle, r.PreviewName.Width, look: TextLook.Plain(C.InkTitle));

            switch (editor.Tab)
            {
                case ProfileTab.Avatar:
                    for (int i = 0; i < meta.Profile.Avatars.Count; i++)
                    {
                        AvatarItem avatar = meta.Profile.Avatars[i];
                        Box cell = r.Cell(i);
                        bool picked = avatar.Id == editor.AvatarId;
                        Box picture = ProfileEditRegions.CellPicture(cell);
                        if (picked)
                        {
                            Box plate = ProfileEditRegions.Picked(picture);
                            p.FillRound(plate, GardenLook.IconRadius(plate), GardenLook.Green.Face);
                        }

                        Kit.Avatar(p, picture, avatar, null, null, null);
                        if (!meta.Profile.Owns(avatar))
                        {
                            Kit.CostPill(p, ProfileEditRegions.CellPrice(cell), Cost.Petals(meta.Profile.Price(avatar)), button: true);
                        }

                        if (picked)
                        {
                            Box check = ProfileEditRegions.CellCheck(cell);
                            Kit.CheckBadge(p, check.CenterX, check.CenterY, check.Width);
                        }

                        Action? pick = Act(() =>
                        {
                            app.Sound.Play(Bloomlings.Client.Services.Feedback.SoundCue.Click);
                            editor.PickAvatar(avatar.Id);
                        });
                        if (pick != null)
                        {
                            p.Hit(cell, pick);
                        }
                    }

                    break;
                case ProfileTab.Frame:
                case ProfileTab.Badge:
                    Items(p, app, editor, r, editor.Tab == ProfileTab.Frame ? CosmeticKind.Frame : CosmeticKind.Badge);
                    break;
                default:
                    Kit.Well(p, r.NameField, r.NameField.Height * 0.3f, C.CreamTop);
                    p.Text(name, r.NameField.CenterX, r.NameField.CenterY, T.Title, C.InkBrown, r.NameField.Width * 0.9f, look: TextLook.Plain(C.InkBrown));
                    Kit.SecondaryButton(p, r.NameButton, PlaytestText.T("profile.name_change"), Act(app.AskProfileName), "ui.edit");
                    p.Text(PlaytestText.T("profile.name_hint"), r.NameHint.CenterX, r.NameHint.CenterY, T.Caption, C.InkBrownSoft, r.NameHint.Width);
                    break;
            }

            string label = editor.Action == ProfileAction.Buy
                ? PlaytestText.F("profile.buy", NumberText.Group(editor.Price))
                : PlaytestText.T("profile.save");
            Kit.PrimaryButton(p, r.Button, label, Act(app.ConfirmProfileEdit));
            Kit.EndCard(p);

            // The page's toast lies under the scrim while the card is open: it shows under the card instead.
            string? toast = app.HomeToastText;
            if (toast != null && !app.DrawingCovered)
            {
                Box safe = ScreenLayout.SafeArea(p.Width, p.Height, p.Insets);
                Kit.Toast(p, new Box(safe.Left, r.Card.Card.Bottom, safe.Right, Math.Min(safe.Bottom, r.Card.Card.Bottom + p.U(140f))), toast);
            }
        }

        /// <summary>
        /// The Frame or Badge tab: the listed items on the picked avatar (the five free frames from Level 1, every owned one
        /// once the Wardrobe is open), with "No badge" first on the Badge tab, the picked one on a green disc with the check.
        /// </summary>
        private static void Items(IPainter p, DesignApp app, ProfileEditor editor, ProfileEditRegions r, CosmeticKind kind)
        {
            if (editor.IsLocked(kind))
            {
                Kit.LockBadge(p, r.NoteLock.CenterX, r.NoteLock.CenterY, r.NoteLock.Width);
                p.Text(PlaytestText.F("profile.badges_locked", app.UnlockLevel(NavPlace.Wardrobe)), r.Note.CenterX, r.Note.CenterY, T.Body, C.InkBrownSoft, r.Note.Width);
                return;
            }

            IReadOnlyList<CosmeticItem> owned = editor.Owned(kind);
            bool badges = kind == CosmeticKind.Badge;
            int itemCount = owned.Count + (badges ? 1 : 0);
            if (itemCount == 0)
            {
                p.Text(PlaytestText.T(kind == CosmeticKind.Frame ? "profile.no_frames" : "profile.no_badges"), r.Note.CenterX, r.Note.CenterY, T.Body, C.InkBrownSoft, r.Note.Width);
                return;
            }

            bool paged = badges && itemCount > r.CellCount;
            int perPage = paged ? r.CellCount - 1 : r.CellCount;
            int pageCount = (itemCount + perPage - 1) / perPage;
            int page = paged ? Math.Max(0, Math.Min(pageCount - 1, app.ProfileBadgePage)) : 0;
            int start = page * perPage;
            int count = Math.Min(itemCount - start, perPage);
            for (int i = 0; i < count; i++)
            {
                int itemIndex = start + i;
                Box cell = r.Cell(i);
                if (badges && itemIndex == 0)
                {
                    bool noBadgePicked = editor.BadgeId == null;
                    if (noBadgePicked)
                    {
                        Box plate = ProfileEditRegions.Picked(ProfileEditRegions.CellPicture(cell));
                        p.FillRound(plate, GardenLook.IconRadius(plate), GardenLook.Green.Face);
                    }

                    Kit.Avatar(p, ProfileEditRegions.CellItemAvatar(cell), editor.Avatar, null, null, null);
                    Box label = ProfileEditRegions.CellPrice(cell);
                    p.Text(PlaytestText.T("profile.badge_none"), label.CenterX, label.CenterY, T.Caption, C.InkBrown, label.Width);
                    if (noBadgePicked)
                    {
                        Box check = ProfileEditRegions.CellCheck(cell);
                        Kit.CheckBadge(p, check.CenterX, check.CenterY, check.Width);
                    }

                    if (!app.DrawingCovered) p.Hit(cell, editor.ClearBadge);
                    continue;
                }

                CosmeticItem item = owned[itemIndex - (badges ? 1 : 0)];
                bool frame = kind == CosmeticKind.Frame;
                bool picked = item.Id == (frame ? editor.FrameId : editor.BadgeId);
                if (picked)
                {
                    Box plate = ProfileEditRegions.Picked(ProfileEditRegions.CellPicture(cell));
                    p.FillRound(plate, GardenLook.IconRadius(plate), GardenLook.Green.Face);
                }

                Kit.Avatar(p, ProfileEditRegions.CellItemAvatar(cell), editor.Avatar, frame ? item : null, frame ? null : item, null);
                if (picked)
                {
                    Box check = ProfileEditRegions.CellCheck(cell);
                    Kit.CheckBadge(p, check.CenterX, check.CenterY, check.Width);
                }

                if (!app.DrawingCovered)
                {
                    p.Hit(cell, () =>
                    {
                        app.Sound.Play(Bloomlings.Client.Services.Feedback.SoundCue.Click);
                        if (frame)
                        {
                            editor.PickFrame(item.Id);
                        }
                        else
                        {
                            editor.PickBadge(item.Id);
                        }
                    });
                }
            }
            if (paged)
            {
                Box more = r.Cell(r.CellCount - 1);
                Kit.FramedTile(p, more, 0.22f);
                p.Text(PlaytestText.F("profile.badge_page", page + 1, pageCount), more.CenterX, more.CenterY, T.Caption, C.InkBrown, more.Width);
                if (!app.DrawingCovered) p.Hit(more, () => app.ProfileBadgePage = (page + 1) % pageCount);
            }
        }

        private static string TabKey(ProfileTab tab) => tab switch
        {
            ProfileTab.Avatar => "profile.tab_avatar",
            ProfileTab.Frame => "profile.tab_frame",
            ProfileTab.Badge => "profile.tab_badge",
            _ => "profile.tab_name",
        };
    }
}

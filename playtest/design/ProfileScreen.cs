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
    /// <item><description>three stats: levels won, pictures collected, milestones reached;</description></item>
    /// <item><description>the three achievements (<see cref="Achievements"/>): Green Thumb, Picture Keeper and Daily
    /// Gardener, each a trophy in its tier's medal color with the count toward the next tier.</description></item>
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
            Kit.Paper(p, new Box(r.Panel.Left, r.Panel.Top, r.Panel.Right, r.Panel.Bottom + radius), radius, DesignTokens.Garden.FrameWidth, DesignTokens.Garden.FrameDepthCard);
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
            Kit.WoodSign(p, r.Plaque, PlaytestText.F("common.level", NumberText.Group(meta.CurrentLevel)), T.LevelPill);

            // The stats.
            (string Label, long Value)[] stats =
            {
                (PlaytestText.T("profile.stat_levels"), Achievements.Count(meta.Save, Achievements.LevelsCounter)),
                (PlaytestText.T("profile.stat_pictures"), meta.Collection.Count),
                (PlaytestText.T("profile.stat_milestones"), meta.Save.Milestones.Claimed.Count),
            };
            for (int i = 0; i < stats.Length; i++)
            {
                Box cell = r.Stats[i];
                Kit.Well(p, cell, cell.Height * 0.22f);
                Box value = ReferenceProfileRegions.StatValue(cell);
                p.Text(NumberText.Group(stats[i].Value), value.CenterX, value.CenterY, T.LevelPill, C.InkBrown, value.Width * 0.9f, grow, TextLook.Plain(C.InkBrown));
                Box label = ReferenceProfileRegions.StatLabel(cell);
                p.Text(stats[i].Label, label.CenterX, label.CenterY, T.Caption, C.InkBrownSoft, label.Width, grow);
            }

            // The Achievements: bronze, silver and gold for what the save counts (Achievements).
            p.Text(PlaytestText.T("profile.achievements"), r.AchievementsTitle.CenterX, r.AchievementsTitle.CenterY, T.Title, C.InkTitle, r.AchievementsTitle.Width, grow, TextLook.Plain(C.InkTitle));
            IReadOnlyList<AchievementState> achievements = Achievements.Of(stats[0].Value, stats[1].Value, Achievements.Count(meta.Save, Achievements.DailyCounter));
            for (int i = 0; i < r.Achievements.Count && i < achievements.Count; i++)
            {
                Achievement(p, r.Achievements[i], achievements[i], grow);
            }

            p.Text(PlaytestText.T("profile.achievements_note"), r.AchievementsNote.CenterX, r.AchievementsNote.CenterY, T.Caption, C.InkBrownSoft, r.AchievementsNote.Width, grow);

            Kit.PageHeader(p, r.Header, PlaytestText.T("profile.title"), app.CloseProfile, app.ShownPetals, look.Store ? app.OpenStore : (Action?)null);
            string? toast = app.HomeToastText;
            if (toast != null && !covered)
            {
                Kit.Toast(p, new Box(r.Safe.Left, r.Panel.Top, r.Safe.Right, r.Card.Bottom), toast);
            }
        }

        /// <summary>
        /// An achievement's tile (<c>ui.achievement</c>): a parchment well holding the trophy in its tier's medal color
        /// (faded under the padlock badge before the bronze tier) and the count toward the next tier ("37/50"; the count
        /// alone with the check badge once gold), the name under it.
        /// </summary>
        public static void Achievement(IPainter p, Box tile, AchievementState state, float grow)
        {
            p.Mark("ui.achievement");
            Box well = ReferenceProfileRegions.AchievementWell(tile);
            Kit.Well(p, well, well.Width * 0.24f);
            Box trophy = ReferenceProfileRegions.AchievementTrophy(well);
            Rgba? medal = AchievementLook.TierColor(state.Tier);
            if (medal.HasValue)
            {
                p.Shape("ui.trophy", trophy, medal.Value);
            }
            else
            {
                p.PushAlpha(0.35f);
                p.Shape("ui.trophy", trophy, C.InkBrownSoft);
                p.PopAlpha();
            }

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
            Kit.Card(p, ProfileEditRegions.ContentUnits, PlaytestText.T("profile.edit_title"), app.CardClose, Kit.Pop(since), T.Title);
            Action? Act(Action action) => app.DrawingCovered ? null : action;

            // The tabs, then the preview: the picked avatar in the picked frame and badge, and the name.
            var labels = new List<string>();
            foreach (ProfileTab tab in ProfileEditor.Tabs)
            {
                labels.Add(PlaytestText.T(TabKey(tab)));
            }

            Kit.Tabs(p, r.Tabs, labels, (int)editor.Tab, i => editor.Tab = ProfileEditor.Tabs[i]);
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
                            p.FillCircle(picture.CenterX, picture.CenterY, picture.Width * ProfileEditRegions.PickedShare, GardenLook.Green.Face);
                        }

                        Kit.Avatar(p, picture, avatar, null, null, null);
                        if (!meta.Profile.Owns(avatar))
                        {
                            Kit.CostPill(p, ProfileEditRegions.CellPrice(cell), Cost.Petals(meta.Profile.Price(avatar)));
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
            Kit.PrimaryButton(p, r.Button, label, Act(app.ConfirmProfileEdit), decorate: true);
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
        /// once the Wardrobe is open), the picked one on a green disc with the check, or the note while there are none.
        /// </summary>
        private static void Items(IPainter p, DesignApp app, ProfileEditor editor, ProfileEditRegions r, CosmeticKind kind)
        {
            if (editor.IsLocked(kind))
            {
                Kit.LockBadge(p, r.Note.CenterX, r.Note.Top - (r.CellSize * 0.4f), r.CellSize * 0.5f);
                p.Text(PlaytestText.F("profile.badges_locked", app.UnlockLevel(NavPlace.Wardrobe)), r.Note.CenterX, r.Note.CenterY, T.Body, C.InkBrownSoft, r.Note.Width);
                return;
            }

            IReadOnlyList<CosmeticItem> owned = editor.Owned(kind);
            if (owned.Count == 0)
            {
                p.Text(PlaytestText.T(kind == CosmeticKind.Frame ? "profile.no_frames" : "profile.no_badges"), r.Note.CenterX, r.Note.CenterY, T.Body, C.InkBrownSoft, r.Note.Width);
                return;
            }

            int count = Math.Min(owned.Count, ProfileEditRegions.Columns * ProfileEditRegions.Rows);
            for (int i = 0; i < count; i++)
            {
                CosmeticItem item = owned[i];
                Box cell = r.Cell(i);
                bool frame = kind == CosmeticKind.Frame;
                bool picked = item.Id == (frame ? editor.FrameId : editor.BadgeId);
                if (picked)
                {
                    Box disc = ProfileEditRegions.CellPicture(cell);
                    p.FillCircle(disc.CenterX, disc.CenterY, disc.Width * ProfileEditRegions.PickedShare, GardenLook.Green.Face);
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

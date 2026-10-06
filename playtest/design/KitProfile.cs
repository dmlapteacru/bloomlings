using System;
using Bloomlings.Client.Meta.Profile;
using Bloomlings.Client.Meta.Wardrobe;
using Bloomlings.Client.UI.Design;
using C = Bloomlings.Client.UI.Design.DesignTokens.Colors;

namespace Bloomlings.Playtest.Design
{
    /// <summary>The profile's pieces (spec 005 FR-037): the rounded-square avatar in its wood rim or its frame, and its badge.</summary>
    public static partial class Kit
    {
        /// <summary>
        /// The profile avatar (Home's header, the profile page, the edit card; Unity's <c>ProfileAvatar</c>) on
        /// <see cref="AvatarLook"/> (the owner, 2026-10-06: the picture fills the avatar, a rounded square, and its border is
        /// one: the light wood rim by default, or the chosen frame in its place): with no <paramref name="frame"/>, the wood
        /// rim (<see cref="IconRim"/>) round the picture filling the rounded square inside it; with one, the picture a little
        /// larger and the frame over its edge (a drawn frame, <see cref="ProfileFrames"/>, or the plain tinted band); a thin
        /// <c>wood.line</c> ring on the picture's edge, and the profile badge at the lower left (0.36 of the side). With
        /// <paramref name="action"/> it presses like an icon button and a tap runs it.
        /// </summary>
        public static void Avatar(IPainter p, Box box, AvatarItem avatar, CosmeticItem? frame, CosmeticItem? badge, Action? action, Outfit? outfit = null)
        {
            p.Mark("ui.button.round");
            float depth = Press(p, box, action != null);
            Squash(p, box, depth);
            float dark = 0.08f * Math.Max(0f, Math.Min(1f, depth));
            bool framed = frame != null;
            if (!framed)
            {
                IconRim(p, AvatarLook.Rim(box), AvatarLook.RimRadius(box));
            }

            Box disc = AvatarLook.Disc(box, framed);
            float radius = AvatarLook.DiscRadius(box, framed);
            float ring = AvatarLook.Ring(disc, p.U(2f));
            p.FillRound(disc, radius, C.WoodLine);
            AvatarPicture(p, AvatarLook.Picture(disc, ring), avatar, outfit, ring: false, radius: radius - ring);
            if (dark > 0f)
            {
                // Pressed, the picture darkens as the icon buttons' faces do.
                p.FillRound(disc, radius, C.GardenShadow.WithAlpha(dark));
            }

            if (frame != null)
            {
                Frame(p, box, frame);
            }

            if (badge != null)
            {
                p.Shape("cosmetic.badge", AvatarLook.Badge(box), Visuals.Tint(badge));
            }

            p.PopTransform();
            if (action != null)
            {
                p.Hit(Touch(p, box), action);
            }
        }

        /// <summary>
        /// A profile frame in place of an avatar's wood rim: a drawn one (<see cref="ProfileFrames.StyleOf"/> its shape; its
        /// picture, <see cref="UiRaster.ProfileFrame"/>, in <see cref="AvatarLook.Frame"/>), else the plain
        /// <c>cosmetic.frame</c> rounded-square band in its tint (<see cref="AvatarLook.ShapeFrame"/>).
        /// </summary>
        public static void Frame(IPainter p, Box avatar, CosmeticItem frame)
        {
            ProfileFrameStyle? style = ProfileFrames.StyleOf(frame.Shape);
            if (style.HasValue)
            {
                ProfileFrameStyle drawn = style.Value;
                string slot = ProfileFrames.Slot(drawn);
                p.Mark(slot);
                p.Picture(slot, AvatarLook.Frame(avatar), (w, h) => ProfileFrames.Render(drawn, Math.Min(w, h)));
            }
            else
            {
                p.Shape("cosmetic.frame", AvatarLook.ShapeFrame(avatar), Visuals.Tint(frame));
            }
        }

        /// <summary>
        /// An avatar's picture filling the rounded square <paramref name="picture"/> (its corners <paramref name="radius"/>,
        /// by default <see cref="AvatarLook.Radius"/>; the owner's <c>Avatars/{picture}.jpg</c>, clipped to it), or, while it is
        /// missing, the avatar's family hero (in <paramref name="outfit"/>) on the soft green middle Home's avatar had
        /// before; with <paramref name="ring"/>, a thin <c>cream.line</c> ring over its edge (the leaderboard's portrait,
        /// whose disc has no ring of its own).
        /// </summary>
        public static void AvatarPicture(IPainter p, Box picture, AvatarItem avatar, Outfit? outfit = null, bool ring = true, float? radius = null)
        {
            p.Mark(OwnerPictures.AvatarSlot);
            string name = PainterBase.AvatarPrefix + avatar.Picture;
            float r = radius ?? AvatarLook.Radius(picture);
            if (p.HasSprite(name))
            {
                p.PushClipRound(picture, r);
                p.Sprite(name, picture);
                p.PopClip();
            }
            else
            {
                p.FillRoundGradient(picture, r, GardenLook.Green.Top.Mix(C.CreamTop, 0.6f), GardenLook.Green.Face.Mix(C.CreamTop, 0.45f));
                Visuals.Hero(p, AvatarLook.Hero(picture), avatar.Family, outfit);
            }

            if (ring)
            {
                p.StrokeRound(picture, r, Math.Max(1f, p.U(DesignTokens.Garden.OutlineWidth)), C.CreamLine);
            }
        }
    }
}

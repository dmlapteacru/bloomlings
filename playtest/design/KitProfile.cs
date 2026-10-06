using System;
using Bloomlings.Client.Meta.Profile;
using Bloomlings.Client.Meta.Wardrobe;
using Bloomlings.Client.UI.Design;
using C = Bloomlings.Client.UI.Design.DesignTokens.Colors;

namespace Bloomlings.Playtest.Design
{
    /// <summary>The profile's pieces (spec 005 FR-037): the rounded-square avatar filling its disc in its wood rim, its frame and badge.</summary>
    public static partial class Kit
    {
        /// <summary>
        /// The profile avatar (Home's header, the profile page, the edit card; Unity's <c>ProfileAvatar</c>) on
        /// <see cref="AvatarLook"/> (the owner, 2026-10-06: the picture fills the whole avatar, which became a rounded
        /// square in the icon buttons' wood rim): the light wood rim (<see cref="IconRim"/>), the cream lip (<c>cream.lip</c>
        /// in its <c>cream.line</c> outline) under the disc, and the avatar's picture filling the disc inside a thin
        /// <c>cream.line</c> ring (<see cref="AvatarPicture"/>), with no cream gap; the chosen profile frame over the disc's
        /// edge and the rim (a drawn frame, <see cref="ProfileFrames"/>, or the plain tinted frame at 1.08 of the avatar)
        /// and the profile badge at its lower left (0.36 of it). With <paramref name="action"/> it presses like an icon
        /// button (the disc sinks into its lip) and a tap runs it.
        /// </summary>
        public static void Avatar(IPainter p, Box box, AvatarItem avatar, CosmeticItem? frame, CosmeticItem? badge, Action? action, Outfit? outfit = null)
        {
            p.Mark("ui.button.round");
            float depth = Press(p, box, action != null);
            Squash(p, box, depth);
            ColorSet set = GardenLook.White;
            float dark = 0.08f * Math.Max(0f, Math.Min(1f, depth));
            IconRim(p, AvatarLook.Rim(box), AvatarLook.RimRadius(box));
            Box disc = AvatarLook.Disc(box, depth);
            Box lip = AvatarLook.Lip(box);
            float ring = AvatarLook.Ring(disc, p.U(2f));
            float radius = AvatarLook.Radius(disc);
            p.FillRound(lip, radius, set.Line);
            p.FillRound(lip.Inset(ring), radius - ring, set.Lip.Darken(dark));
            p.FillRound(disc, radius, set.Line);
            AvatarPicture(p, AvatarLook.Picture(disc, ring), avatar, outfit, ring: false);
            if (dark > 0f)
            {
                // Pressed, the disc darkens as the icon buttons' faces do.
                p.FillRound(disc, radius, C.GardenShadow.WithAlpha(dark));
            }

            if (frame != null)
            {
                Frame(p, box, disc, frame);
            }

            if (badge != null)
            {
                p.Shape("cosmetic.badge", AvatarLook.Badge(box, disc), Visuals.Tint(badge));
            }

            p.PopTransform();
            if (action != null)
            {
                p.Hit(Touch(p, box), action);
            }
        }

        /// <summary>
        /// A profile frame over an avatar's disc: a drawn one (<see cref="ProfileFrames.StyleOf"/> its shape; its picture,
        /// <see cref="UiRaster.ProfileFrame"/>, in <see cref="AvatarLook.Frame"/>), else the plain <c>cosmetic.frame</c>
        /// rounded-square band in its tint, 1.08 of the avatar.
        /// </summary>
        public static void Frame(IPainter p, Box avatar, Box disc, CosmeticItem frame)
        {
            ProfileFrameStyle? style = ProfileFrames.StyleOf(frame.Shape);
            if (style.HasValue)
            {
                ProfileFrameStyle drawn = style.Value;
                string slot = ProfileFrames.Slot(drawn);
                p.Mark(slot);
                p.Picture(slot, AvatarLook.Frame(disc), (w, h) => ProfileFrames.Render(drawn, Math.Min(w, h)));
            }
            else
            {
                p.Shape("cosmetic.frame", AvatarLook.ShapeFrame(avatar, disc), Visuals.Tint(frame));
            }
        }

        /// <summary>
        /// An avatar's picture filling the rounded square <paramref name="picture"/> (its corners
        /// <see cref="AvatarLook.Radius"/>; the owner's <c>Avatars/{picture}.jpg</c>, clipped to it), or, while it is
        /// missing, the avatar's family hero (in <paramref name="outfit"/>) on the soft green middle Home's avatar had
        /// before; with <paramref name="ring"/>, a thin <c>cream.line</c> ring over its edge (the leaderboard's portrait,
        /// whose disc has no ring of its own).
        /// </summary>
        public static void AvatarPicture(IPainter p, Box picture, AvatarItem avatar, Outfit? outfit = null, bool ring = true)
        {
            p.Mark(OwnerPictures.AvatarSlot);
            string name = PainterBase.AvatarPrefix + avatar.Picture;
            float r = AvatarLook.Radius(picture);
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

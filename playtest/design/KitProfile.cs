using System;
using Bloomlings.Client.Meta.Profile;
using Bloomlings.Client.Meta.Wardrobe;
using Bloomlings.Client.UI.Design;
using C = Bloomlings.Client.UI.Design.DesignTokens.Colors;

namespace Bloomlings.Playtest.Design
{
    /// <summary>The profile's pieces (spec 005 FR-037): the round avatar on its cream disc, in its frame and badge.</summary>
    public static partial class Kit
    {
        /// <summary>
        /// The profile avatar (Home's header, the profile page, the edit card; Unity's <c>ProfileAvatar</c>): a domed cream
        /// disc (<c>ui.button.round</c>) holding the avatar's round picture (<see cref="AvatarPicture"/>), in the chosen
        /// profile frame (1.08 of it) with the profile badge at its bottom left (0.36 of it). With
        /// <paramref name="action"/> it presses like a round button and a tap runs it.
        /// </summary>
        public static void Avatar(IPainter p, Box box, AvatarItem avatar, CosmeticItem? frame, CosmeticItem? badge, Action? action, Outfit? outfit = null)
        {
            p.Mark("ui.button.round");
            float size = box.Width;
            float depth = Press(p, box, action != null);
            Squash(p, box, depth);
            Box face = IconFace(p, box, GardenLook.White, size / 2f, depth);
            AvatarPicture(p, OwnerPictures.AvatarPicture(face), avatar, outfit);
            if (frame != null)
            {
                p.Shape("cosmetic.frame", Box.FromCenter(face.CenterX, face.CenterY, size * 1.08f, size * 1.08f), Visuals.Tint(frame));
            }

            if (badge != null)
            {
                p.Shape("cosmetic.badge", Box.FromCenter(face.CenterX - (size * 0.36f), face.CenterY + (size * 0.36f), size * 0.36f, size * 0.36f), Visuals.Tint(badge));
            }

            p.PopTransform();
            if (action != null)
            {
                p.Hit(Touch(p, box), action);
            }
        }

        /// <summary>
        /// An avatar's round picture in <paramref name="circle"/> (the owner's <c>Avatars/{picture}.jpg</c>, clipped round,
        /// in a thin <c>cream.line</c> ring), or, while it is missing, the avatar's family hero (in
        /// <paramref name="outfit"/>) on the soft green middle Home's avatar had before.
        /// </summary>
        public static void AvatarPicture(IPainter p, Box circle, AvatarItem avatar, Outfit? outfit = null)
        {
            p.Mark(OwnerPictures.AvatarSlot);
            string name = PainterBase.AvatarPrefix + avatar.Picture;
            float r = circle.Width / 2f;
            if (p.HasSprite(name))
            {
                p.PushClipRound(circle, r);
                p.Sprite(name, circle);
                p.PopClip();
            }
            else
            {
                p.FillRoundGradient(circle, r, GardenLook.Green.Top.Mix(C.CreamTop, 0.6f), GardenLook.Green.Face.Mix(C.CreamTop, 0.45f));
                Visuals.Hero(p, Box.FromCenter(circle.CenterX, circle.CenterY + (circle.Height * 0.02f), circle.Width * 0.82f, circle.Height * 0.82f), avatar.Family, outfit);
            }

            p.StrokeCircle(circle.CenterX, circle.CenterY, r, Math.Max(1f, p.U(DesignTokens.Garden.OutlineWidth)), C.CreamLine);
        }
    }
}

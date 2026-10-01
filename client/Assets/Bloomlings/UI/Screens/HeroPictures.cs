using Bloomlings.Client.Art;
using Bloomlings.Client.UI.Design;
using Bloomlings.Core.Variants;
using UnityEngine;
using UnityEngine.UI;

namespace Bloomlings.Client.UI.Screens
{
    /// <summary>
    /// The 3D hero group of spec 004 (FR-017) for the meta screens only: the splash, Home early on, and the win and
    /// milestone cards. Without the picture it shows the four family silhouettes in a row (FR-021).
    /// </summary>
    public static class HeroPictures
    {
        /// <summary>The four heroes on their stone pedestal, fitted into a new rect the caller places.</summary>
        public static RectTransform Group(string name, Transform parent)
        {
            RectTransform root = UiFactory.CreateRect(name, parent);
            Sprite? group = CharacterSprites.Group;
            if (group != null)
            {
                Image image = UiFactory.CreateImage("Heroes", root, group, Color.white);
                image.preserveAspect = true;
                image.raycastTarget = false;
                UiFactory.Stretch(image.rectTransform);
                return root;
            }

            VariantId[] colors = { VariantId.Leaf, VariantId.Flower, VariantId.Water, VariantId.Wood };
            for (int i = 0; i < CharacterArt.Families.Count; i++)
            {
                Family family = CharacterArt.Families[i];
                Color color = UiTheme.Of(Rgba.FromHex(VariantCatalog.Default.Get(colors[i]).ColorHex));
                Image body = UiFactory.CreateImage(family.ToString(), root, ProceduralSprites.Silhouette(family), color);
                body.preserveAspect = true;
                body.raycastTarget = false;
                UiFactory.Place(body.rectTransform, 0.06f + (i * 0.22f), 0.2f, 0.26f + (i * 0.22f), 0.8f);
            }

            return root;
        }

        /// <summary>
        /// The group standing on a card's top edge (<see cref="CharacterArt.GroupOnCard"/>), as a child of the card so it
        /// shows and pops with it. Nothing on a phone too short for it.
        /// </summary>
        public static void OnCard(RectTransform card, Box cardBox, float widthShare)
        {
            (float w, float h, Insets insets) = UiKit.ScreenFrame();
            Box? group = CharacterArt.GroupOnCard(cardBox, ScreenLayout.SafeArea(w, h, insets), DesignTokens.ScaleFor(w, h), widthShare);
            if (!group.HasValue)
            {
                return;
            }

            Box g = group.Value;
            RectTransform heroes = Group("Heroes", card);
            UiFactory.Place(heroes, (g.Left - cardBox.Left) / cardBox.Width, 1f - ((g.Bottom - cardBox.Top) / cardBox.Height), (g.Right - cardBox.Left) / cardBox.Width, 1f - ((g.Top - cardBox.Top) / cardBox.Height));
        }
    }
}

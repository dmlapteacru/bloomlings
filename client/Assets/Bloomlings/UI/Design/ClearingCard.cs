using System;

namespace Bloomlings.Client.UI.Design
{
    /// <summary>
    /// A clearing style's card on the Store's Animations tab (spec 005 FR-038 as amended on 2026-10-06, contracts/look.md
    /// §6.12; slot <c>ui.button.clearing</c>), both builds: the outfit card (<see cref="ReferenceStoreRegions.ClearingCard"/>)
    /// holding the style's live preview and its name, with its action button where an outfit card's cost pill hangs, a
    /// little wider (<see cref="Button"/>): the green "Buy" with the lotus and the price (<see cref="BuyParts"/>), the cream
    /// "Choose", or "Chosen" with a green check on a flat cream plate; before L40 the cost pill and the padlock stay. Engine-free.
    /// </summary>
    public static class ClearingCard
    {
        /// <summary>The button's height over the card's body (an outfit card's cost pill's, §4.6).</summary>
        public const float ButtonShare = 0.2f;

        /// <summary>The button's width over the card's (the cost pill's 0.78, wider for "Buy" and the price).</summary>
        public const float ButtonWidthShare = 0.94f;

        /// <summary>The letters' height over the button's (the cost pill's price).</summary>
        public const float LabelShare = 0.5f;

        /// <summary>The lotus' side over the button's height, and the gaps between the parts.</summary>
        public const float LotusShare = 0.66f;

        public const float GapShare = 0.12f;

        /// <summary>The room kept clear inside each end of the button, over its height.</summary>
        public const float PadShare = 0.3f;

        /// <summary>The card's body in its slot: above the room the button hangs into (an outfit card's with its pill's room).</summary>
        public static Box Body(Box slot) => new Box(slot.Left, slot.Top, slot.Right, slot.Top + (slot.Height / (1f + (0.6f * ButtonShare))));

        /// <summary>The action button over the body's bottom edge, centered (its middle 0.1 of its height below the edge).</summary>
        public static Box Button(Box slot)
        {
            Box body = Body(slot);
            float h = body.Height * ButtonShare;
            return Box.FromCenter(body.CenterX, body.Bottom + (h * 0.1f), body.Width * ButtonWidthShare, h);
        }

        /// <summary>The letters' size in pixels on <paramref name="button"/> (the hosts measure the label and the price at it).</summary>
        public static float LabelSize(Box button) => button.Height * LabelShare;

        /// <summary>
        /// The green "Buy" button's parts on <paramref name="button"/> (its face): "Buy", the lotus and the price, in that
        /// order and centered, from the label's and the price's widths measured at <see cref="LabelSize"/>; all three shrink
        /// together (<c>Scale</c>, at most 1) when they would not fit between the button's padded ends.
        /// </summary>
        public static (Box Label, Box Lotus, Box Price, float Scale) BuyParts(Box button, float labelWidth, float priceWidth)
        {
            float h = button.Height;
            float lotus = h * LotusShare;
            float gap = h * GapShare;
            float total = labelWidth + gap + lotus + (gap * 0.5f) + priceWidth;
            float room = Math.Max(1f, button.Width - (2f * h * PadShare));
            float scale = total > room ? room / total : 1f;
            float x = button.CenterX - (total * scale / 2f);
            float text = LabelSize(button) * scale * 1.4f;
            var label = new Box(x, button.CenterY - (text / 2f), x + (labelWidth * scale), button.CenterY + (text / 2f));
            x = label.Right + (gap * scale);
            Box icon = Box.FromCenter(x + (lotus * scale / 2f), button.CenterY, lotus * scale, lotus * scale);
            x = icon.Right + (gap * 0.5f * scale);
            var price = new Box(x, label.Top, x + (priceWidth * scale), label.Bottom);
            return (label, icon, price, scale);
        }
    }
}

using System;

namespace Bloomlings.Client.UI.Design
{
    /// <summary>A Daily Reward row's parts (<see cref="DailyRewardCard.Row"/>).</summary>
    public readonly struct DailyRowParts
    {
        public DailyRowParts(Box row, Box number, Box lotus, Box amount, Box button)
        {
            Row = row;
            Number = number;
            Lotus = lotus;
            Amount = amount;
            Button = button;
        }

        public Box Row { get; }

        /// <summary>The step's number, at the row's left.</summary>
        public Box Number { get; }

        /// <summary>The lotus before the amount.</summary>
        public Box Lotus { get; }

        /// <summary>The "+N", left-aligned after the lotus.</summary>
        public Box Amount { get; }

        /// <summary>At the row's right: the step's Claim, or the check or padlock badge in its middle.</summary>
        public Box Button { get; }
    }

    /// <summary>The Daily Reward card's body (<see cref="DailyRewardCard.Layout"/>).</summary>
    public sealed class DailyRewardRegions
    {
        public DailyRewardRegions(Box art, Box basket, Box[] rows, Box caption)
        {
            Art = art;
            Basket = basket;
            Rows = rows;
            Caption = caption;
        }

        /// <summary>The reward's light and lotus heap.</summary>
        public Box Art { get; }

        /// <summary>The woven basket under the heap.</summary>
        public Box Basket { get; }

        /// <summary>The steps' rows, step 1 at the top.</summary>
        public Box[] Rows { get; }

        /// <summary>The line under the rows: when the day's rewards come again.</summary>
        public Box Caption { get; }
    }

    /// <summary>
    /// The Daily Reward card (spec 005 FR-050, the owner, 2026-10-09: "always 5 claims every day ... follow the popups'
    /// design"; contracts/look.md §6.23) in the popups' look, shared by both builds: the reward basket with its lotus heap
    /// in the win's soft turning light, smaller than before; the day's five steps (spec 001 FR-055 as amended) as raised
    /// rows (<c>Kit.RaisedRow</c> / <c>UiKit.RaisedRow</c>), each with its number, the lotus and "+N", and at its right its
    /// Claim raised on the plate (an ad step's with the clapperboard before it, `GardenLook.AdMark`), green on the next step and greyed, not
    /// active, on a later one (the owner, 2026-10-09: "the Claim buttons always visible, but not active"), or a claimed
    /// step's green check; and under them when the day's rewards come again. Home's Daily
    /// scene wears the "!" badge (<see cref="BadgeDisc"/>) while a step waits and the card was not opened that day.
    /// Engine-free.
    /// </summary>
    public static class DailyRewardCard
    {
        public const float TopUnits = 6f;
        public const float ArtUnits = 236f;
        public const float ArtGapUnits = 14f;
        public const float RowUnits = 132f;
        public const float RowGapUnits = 16f;
        public const float CaptionGapUnits = 14f;
        public const float CaptionUnits = 60f;

        /// <summary>The Claim's "Claim" as a share of its face's content height (the owner, 2026-10-09: "the text and the camera much bigger").</summary>
        public const float ClaimTextShare = 0.8f;

        /// <summary>The clapperboard before an ad step's "Claim", as a share of the face's content height.</summary>
        public const float ClaimIconShare = 0.92f;

        /// <summary>The gap between the clapperboard and "Claim", as a share of the face's content height.</summary>
        public const float ClaimGapShare = 0.14f;

        /// <summary>The "+N" rising from a claimed row: its seconds.</summary>
        public const float RiseSeconds = 0.9f;

        /// <summary>The lotuses heaped over the basket: center offsets and sizes in shares of the art's width.</summary>
        public static readonly (float X, float Y, float S)[] Heap = { (-0.22f, 0.02f, 0.3f), (0.2f, 0.0f, 0.32f), (0f, -0.12f, 0.34f), (-0.08f, 0.1f, 0.28f), (0.12f, 0.12f, 0.26f) };

        /// <summary>The card's content height in reference units (<see cref="ScreenLayout.Card"/>) for <paramref name="steps"/> rows.</summary>
        public static float ContentUnits(int steps) =>
            TopUnits + ArtUnits + ArtGapUnits + (steps * RowUnits) + (Math.Max(0, steps - 1) * RowGapUnits) + CaptionGapUnits + CaptionUnits;

        /// <summary>The card's body laid out from its top (<paramref name="unit"/> one reference unit in pixels).</summary>
        public static DailyRewardRegions Layout(Box body, float unit, int steps)
        {
            float y = body.Top + (TopUnits * unit);
            float art = ArtUnits * unit;
            var artBox = new Box(body.CenterX - (art * 0.7f), y, body.CenterX + (art * 0.7f), y + art);
            var basket = Box.FromCenter(artBox.CenterX, artBox.CenterY + (artBox.Height * 0.2f), artBox.Width * 0.8f, artBox.Width * 0.6f);
            Box[] rows = ScreenLayout.Column(new Box(body.Left, artBox.Bottom + (ArtGapUnits * unit), body.Right, body.Bottom), steps, RowUnits * unit, RowGapUnits * unit);
            float top = rows.Length > 0 ? rows[rows.Length - 1].Bottom : artBox.Bottom;
            var caption = new Box(body.Left, top + (CaptionGapUnits * unit), body.Right, top + ((CaptionGapUnits + CaptionUnits) * unit));
            return new DailyRewardRegions(artBox, basket, rows, caption);
        }

        /// <summary>
        /// A step's row: its number in a box 0.48 of the row's height at the left, the lotus 0.62 of it after the number, the
        /// amount after the lotus up to the claim, and the claim 0.84 of the row tall and up to 0.42 of its width at the right.
        /// </summary>
        public static DailyRowParts Row(Box row)
        {
            float h = row.Height;
            float pad = h * 0.2f;
            var number = Box.FromCenter(row.Left + pad + (h * 0.24f), row.CenterY, h * 0.48f, h * 0.56f);
            float lotus = h * 0.62f;
            var lotusBox = Box.FromCenter(number.Right + (h * 0.12f) + (lotus / 2f), row.CenterY - (h * 0.02f), lotus, lotus);
            float width = Math.Min(row.Width * 0.42f, h * 2.8f);
            float right = row.Right - (pad * 0.5f);
            var button = new Box(right - width, row.CenterY - (h * 0.42f), right, row.CenterY + (h * 0.42f));
            var amount = new Box(lotusBox.Right + (h * 0.1f), row.Top, button.Left - (h * 0.15f), row.Bottom);
            return new DailyRowParts(row, number, lotusBox, amount, button);
        }

        /// <summary>A claimed step's check in the middle of its row's claim box: 0.72 of its height.</summary>
        public static Box Badge(Box button)
        {
            float s = button.Height * 0.72f;
            return Box.FromCenter(button.CenterX, button.CenterY, s, s);
        }

        /// <summary>
        /// The "!" badge over the top-right corner of Home's Daily scene's plate (<see cref="HomePromo.PlateBox"/>): a disc
        /// 0.3 of the plate's shorter side, its center 0.3 of the disc inside the corner.
        /// </summary>
        public static Box BadgeDisc(Box plate)
        {
            float s = Math.Min(plate.Width, plate.Height) * 0.3f;
            return Box.FromCenter(plate.Right - (s * 0.3f), plate.Top + (s * 0.3f), s, s);
        }

        /// <summary>The "!" badge's scale at <paramref name="seconds"/>: a soft pulse of 8% every 1.6 s.</summary>
        public static float BadgePulse(float seconds)
        {
            float t = (seconds % 1.6f) / 1.6f;
            return 1f + (0.08f * (0.5f - (0.5f * (float)Math.Cos(t * 2.0 * Math.PI))));
        }

        /// <summary>The "+N" rising from a claimed row at <paramref name="since"/> seconds: its rise in rows and its alpha, or null when gone.</summary>
        public static (float Rise, float Alpha)? Rise(float since)
        {
            if (since < 0f || since >= RiseSeconds)
            {
                return null;
            }

            float t = since / RiseSeconds;
            return (0.55f * (1f - ((1f - t) * (1f - t))), t < 0.6f ? 1f : 1f - ((t - 0.6f) / 0.4f));
        }

        /// <summary>A claimed step's check popping in at <paramref name="since"/> seconds after the claim (its scale).</summary>
        public static float CheckPop(float since)
        {
            if (since < 0f || since >= 0.35f)
            {
                return 1f;
            }

            float t = since / 0.35f;
            return t < 0.6f ? 1.18f * (t / 0.6f) : 1.18f - (0.18f * ((t - 0.6f) / 0.4f));
        }

        /// <summary>
        /// A duration's string and its numbers: <c>common.hours_minutes</c> with the hours and minutes from an hour on, else
        /// <c>common.minutes</c> with the minutes.
        /// </summary>
        public static (string Key, object[] Args) Duration(int minutes) =>
            minutes >= 60 ? ("common.hours_minutes", new object[] { minutes / 60, minutes % 60 }) : ("common.minutes", new object[] { Math.Max(1, minutes) });

        /// <summary>The caption's string under the rows: <c>daily_reward.next_in</c> while a step waits, else <c>daily_reward.all_claimed</c>.</summary>
        public static string CaptionKey(bool stepsLeft) => stepsLeft ? "daily_reward.next_in" : "daily_reward.all_claimed";
    }
}

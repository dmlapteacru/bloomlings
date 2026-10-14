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

    /// <summary>The shared calm shelf, five stations, one claim action and UTC reset caption.</summary>
    public sealed record DailyRewardRegions(Box Shelf,Box[] Rows,Box Claim,Box Caption);

    public static class DailyRewardCard
    {
        public const float TopUnits=30f, RowUnits=290f, RowGapUnits=10f, CaptionGapUnits=26f, CaptionUnits=70f;
        public const float RiseSeconds=.9f;
        public static float ContentUnits(int steps)=>TopUnits+RowUnits+40f+DesignTokens.Size.CardPrimaryHeight+CaptionGapUnits+CaptionUnits+20f;
        public static DailyRewardRegions Layout(Box body,float unit,int steps)
        {
            Box shelf=new Box(body.Left,body.Top+TopUnits*unit,body.Right,body.Top+(TopUnits+RowUnits)*unit);
            Box[] rows=ScreenLayout.Row(shelf,steps,RowGapUnits*unit,float.MaxValue,false);
            Box claim=ScreenLayout.CardButton(body,shelf.Bottom+40*unit,true,unit);
            Box caption=new Box(body.Left,claim.Bottom+CaptionGapUnits*unit,body.Right,claim.Bottom+(CaptionGapUnits+CaptionUnits)*unit);
            return new DailyRewardRegions(shelf,rows,claim,caption);
        }
        public static DailyRowParts Row(Box row)
        {
            float w=row.Width,h=row.Height;
            return new DailyRowParts(row,
                Box.FromCenter(row.CenterX,row.Top+h*.10f,w*.45f,h*.18f),
                Box.FromCenter(row.CenterX,row.Top+h*.35f,w*.64f,w*.64f),
                new Box(row.Left,row.Top+h*.48f,row.Right,row.Top+h*.68f),
                Box.FromCenter(row.CenterX,row.Top+h*.81f,w*.58f,h*.25f));
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

using System;
using Bloomlings.Client.Meta.Wardrobe;

namespace Bloomlings.Client.UI.Design
{
    /// <summary>Depth-masked 2D layers from the owner's modular Sprig. No runtime 3D or combination atlas.</summary>
    public static partial class SprigGearArt
    {
        public const string CapId = "hat.sprig_daisy";
        public const string GlassesId = "glasses.sprig_round";
        public const string BackpackId = "backpack.sprig_leaf";
        public const int LayerCount = 3;
        public static bool IsCap(Outfit? outfit) => outfit?.Hat?.Id == CapId;
        public static HeroFrame? Layer(Outfit? outfit, int index)
        {
            bool cap = IsCap(outfit);
            return index switch
            {
                0 => Frames[cap ? 1 : 0],
                1 => outfit?.Backpack?.Id == BackpackId ? Frames[cap ? 5 : 4] : (HeroFrame?)null,
                2 => outfit?.Glasses?.Id == GlassesId ? Frames[cap ? 3 : 2] : (HeroFrame?)null,
                _ => null,
            };
        }
        public static HeroFrame? Icon(string id) => id switch
        {
            CapId => Frames[6], GlassesId => Frames[7], BackpackId => Frames[8], _ => null,
        };
        public static bool IsTrial(string id) => id == CapId || id == GlassesId || id == BackpackId;

        /// <summary>One restrained shared breath until the separate action/animation feature is authored.</summary>
        public static Box PoseCell(Box cell, HeroPose pose, Bloomlings.Core.Variants.Family family = Bloomlings.Core.Variants.Family.Sprig)
        {
            int count = Math.Max(1, HeroMotion.FrameCount(family, pose.Clip));
            float wave = (float)Math.Sin(2 * Math.PI * pose.Index / count);
            float sx = 1 + wave * .003f, sy = 1 + wave * .006f;
            float foot = cell.Top + cell.Height * .9f;
            return new Box(cell.CenterX - cell.Width * sx / 2, foot - (foot - cell.Top) * sy,
                cell.CenterX + cell.Width * sx / 2, foot + (cell.Bottom - foot) * sy);
        }
    }
}

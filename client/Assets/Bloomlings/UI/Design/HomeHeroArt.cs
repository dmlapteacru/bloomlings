using System.Collections.Generic;
using Bloomlings.Client.Meta.Wardrobe;
using Bloomlings.Core.Variants;

namespace Bloomlings.Client.UI.Design
{
    /// <summary>Home's inward-facing views of the same fitted outfits. Other screens keep their frontal art.</summary>
    public static partial class HomeHeroArt
    {
        public static IReadOnlyList<HeroFrame> AllFrames => Frames;

        public static HeroFrame? Layer(Family family, Outfit? outfit, int index)
        {
            HeroFrame? front = HeroGearArt.Layer(family, outfit, index);
            return front.HasValue ? Frames[FrontToHome[front.Value.Name]] : (HeroFrame?)null;
        }
    }
}

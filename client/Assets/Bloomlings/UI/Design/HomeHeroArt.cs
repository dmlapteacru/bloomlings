using System.Collections.Generic;
using Bloomlings.Client.Meta.Wardrobe;
using Bloomlings.Core.Variants;

namespace Bloomlings.Client.UI.Design
{
    /// <summary>Home's inward-facing views of the same fitted outfits. Other screens keep their frontal art.</summary>
    public static partial class HomeHeroArt
    {
        private static IReadOnlyList<HeroFrame>? _allFrames;
        public static IReadOnlyList<HeroFrame> AllFrames
        {
            get
            {
                if(_allFrames==null){var all=new List<HeroFrame>(Frames);all.AddRange(EyewearArt.HomeFrames);all.AddRange(BloomExtrasArt.HomeFrames);all.AddRange(WardrobeAdditionsArt.HomeFrames);all.AddRange(WardrobeFinishArt.HomeFrames);_allFrames=all;}
                return _allFrames;
            }
        }

        public static HeroFrame? Layer(Family family, Outfit? outfit, int index)
        {
            if(WardrobeFinishArt.TryLayer(family,outfit,index,true,out var finished))return finished;
            if(WardrobeAdditionsArt.TryLayer(family,outfit,index,true,out var addition))return addition;
            if(family==Family.Bloom&&BloomExtrasArt.TryLayer(outfit,index,true,out var extra))return extra;
            if(index==3&&EyewearArt.IsItem(outfit?.Glasses?.Id))return EyewearArt.Layer(family,outfit,true);
            HeroFrame? front = HeroGearArt.Layer(family, outfit, index);
            return front.HasValue ? Frames[FrontToHome[front.Value.Name]] : (HeroFrame?)null;
        }
    }
}

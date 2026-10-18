using System.Collections.Generic;
using Bloomlings.Client.Meta.Wardrobe;
using Bloomlings.Core.Variants;

namespace Bloomlings.Client.UI.Design
{
    /// <summary>Owner's separate Head-bound glasses, depth-masked for each fitted outfit and Home angle.</summary>
    public static partial class EyewearArt
    {
        public const string SprigSquareId = "glasses.sprig_square";
        public const string BloomHeartId = "glasses.bloom_heart";
        public const string TwigOctagonId = "glasses.twig_octagon";
        private sealed record ItemSet(Family Family, IReadOnlyList<string> Heads, IReadOnlyList<string> Clothes,
            IReadOnlyList<string> Scarves, int[] Front, int[] Home, int Icon);
        public static IReadOnlyList<HeroFrame> AllFrames => Frames;
        public static IEnumerable<string> ItemIds => Items.Keys;
        public static bool IsItem(string? id) => id != null && Items.ContainsKey(id);
        public static HeroFrame? Icon(string id) => Items.TryGetValue(id,out ItemSet? item) ? Frames[item.Icon] : (HeroFrame?)null;
        public static HeroFrame? Layer(Family family,Outfit? outfit,bool home)
        {
            if(outfit?.Glasses==null||!Items.TryGetValue(outfit.Glasses.Id,out ItemSet? item)||item.Family!=family)return null;
            // Empty/legacy Head means the stock hair/hat, exactly as the existing base layer resolves it.
            int head=Find(item.Heads,outfit.Hat?.Id),clothes=Find(item.Clothes,outfit.Clothes?.Id)+1,scarf=Find(item.Scarves,outfit.Scarf?.Id)+1;
            if(head<0)head=0;
            int index=(head*(item.Clothes.Count+1)+clothes)*(item.Scarves.Count+1)+scarf;
            return Frames[(home?item.Home:item.Front)[index]];
        }
        private static int Find(IReadOnlyList<string> choices,string? id)
        {
            for(int i=0;i<choices.Count;i++)if(choices[i]==id)return i;
            return -1;
        }
    }
}

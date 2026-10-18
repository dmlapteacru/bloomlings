using System.Collections.Generic;
using Bloomlings.Client.Meta.Wardrobe;

namespace Bloomlings.Client.UI.Design
{
    /// <summary>Additional fitted Head and Body items, with masks for existing scarves and eyewear.</summary>
    public static partial class BloomExtrasArt
    {
        private sealed record Recipe(int Base, int Body, int Scarf, int Round, int Heart);
        public static IReadOnlyList<HeroFrame> AllFrames => Frames;
        public static IEnumerable<string> ItemIds => Icons.Keys;
        public static bool IsItem(string? id) => id != null && Icons.ContainsKey(id);
        public static bool IsHead(string? id) => Find(HeadIds,id)>=0;
        public static bool IsClothes(string? id) => Find(ClothesIds,id)>=0;
        public static HeroFrame? Icon(string id) => Icons.TryGetValue(id,out int i)?Frames[i]:(HeroFrame?)null;
        public static bool TryLayer(Outfit? outfit,int layer,bool home,out HeroFrame? frame)
        {
            frame=null;
            if(outfit==null||(!IsHead(outfit.Hat?.Id)&&!IsClothes(outfit.Clothes?.Id)))return false;
            // An existing Head with a new garment keeps its accepted base / Home counterpart.
            if(layer==0&&!IsHead(outfit.Hat?.Id))return false;
            int head=Find(Heads,outfit.Hat?.Id),body=Find(Bodies,outfit.Clothes?.Id),scarf=Find(Scarves,outfit.Scarf?.Id);
            if(head<0)head=0;if(body<0)body=0;if(scarf<0)scarf=0;
            Recipe? recipe=(home?Home:Front)[(head*Bodies.Length+body)*Scarves.Length+scarf];
            if(recipe==null)return false;
            int index=layer switch {0=>recipe.Base,1=>recipe.Body,2=>recipe.Scarf,
                3=>outfit.Glasses?.Id==BloomV2Art.GlassesId?recipe.Round:outfit.Glasses?.Id==EyewearArt.BloomHeartId?recipe.Heart:-1,_=>-1};
            if(index>=0)frame=Frames[index];
            return true;
        }
        private static int Find(IReadOnlyList<string> choices,string? id)
        {
            if(id==null)return -1;
            for(int i=0;i<choices.Count;i++)if(choices[i]==id)return i;
            return -1;
        }
    }
}

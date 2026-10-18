using System.Collections.Generic;
using Bloomlings.Client.Meta.Wardrobe;
using Bloomlings.Core.Variants;

namespace Bloomlings.Client.UI.Design
{
    /// <summary>Repaired garments for all heroes and fitted Drop headwear, with front and Home outfit masks.</summary>
    public static partial class WardrobeFinishArt
    {
        private sealed record Recipe(int Base,int Body,int Scarf,int EyeA,int EyeB);
        private sealed record FamilySet(string[] Heads,string[] Bodies,string[] Scarves,string[] Faces,Recipe?[] Front,Recipe?[] Home);
        public static IReadOnlyList<HeroFrame> AllFrames => Frames;
        public static IEnumerable<string> ItemIds => Icons.Keys;
        public static bool IsItem(string? id) => id!=null&&Icons.ContainsKey(id);
        public static bool IsHead(string? id) => Find(HeadIds,id)>=0;
        public static bool HasHead(Family family,string? id) => IsHead(id)&&Sets.TryGetValue(family,out FamilySet? set)&&Find(set.Heads,id)>=0;
        public static bool IsClothes(string? id) => Find(ClothesIds,id)>=0;
        public static HeroFrame? Icon(string id) => Icons.TryGetValue(id,out int i)?Frames[i]:(HeroFrame?)null;
        public static bool TryLayer(Family family,Outfit? outfit,int layer,bool home,out HeroFrame? frame)
        {
            frame=null;
            if(outfit==null||!Sets.TryGetValue(family,out FamilySet? set))return false;
            int head=Find(set.Heads,outfit.Hat?.Id),body=Find(set.Bodies,outfit.Clothes?.Id),scarf=Find(set.Scarves,outfit.Scarf?.Id);
            bool newHead=head>=0&&IsHead(outfit.Hat?.Id),newBody=body>=0&&IsClothes(outfit.Clothes?.Id);
            if(!newHead&&!newBody)return false;
            if(layer==0&&!newHead)return false; // Keep the accepted stock / existing Head base.
            if(head<0)head=0;if(body<0)body=0;if(scarf<0)scarf=0;
            Recipe? recipe=(home?set.Home:set.Front)[(head*set.Bodies.Length+body)*set.Scarves.Length+scarf];
            if(recipe==null)return false;
            int index=layer switch {0=>recipe.Base,1=>recipe.Body,2=>recipe.Scarf,
                3=>outfit.Glasses?.Id==set.Faces[0]?recipe.EyeA:outfit.Glasses?.Id==set.Faces[1]?recipe.EyeB:-1,_=>-1};
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

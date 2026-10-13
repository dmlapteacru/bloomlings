using System;
using Bloomlings.Client.App.Progression;

namespace Bloomlings.Client.UI.Design
{
    public static class PrimaryActionDesign
    {
        public const string Plain="navigation/game-primary",Decorated="navigation/game-primary-floral",Reward="navigation/game-reward";
        public const string Play="navigation/game-play";
        public static Box PlayArt(Box b)=>b;
        public const float BorderY=.30f;
        public static float BorderX(bool decorate)=>decorate ? .25f : .20f;
        public static TypeStyle Type(TypeStyle original)=>original with {Name="type.shop.primary",Bold=true,Outline=0};
        public static readonly TextLook Letters=new TextLook(Rgba.FromHex("#FFF9E8"),Rgba.FromHex("#FFF4DA"),Rgba.FromHex("#592C16"),.055f,.015f,.04f);
        public static readonly TextLook PlayLetters=new TextLook(Rgba.FromHex("#FFF9E8"),Rgba.FromHex("#FFF4DA"),Rgba.FromHex("#592C16"),.040f,.010f,.03f);
        public static readonly TypeStyle Status=new TypeStyle("type.shop.reward_progress",30,true,false,0,10);
        public static Box Content(Box b,bool decorate,bool hero=false)=>hero ? ProfileGameDesign.In(PlayArt(b),.17f,.18f,.83f,.82f) : ProfileGameDesign.In(b,decorate ? .17f : .08f,.13f,decorate ? .83f : .92f,.83f);
        public static Box RewardLabel(Box b)=>ProfileGameDesign.In(b,.18f,.25f,.71f,.51f);
        public static Box RewardLeaf(Box b,int index)
        {
            float step=b.Width*.071f,side=Math.Min(b.Height*.26f,step*.86f);
            return Box.FromCenter(b.Left+b.Width*.205f+index*step,b.Top+b.Height*.69f,side,side);
        }
        /// <summary>Decorative progress between real configured milestones; never grants or changes a reward.</summary>
        public static int FilledLeaves(MilestoneTable table,int next,int remaining)
        {
            int previous=0;
            foreach(var cadence in table.Cadences)previous=Math.Max(previous,((Math.Max(1,next)-1)/cadence.Every)*cadence.Every);
            int span=Math.Max(1,next-previous),done=Math.Max(0,Math.Min(span,span-remaining));
            return Math.Min(7,done*7/span);
        }
    }
}

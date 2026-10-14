using System;
using System.Globalization;
using Bloomlings.Client.Meta.Profile;
using Bloomlings.Client.Meta.Wardrobe;
using Bloomlings.Client.Services.Backend;

namespace Bloomlings.Client.UI.Design
{
    public sealed record LeaderboardHonorRegions(Box Safe,float W,PageHeader Header,Box Podium,Box Rows,Box Player,Box Status,Box Refresh,Box Viewport)
    {
        // Atlas edges include transparent space: pack rows closer while retaining portrait and label sizes.
        public float RowHeight=>W*.104f;
        public float RowFaceHeight=>W*.116f;
        public Box Row(int index)=>new Box(Rows.Left,Rows.Top+index*RowHeight,Rows.Right,Rows.Top+index*RowHeight+RowFaceHeight);
        public float ContentHeight(int count)=>count<=0?0:(count-1)*RowHeight+RowFaceHeight;
        public float Maximum(int count)=>Math.Max(0,ContentHeight(count)-Rows.Height);
        public Box Portrait(int rank)=>LeaderboardHonorDesign.In(Podium,rank==2?.18f:rank==1?.425f:.70f,rank==1?.095f:.22f,rank==2?.315f:rank==1?.565f:.835f,rank==1?.38f:.485f);
        public Box Medal(int rank)=>LeaderboardHonorDesign.In(Podium,rank==2?.22f:rank==1?.47f:.745f,rank==1?.425f:.54f,rank==2?.285f:rank==1?.535f:.81f,rank==1?.515f:.63f);
        public Box Name(int rank)=>LeaderboardHonorDesign.In(Podium,rank==2?.17f:rank==1?.415f:.69f,rank==1?.61f:.68f,rank==2?.34f:rank==1?.60f:.86f,rank==1?.71f:.78f);
        public Box Level(int rank)=>LeaderboardHonorDesign.In(Podium,rank==2?.19f:rank==1?.44f:.72f,rank==1?.725f:.79f,rank==2?.32f:rank==1?.58f:.84f,rank==1?.81f:.875f);
    }
    public static class LeaderboardHonorDesign
    {
        public static readonly TypeStyle NameType=new TypeStyle("type.shop.leaderboard_name",38,true,false,0,16);
        public static readonly TypeStyle SmallName=new TypeStyle("type.shop.leaderboard_podium_name",30,true,false,0,16);
        public static readonly TypeStyle Number=new TypeStyle("type.shop.leaderboard_rank",38,true,false,0,16);
        public static LeaderboardHonorRegions Page(float width,float height,Insets insets)
        {
            var page=ScreenLayout.ReferenceStore(width,height,insets,false,false);float w=page.W;
            float top=page.Header.Row.Bottom+w*.025f,nav=ScreenLayout.BottomNavTop(width,height,insets);
            float podiumHeight=Math.Min(w*.48f,(nav-top)*.33f);
            Box podium=Box.FromCenter(page.Safe.CenterX,top+podiumHeight*.5f,podiumHeight*1796f/876f,podiumHeight);
            float left=page.Safe.Left+w*.04f,right=page.Safe.Right-w*.04f;
            Box player=new Box(left,nav-w*.29f,right,nav-w*.165f);
            Box status=new Box(left,player.Bottom+w*.01f,right-w*.10f,nav-w*.02f);
            float touch=DesignTokens.Size.TouchMin*DesignTokens.ScaleFor(width,height);
            Box refresh=Box.FromCenter(right-touch*.5f,status.CenterY,touch,touch);
            Box rows=new Box(left,podium.Bottom+w*.015f,right,player.Top-w*.025f);
            return new LeaderboardHonorRegions(page.Safe,w,page.Header,podium,rows,player,status,refresh,rows);
        }
        public static (Box Rank,Box Avatar,Box Name,Box Level) Parts(Box row)
        {
            float portrait=row.Height*.78f;
            Box avatar=Box.FromCenter(row.Left+row.Width*.21f,row.CenterY,portrait,portrait);
            return (In(row,.015f,.18f,.12f,.82f),avatar,In(row,.285f,.13f,.73f,.87f),In(row,.755f,.21f,.97f,.79f));
        }
        public static ProfileLook Look(LeaderboardAppearance? appearance,CosmeticCatalog catalog)
        {
            CosmeticItem? Get(string? id,CosmeticKind kind)=>id!=null&&catalog.TryGet(id,out CosmeticItem? item)&&item!=null&&item.Kind==kind?item:null;
            return new ProfileLook(Get(appearance?.Frame,CosmeticKind.Frame),Get(appearance?.Badge,CosmeticKind.Badge),Get(appearance?.Marker,CosmeticKind.Marker));
        }
        public static Box NamePlate(Box maximum,float measuredWidth) => Box.FromCenter(maximum.CenterX,maximum.CenterY,Math.Min(maximum.Width,Math.Max(maximum.Height*1.8f,measuredWidth+maximum.Height*.5f)),maximum.Height);
        public static Box In(Box b,float l,float t,float r,float bottom)=>WardrobeShelfDesign.In(b,l,t,r,bottom);
        /// <summary>Keep the full model value. Ellipsize at grapheme boundaries without shrinking below the chosen font.</summary>
        public static string FitName(string? name,float width,Func<string,float> measure)
        {
            string full=string.IsNullOrWhiteSpace(name)?"":name.Trim().Replace("\r"," ").Replace("\n"," ");
            if(measure(full)<=width)return full;
            int[] starts=StringInfo.ParseCombiningCharacters(full);int low=0,high=starts.Length;
            while(low<high){int mid=(low+high+1)/2;int end=mid==starts.Length?full.Length:starts[mid];if(measure(full.Substring(0,end)+"…")<=width)low=mid;else high=mid-1;}
            return low==0?"…":full.Substring(0,low==starts.Length?full.Length:starts[low])+"…";
        }
    }
}

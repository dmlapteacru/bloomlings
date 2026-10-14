using System;
using System.Collections.Generic;
using Bloomlings.Client.Meta.Profile;

namespace Bloomlings.Client.UI.Design
{
    public sealed record ProfileGameEditRegions(Box Safe, float W, PageHeader Header, Box Tabs, Box Summary, Box Preview, Box PreviewName, Box Level, Box Grid, Box Button)
    {
        public Box Cell(int index, bool frames = false) => ProfileGameDesign.Cell(Grid,index,frames);
        public Box NameCard => Box.FromCenter(Grid.CenterX,Grid.Top+Math.Min(Grid.Height*.44f,W*.36f),Grid.Width*.90f,Math.Min(Grid.Height*.76f,W*.58f));
        public Box NameField => ProfileGameDesign.In(NameCard,.075f,.43f,.925f,.70f);
        public Box NameHint => ProfileGameDesign.In(NameCard,.08f,.74f,.92f,.92f);
        public Box Note => Box.FromCenter(Grid.CenterX,Grid.CenterY,Grid.Width*.88f,W*.11f);
    }

    /// <summary>One responsive composition for all four editor tabs and the approved profile landing page.</summary>
    public static class ProfileGameDesign
    {
        public const string Gold = "shop/profile-gold-v1";
        public const string Showcase = "shop/profile-showcase-v1";
        public const string LeafFrame = "shop/profile-leaf-v1";
        public const float ShowcaseWidth = 940f, ShowcaseHeight = 1672f;
        public static readonly TypeStyle Name = new TypeStyle("type.shop.profile_name",56,true,false,0,18);
        public static readonly TypeStyle Info = new TypeStyle("type.shop.profile_info",32,false,false,0,16);
        public static readonly TypeStyle Item = new TypeStyle("type.shop.profile_item",30,true,false,0,16);
        public static readonly TypeStyle Action = new TypeStyle("type.shop.profile_action",56,true,false,0,20);
        public static readonly TypeStyle Progress = new TypeStyle("type.shop.profile_progress",28,true,false,0,16);
        public static TextLook Ink => ShopBodyDesign.DarkLetters;
        public static byte[] EditSurface(int w,int h) => ShopBodyRaster.Framed(w,h,.28f,ShopBodyDesign.Green,ShopBodyDesign.Green,.05f,true,.15f,.006f,.7f);
        public static IEnumerable<Box> PencilLayers(Box button)
        {
            Box icon=button.Inset(button.Width*.18f);float edge=button.Width*.018f;
            yield return icon.Offset(-edge,0);yield return icon.Offset(edge,0);
            yield return icon.Offset(0,-edge);yield return icon.Offset(0,edge);yield return icon;
        }
        public static (int Left,int Top,int Width,int Height) GoldCrop(int w,int h) => (88*w/1254,100*h/1254,1079*w/1254,1054*h/1254);
        public static string? FrameAsset(string id) => id=="frame.daisy" ? "shop/profile-frame-daisy" : id=="frame.ivy" ? "shop/profile-frame-ivy" : null;
        public static (int Left,int Top,int Width,int Height) FrameCrop(string name,int w,int h)
        {
            Box b=name=="shop/profile-frame-daisy" ? new Box(116,88,828,782) : new Box(930,89,1650,778);
            return ((int)(b.Left*w/1774),(int)(b.Top*h/887),(int)(b.Width*w/1774),(int)(b.Height*h/887));
        }
        public static ProfileGameEditRegions Edit(float width,float height,Insets insets)
        {
            var store=ScreenLayout.ReferenceStore(width,height,insets,false,false);
            Box safe=store.Safe;float w=store.W;
            float left=safe.Left+w*.04f,right=safe.Right-w*.04f;
            float top=store.Header.Back.Bottom+w*.05f;
            Box tabs=new Box(left,top,right,top+w*.09f);
            Box summary=new Box(left,tabs.Bottom+w*.025f,right,tabs.Bottom+w*.345f);
            Box preview=Box.FromCenter(summary.Left+summary.Width*.19f,summary.CenterY,w*.245f,w*.245f);
            Box name=In(summary,.38f,.15f,.95f,.46f);
            Box level=In(summary,.39f,.52f,.88f,.85f);
            Box button=new Box(safe.Left+w*.12f,safe.Bottom-w*.20f,safe.Right-w*.12f,safe.Bottom-w*.045f);
            Box grid=new Box(left,summary.Bottom+w*.025f,right,button.Top-w*.035f);
            return new ProfileGameEditRegions(safe,w,store.Header,tabs,summary,preview,name,level,grid,button);
        }
        public static Box Cell(Box area,int index,bool frames=false)
        {
            int columns=frames ? 2 : 3;
            float gap=area.Width*.025f,side=(area.Width-gap*(columns-1))/columns,height=side*(frames ? 1.05f : 1.40f);
            float x=area.Left+(index%columns)*(side+gap),y=area.Top+(index/columns)*(height+gap);
            return new Box(x,y,x+side,y+height);
        }
        public static float ContentHeight(Box area,int count,bool frames) => Math.Max(area.Height,Cell(area,Math.Max(0,count-1),frames).Bottom-area.Top+area.Width*.022f);
        public static Box Picture(Box b,bool frames=false)
        {
            Box well=In(b,frames ? .17f : .09f,.055f,frames ? .83f : .91f,.64f);float side=Math.Min(well.Width,well.Height);
            return Box.FromCenter(well.CenterX,well.CenterY,side,side);
        }
        public static Box CellLabel(Box b) => In(b,.05f,.775f,.95f,.955f);
        public static Box CellPrice(Box b) => In(b,.06f,.67f,.94f,.97f);
        public static Box PriceHit(Box card,float touch) {Box b=CellPrice(card);return Box.FromCenter(b.CenterX,b.CenterY,b.Width,Math.Max(touch,b.Height));}
        public static Box CellCheck(Box b) => Box.FromCenter(b.Right-b.Width*.12f,b.Top+b.Width*.12f,b.Width*.22f,b.Width*.22f);
        public static Box AchievementPicture(Box b) => In(b,.075f,.025f,.925f,.73f);
        public static Box AchievementCount(Box b) => In(b,.20f,.705f,.81f,.825f);
        public static Box AchievementLabel(Box b) => In(b,.105f,.835f,.895f,.985f);
        public static Box AchievementMark(Box b) => Box.FromCenter(b.Right-b.Width*.13f,b.Top+b.Height*.78f,b.Width*.20f,b.Width*.20f);
        public static Box ShowcasePart(Box panel,float l,float t,float r,float bottom) => In(panel,l/ShowcaseWidth,t/ShowcaseHeight,r/ShowcaseWidth,bottom/ShowcaseHeight);
        public static Box Previous(ReferenceProfileRegions r) => ShowcasePart(r.Panel,118,605,196,679);
        public static Box Next(ReferenceProfileRegions r) => ShowcasePart(r.Panel,740,605,818,679);
        public static Box ShowcaseTitle(ReferenceProfileRegions r) => ShowcasePart(r.Panel,279,603,659,678);
        public static Box Pencil(Box button) => button.Inset(button.Width*.24f);
        public static Box AvatarTouch(Box avatar) => new Box(avatar.Left,avatar.Top,avatar.Right,avatar.Bottom-avatar.Height*.20f);
        public static Box ArrowTouch(Box face,float minimum) => Box.FromCenter(face.CenterX,face.CenterY-Math.Max(0,minimum*.5f-face.Height*.23f),Math.Max(face.Width,minimum),Math.Max(face.Height,minimum));
        public static ReferenceProfileRegions Page(float width,float height,Insets insets)
        {
            var store=ScreenLayout.ReferenceStore(width,height,insets,false,false);Box safe=store.Safe;float w=store.W;
            // One aspect-fitted illustration; labels and hits share its source coordinates on every phone.
            float top=store.Header.Row.Bottom+w*.012f;
            float scale=Math.Min(w*.96f/ShowcaseWidth,(safe.Bottom-w*.04f-top)/ShowcaseHeight);
            Box panel=new Box(safe.CenterX-ShowcaseWidth*scale*.5f,top,safe.CenterX+ShowcaseWidth*scale*.5f,top+ShowcaseHeight*scale);
            Box Part(float l,float t,float r,float bottom)=>ShowcasePart(panel,l,t,r,bottom);
            Box card=Part(126,0,814,589),avatar=Part(306,36,634,364);
            Box name=Part(312,357,570,414),edit=Part(585,340,675,431);
            Box id=Part(322,500,622,528),joined=Part(300,530,637,565);
            Box level=Part(350,435,582,478),title=Part(104,597,836,685);
            var tiles=new List<Box>();float[] xs={77,349,620},ys={695,956,1220};
            for(int i=0;i<9;i++){float x=xs[i%3],y=ys[i/3];tiles.Add(Part(x,y,x+238,y+231));}
            Box note=Part(250,1518,690,1570);
            return new ReferenceProfileRegions(safe,w,store.Header,panel,card,avatar,name,edit,id,joined,level,title,tiles,note);
        }
        public static string TabKey(ProfileTab tab) => tab switch {ProfileTab.Avatar=>"profile.tab_avatar",ProfileTab.Frame=>"profile.tab_frame",ProfileTab.Badge=>"profile.tab_badge",_=>"profile.tab_name"};
        public static Box In(Box b,float l,float t,float r,float bottom) => new Box(b.Left+b.Width*l,b.Top+b.Height*t,b.Left+b.Width*r,b.Top+b.Height*bottom);
    }
}

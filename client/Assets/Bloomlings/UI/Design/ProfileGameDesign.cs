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
        public static readonly TypeStyle Name = new TypeStyle("type.shop.profile_name",56,true,false,0,18);
        public static readonly TypeStyle Info = new TypeStyle("type.shop.profile_info",32,false,false,0,16);
        public static readonly TypeStyle Item = new TypeStyle("type.shop.profile_item",30,true,false,0,16);
        public static readonly TypeStyle Action = new TypeStyle("type.shop.profile_action",56,true,false,0,20);
        public static readonly TypeStyle Progress = new TypeStyle("type.shop.profile_progress",28,true,false,0,16);
        public static TextLook Ink => ShopBodyDesign.DarkLetters;
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
            Box summary=new Box(left,tabs.Bottom+w*.025f,right,tabs.Bottom+w*.265f);
            Box preview=In(summary,.07f,.10f,.33f,.90f);
            Box name=In(summary,.40f,.19f,.93f,.50f);
            Box level=In(summary,.40f,.57f,.71f,.80f);
            Box button=new Box(safe.Left+w*.16f,safe.Bottom-w*.17f,safe.Right-w*.16f,safe.Bottom-w*.045f);
            Box grid=new Box(left,summary.Bottom+w*.025f,right,button.Top-w*.035f);
            return new ProfileGameEditRegions(safe,w,store.Header,tabs,summary,preview,name,level,grid,button);
        }
        public static Box Cell(Box area,int index,bool frames=false)
        {
            int columns=frames ? 2 : 3;
            float gap=area.Width*.025f,side=(area.Width-gap*(columns-1))/columns,height=side*(frames ? .82f : 1.04f);
            float x=area.Left+(index%columns)*(side+gap),y=area.Top+(index/columns)*(height+gap);
            return new Box(x,y,x+side,y+height);
        }
        public static float ContentHeight(Box area,int count,bool frames) => Math.Max(area.Height,Cell(area,Math.Max(0,count-1),frames).Bottom-area.Top+area.Width*.022f);
        public static Box Picture(Box b,bool frames=false)
        {
            Box well=In(b,frames ? .17f : .09f,.055f,frames ? .83f : .91f,frames ? .73f : .75f);float side=Math.Min(well.Width,well.Height);
            return Box.FromCenter(well.CenterX,well.CenterY,side,side);
        }
        public static Box CellLabel(Box b) => In(b,.05f,.775f,.95f,.955f);
        public static Box CellPrice(Box b) => In(b,.08f,.765f,.92f,.955f);
        public static Box PriceHit(Box card,float touch) {Box b=CellPrice(card);return Box.FromCenter(b.CenterX,b.CenterY,b.Width,Math.Max(touch,b.Height));}
        public static Box CellCheck(Box b) => Box.FromCenter(b.Right-b.Width*.12f,b.Top+b.Width*.12f,b.Width*.22f,b.Width*.22f);
        public static Box AchievementPicture(Box b) => In(b,.07f,.04f,.93f,.66f);
        public static Box AchievementCount(Box b) => In(b,.14f,.66f,.86f,.80f);
        public static Box AchievementLabel(Box b) => In(b,.035f,.82f,.965f,.955f);
        public static Box AchievementMark(Box b) => Box.FromCenter(b.Right-b.Width*.13f,b.Top+b.Height*.755f,b.Width*.24f,b.Width*.24f);
        public static ReferenceProfileRegions Page(float width,float height,Insets insets)
        {
            var store=ScreenLayout.ReferenceStore(width,height,insets,false,false);Box safe=store.Safe;float w=store.W;
            float top=store.Header.Back.Bottom+w*.045f;
            float k=Math.Min(1,(safe.Bottom-w*.04f-top)/(w*1.64f));float u=w*k;
            float left=safe.CenterX-u*.46f,right=safe.CenterX+u*.46f;
            Box card=new Box(left,top,right,top+u*.46f);
            Box avatar=In(card,.08f,.085f,.39f,.71f);
            Box name=In(card,.46f,.13f,.80f,.30f),edit=Box.FromCenter(card.Right-u*.105f,name.CenterY,u*.09f,u*.09f);
            Box id=In(card,.46f,.36f,.93f,.47f),joined=In(card,.46f,.50f,.95f,.61f);
            Box level=In(card,.20f,.77f,.80f,.94f);
            Box title=new Box(left,card.Bottom+u*.035f,right,card.Bottom+u*.115f);
            var tiles=new List<Box>();float gap=u*.02f,side=(right-left-gap*2)/3,h=u*.305f;
            for(int i=0;i<9;i++){float x=left+(i%3)*(side+gap),y=title.Bottom+u*.035f+(i/3)*(h+gap);tiles.Add(new Box(x,y,x+side,y+h));}
            Box note=new Box(left,tiles[8].Bottom+u*.015f,right,tiles[8].Bottom+u*.067f);
            return new ReferenceProfileRegions(safe,w,store.Header,new Box(left,top,right,note.Bottom),card,avatar,name,edit,id,joined,level,title,tiles,note);
        }
        public static string TabKey(ProfileTab tab) => tab switch {ProfileTab.Avatar=>"profile.tab_avatar",ProfileTab.Frame=>"profile.tab_frame",ProfileTab.Badge=>"profile.tab_badge",_=>"profile.tab_name"};
        public static Box In(Box b,float l,float t,float r,float bottom) => new Box(b.Left+b.Width*l,b.Top+b.Height*t,b.Left+b.Width*r,b.Top+b.Height*bottom);
    }
}

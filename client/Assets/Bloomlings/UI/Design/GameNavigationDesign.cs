using System;

namespace Bloomlings.Client.UI.Design
{
    /// <summary>Approved illustrated controls; original pixels are runtime-cropped identically on every platform.</summary>
    public static class GameNavigationDesign
    {
        public const string Wallet="navigation/game-wallet",NameRibbon="navigation/game-name",Plus="navigation/game-plus";
        public static readonly TypeStyle Caption=new TypeStyle("type.shop.nav_caption",38,true,false,0,12);
        public static readonly TypeStyle ProfileName=new TypeStyle("type.shop.home_name",32,true,false,0,9);
        public static readonly Box[] Controls={new Box(48,109,460,516),new Box(561,109,974,516),new Box(1074,109,1487,516),new Box(36,565,431,946),new Box(466,611,999,914),new Box(1012,611,1515,915)};
        public static readonly Box[] Places={new Box(59,29,401,364),new Box(439,29,782,364),new Box(819,29,1165,364),new Box(1200,28,1544,364),new Box(1582,28,1924,364),new Box(51,378,406,753),new Box(433,378,788,753),new Box(815,379,1169,752),new Box(1195,379,1550,752),new Box(1577,378,1932,753)};
        public static readonly Box[] Header={new Box(29,200,973,477),new Box(1004,247,1716,434),new Box(1748,149,2136,537)};
        public static readonly Box[] Frames={new Box(67,59,679,648),new Box(762,47,1388,653),new Box(1474,42,2088,684)};
        public static bool Owns(string name)=>name.StartsWith("navigation/game-",StringComparison.Ordinal);
        public static string? Control(string shape)=>shape switch {"ui.back"=>"navigation/game-control-0","ui.settings"=>"navigation/game-control-1","ui.pause"=>"navigation/game-control-2","ui.close"=>"navigation/game-control-3",_=>null};
        public static string Speed(bool on)=>"navigation/game-control-"+(on ? "4" : "5");
        public static string Nav(NavPlace place,bool selected)=>"navigation/game-nav-"+(BottomNav.Order.IndexOfPlace(place)+(selected ? 5 : 0));
        public static string NameKey(NavPlace place)=>place switch {NavPlace.Shop=>"store.title",NavPlace.Home=>"pause.home",_=>BottomNav.Key(place)+".title"};
        public static string? Frame(string? id)=>id switch {null or "frame.wood_ring"=>"navigation/game-frame-0","frame.leaf_ring"=>"navigation/game-frame-1","frame.golden_ribbon"=>"navigation/game-frame-2",_=>null};
        public static string Resource(string name)=>"navigation/"+(name.StartsWith("navigation/game-control-",StringComparison.Ordinal) ? "controls-v3" : name.StartsWith("navigation/game-nav-",StringComparison.Ordinal) ? "primary-nav-v3" : name.StartsWith("navigation/game-frame-",StringComparison.Ordinal) ? "profile-rims-v3" : "header-parts-v3")+".png";
        public static (int Left,int Top,int Width,int Height) Crop(string name,int w,int h)
        {
            Box b;float sw,sh;
            if(name.StartsWith("navigation/game-control-",StringComparison.Ordinal)){b=Controls[int.Parse(name.Substring(24))];sw=1536;sh=1024;}
            else if(name.StartsWith("navigation/game-nav-",StringComparison.Ordinal)){b=Places[int.Parse(name.Substring(20))];sw=1983;sh=793;}
            else if(name.StartsWith("navigation/game-frame-",StringComparison.Ordinal)){b=Frames[int.Parse(name.Substring(22))];sw=2172;sh=724;}
            else {b=Header[name==Wallet ? 0 : name==NameRibbon ? 1 : 2];sw=2172;sh=724;}
            return ((int)(b.Left*w/sw),(int)(b.Top*h/sh),(int)(b.Width*w/sw),(int)(b.Height*h/sh));
        }
        public static Box NavButton(BottomNavRegions r,int index)
        {
            Box slot=r.PlaceBoxes[index];bool active=index==r.ActiveIndex;
            float aspect=active ? 375f/355 : 335f/342;
            bool compact=r.Safe.Height/r.Safe.Width<1.95f;
            float side=Math.Min(slot.Width*1.04f,r.W*(compact ? active ? .158f : .145f : active ? .19f : .175f));
            float height=Math.Min(side*aspect,r.Safe.Bottom-r.W*.012f-r.Top);side=height/aspect;
            return new Box(slot.CenterX-side*.5f,r.Safe.Bottom-r.W*.012f-height,slot.CenterX+side*.5f,r.Safe.Bottom-r.W*.012f);
        }
        public static Box NavCaption(Box button)=>ProfileGameDesign.In(button,.07f,.76f,.93f,.945f);
        public static Box NavTouch(BottomNavRegions r,int index){Box b=NavButton(r,index);return new Box(b.Left,b.Top,b.Right,r.Safe.Bottom);}
        public static Box ProfileRibbon(Box avatar)=>Box.FromCenter(avatar.CenterX,avatar.Bottom+avatar.Width*.042f,avatar.Width,avatar.Width*.264f);
        public static Box ProfileNameBox(Box ribbon,bool badge)=>ProfileGameDesign.In(ribbon,badge ? .30f : .13f,.14f,.87f,.86f);
        public static string FitName(string value,float width,Func<string,float> measure)
        {
            if(measure(value)<=width)return value;string prefix=value;
            while(prefix.Length>1&&measure(prefix.TrimEnd()+"…")>width)prefix=prefix.Substring(0,prefix.Length-1);
            return prefix.TrimEnd()+"…";
        }
        public static Box ProfileBounds(Box avatar)=>new Box(avatar.Left,avatar.Top,avatar.Right,ProfileRibbon(avatar).Bottom);
        public static Box SpeedLabel(Box button)=>ProfileGameDesign.In(button,.48f,.16f,.91f,.81f);
        private static int IndexOfPlace(this System.Collections.Generic.IReadOnlyList<NavPlace> places,NavPlace place){for(int i=0;i<places.Count;i++)if(places[i]==place)return i;return 2;}
    }
}

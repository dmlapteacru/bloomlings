using System;
using Bloomlings.Client.UI.Design;

namespace Bloomlings.Playtest.Design
{
    public static partial class Kit
    {
        public static void SlicedSprite(IPainter p, string name, Box b, float sourceX, float sourceY, float borderHeightShare = -1)
        {
            var size = p.SpriteSize(name); if (!size.HasValue) return;
            float w = size.Value.Width, h = size.Value.Height;
            float by = b.Height * (borderHeightShare < 0 ? sourceY : borderHeightShare);
            float bx = Math.Min(b.Width * 0.49f, by * (w * sourceX) / (h * sourceY));
            float[] sx = { 0, w * sourceX, w * (1 - sourceX), w }, sy = { 0, h * sourceY, h * (1 - sourceY), h };
            float[] dx = { b.Left, b.Left + bx, b.Right - bx, b.Right }, dy = { b.Top, b.Top + by, b.Bottom - by, b.Bottom };
            for (int y = 0; y < 3; y++) for (int x = 0; x < 3; x++)
            {
                float scaleX = (dx[x + 1] - dx[x]) / (sx[x + 1] - sx[x]), scaleY = (dy[y + 1] - dy[y]) / (sy[y + 1] - sy[y]);
                float cx = dx[x] + (w * .5f - sx[x]) * scaleX, cy = dy[y] + (h * .5f - sy[y]) * scaleY;
                p.PushClip(new Box(dx[x], dy[y], dx[x + 1], dy[y + 1]));
                p.PushSquash(1, scaleY / scaleX, cx, cy);
                p.Sprite(name, Box.FromCenter(cx, cy, w * scaleX, h * scaleX)); p.PopTransform(); p.PopClip();
            }
        }
        public static void NavigationSignArt(IPainter p, Box box, TypeStyle original)
        {
            p.Mark("ui.sign.wood"); p.Mark("font.shop");
            string asset = NavigationSkin.Sign(original);
            if (p.HasSprite(asset)) SlicedSprite(p, asset, box, NavigationSkin.SignBorderX, NavigationSkin.SignBorderY);
            else p.Picture("ui.sign.wood/laminate", box, (w, h) => UiRaster.LaminateSign(w, h, h * .28f));
        }
        public static void NavigationTab(IPainter p, Box box, string label, string icon, int index, bool selected, Action? action)
        {
            p.Mark("ui.tab"); p.Mark("font.shop");
            float depth = action != null ? Press(p, box, true) : 0;
            Squash(p, box, depth);
            if (selected)
            {
                Box face=NavigationSkin.SelectedFace(box);
                if(p.HasSprite(NavigationSkin.CreamSelected)) SlicedSprite(p,NavigationSkin.CreamSelected,face,NavigationSkin.CreamSelectedBorderX,NavigationSkin.CreamBorderY);
                else p.Picture("navigation.tab/selected",face,(w,h)=>NavigationSkin.Button(w,h,0,true));
            }
            float scale=box.Height*NavigationSkin.TabTextHeight/p.U(NavigationSkin.Tab.Size);
            var content=NavigationSkin.TabContent(box,p.MeasureText(label,NavigationSkin.Tab,scale));
            if(p.HasSprite(icon))p.Sprite(icon,content.Icon);
            Box text=content.Label; TextLook look=NavigationSkin.TabLook(selected);
            p.Text(label,text.CenterX,text.CenterY,NavigationSkin.Tab,look.FillTop,text.Width,scale,look);
            p.PopTransform(); if (action != null) p.Hit(Touch(p, box), action);
        }
        public static void NavigationBar(IPainter p, Box box, int count)
        {
            p.Mark("ui.tab");
            if(p.HasSprite(NavigationSkin.CreamBar)) SlicedSprite(p,NavigationSkin.CreamBar,box,NavigationSkin.CreamBarBorderX,NavigationSkin.CreamBorderY);
            else RaisedButton(p,box,GardenLook.Cream,.35f,0);
            for(int i=1;i<count;i++)
            {
                Box d=NavigationSkin.Divider(box,i,count);
                p.FillRound(d,d.Width*.5f,NavigationSkin.DividerInk);
                p.FillRound(new Box(d.Right,d.Top,d.Right+d.Width*.55f,d.Bottom),d.Width*.275f,NavigationSkin.DividerLight);
            }
        }
    }
}

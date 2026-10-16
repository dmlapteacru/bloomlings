using System;
using Bloomlings.Client.UI.Design;
using Bloomlings.Core.Variants;
using C = Bloomlings.Client.UI.Design.DesignTokens.Colors;
using T = Bloomlings.Client.UI.Design.DesignTokens.Type;

namespace Bloomlings.Playtest.Design
{
    /// <summary>A reversible playtest-only bench for the owner's original skeletal-motion experiment.</summary>
    public static class HeroLabScreen
    {
        public const float Content=1160;
        public static Box MotionButton(Box body,float unit,int index)
        {
            float gap=unit*16,w=(body.Width-gap)*.5f,top=body.Top+unit*(650+(index/2)*148);
            return new Box(body.Left+(index%2)*(w+gap),top,body.Left+(index%2)*(w+gap)+w,top+unit*132);
        }
        public static void Draw(IPainter p,DesignApp app,float since)
        {
            CardRegions r=Kit.Card(p,Content,PlaytestText.T("hero_lab.title"),app.CardClose,Kit.Pop(since));
            var state=app.HeroLab;
            Box caption=Box.FromCenter(r.Body.CenterX,r.Body.Top+p.U(38),r.Body.Width,p.U(60));
            p.Text(PlaytestText.T("hero_lab.sprig"),caption.CenterX,caption.CenterY,T.Caption,C.InkBrown,caption.Width);
            Box cell=Box.FromCenter(r.Body.CenterX,r.Body.Top+p.U(365),p.U(490),p.U(551.25f));
            // All frames share a camera, size and ground point. Warm only the selected clip, never every lab clip.
            if(p is IPictureWarmer warmer && warmer.WarmsPictures)
            {
                if(app.HeroLabWarmed!=state.Motion){app.HeroLabWarmed=state.Motion;app.HeroLabWarmNext=0;}
                // Palette frames decode synchronously: bound each warm pass instead of decoding a whole clip on a tap.
                int count=HeroLab.Count(state.Motion),start=app.HeroLabWarmNext,end=Math.Min(count,start+4);
                if(start<end)warmer.Warm(()=>{for(int i=start;i<end;i++){var f=HeroLab.Frame(state.Motion,i);p.Sprite(PainterBase.HeroMotionPrefix+f.Name,f.PictureBox(cell));}});
                app.HeroLabWarmNext=end;
            }
            var frame=state.At(app.Now);
            p.Mark("ui.hero_lab");
            p.Sprite(PainterBase.HeroMotionPrefix+frame.Name,frame.PictureBox(cell));
            if(state.Hat)
            {
                var hat=HeroMotion.Hat(cell,Family.Sprig,frame.HeadAnchor);
                // This one prop has its own Sprig fit; another head shape needs another saved fit.
                Box fitted=Box.FromCenter(hat.Box.CenterX,hat.Box.CenterY-cell.Height*.10f,hat.Box.Width*.90f,hat.Box.Height*.90f);
                p.PushRotate(hat.Degrees,fitted.CenterX,fitted.CenterY);
                p.Sprite("shop/accessory-bucket",fitted);
                p.PopTransform();
            }
            for(int i=0;i<HeroLab.Ids.Length;i++)
            {
                int selected=i;Box b=MotionButton(r.Body,p.Scale,i);
                string label=PlaytestText.T("hero_lab."+HeroLab.Ids[i]);
                Action pick=()=>state.Select(selected,app.Now);
                if(i==state.Motion)Kit.PrimaryButton(p,b,label,pick,PopupDesign.Label);
                else Kit.SecondaryButton(p,b,label,pick,style:PopupDesign.Label);
            }
            Box controls=new Box(r.Body.Left,r.Body.Top+p.U(950),r.Body.Right,r.Body.Top+p.U(1082));
            Box[] buttons=ScreenLayout.Row(controls,3,p.U(12),float.MaxValue,false);
            Kit.SecondaryButton(p,buttons[0],PlaytestText.T("hero_lab.replay"),()=>state.Restart(app.Now),style:PopupDesign.Label);
            Kit.SecondaryButton(p,buttons[1],state.Slow ? "0.5×" : "1×",()=>state.ToggleSpeed(app.Now),style:PopupDesign.Label);
            Kit.SecondaryButton(p,buttons[2],PlaytestText.T(state.Hat ? "hero_lab.hat_on" : "hero_lab.hat_off"),()=>state.Hat=!state.Hat,style:PopupDesign.Label);
            Box note=Box.FromCenter(r.Body.CenterX,r.Body.Top+p.U(1120),r.Body.Width,p.U(50));
            p.Text(PlaytestText.T("hero_lab.note"),note.CenterX,note.CenterY,T.Caption,C.InkBrownSoft,note.Width,.8f);
            Kit.EndCard(p);
        }
    }
}

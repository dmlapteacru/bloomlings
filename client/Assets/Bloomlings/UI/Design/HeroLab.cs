using System;
using System.Collections.Generic;

namespace Bloomlings.Client.UI.Design
{
    public readonly struct HeroLabFrame
    {
        public HeroLabFrame(string name,int x,int y,int w,int h,float hx,float hy,float tx,float ty)
        { Name=name; Crop=new Box(x,y,x+w,y+h); HeadX=hx; HeadY=hy; TopX=tx; TopY=ty; }
        public string Name { get; }
        public Box Crop { get; }
        public float HeadX { get; }
        public float HeadY { get; }
        public float TopX { get; }
        public float TopY { get; }
        public Box PictureBox(Box cell) => new Box(cell.Left+Crop.Left/ HeroLab.CellWidth*cell.Width,
            cell.Top+Crop.Top/HeroLab.CellHeight*cell.Height,cell.Left+Crop.Right/HeroLab.CellWidth*cell.Width,
            cell.Top+Crop.Bottom/HeroLab.CellHeight*cell.Height);
        public HeroFrame HeadAnchor => new HeroFrame(Name,0,0,0,0,HeadX,HeadY,TopX,TopY);
    }

    /// <summary>Read-only experimental frames and a local playback clock. No save, purchases or production clips change.</summary>
    public static partial class HeroLab
    {
        public const float RestSeconds=.65f;
        public static readonly string[] Ids={"look","hello","bow","bounce"};
        public static int Count(int motion) => Frames[Checked(motion)+1].Length;
        public static float Duration(int motion) => Count(motion)/(float)Fps;
        public static HeroLabFrame Idle => Frames[0][0];
        public static HeroLabFrame Frame(int motion,int index)
        {
            var frames=Frames[Checked(motion)+1];
            return frames[Math.Max(0,Math.Min(index,frames.Length-1))];
        }
        public static HeroLabFrame Sample(int motion,float seconds)
        {
            float duration=Duration(motion),phase=Math.Max(0,seconds)%(duration+RestSeconds);
            return phase>=duration ? Idle : Frame(motion,(int)(phase*Fps));
        }
        public static IEnumerable<HeroLabFrame> AllFrames(int motion)
        { foreach(var frame in Frames[Checked(motion)+1])yield return frame; yield return Idle; }
        private static int Checked(int motion) => motion>=0&&motion<Ids.Length ? motion : throw new ArgumentOutOfRangeException(nameof(motion));
    }

    public sealed class HeroLabPlayback
    {
        public int Motion { get; private set; }
        public float Started { get; private set; }
        public bool Slow { get; private set; }
        public bool Hat { get; set; }
        public void Select(int motion,float now) { _=HeroLab.Count(motion); Motion=motion; Restart(now); }
        public void Restart(float now) => Started=now;
        public void ToggleSpeed(float now) { Slow=!Slow; Restart(now); }
        public HeroLabFrame At(float now) => HeroLab.Sample(Motion,Math.Max(0,now-Started)*(Slow ? .5f : 1f));
    }
}

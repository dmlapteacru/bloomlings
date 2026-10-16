using System;
using System.Linq;
using Bloomlings.Client.UI.Design;
using NUnit.Framework;

namespace Bloomlings.Client.Tests
{
    public class HeroLabTests
    {
        [Test]
        public void EveryLabClip_HasRealFramesInsideOneSharedCell_AndACommonSeam()
        {
            Assert.That(HeroLab.Ids.Distinct().Count(), Is.EqualTo(4));
            for(int motion=0;motion<4;motion++)
            {
                Assert.That(HeroLab.Duration(motion), Is.EqualTo(3f));
                foreach(var frame in HeroLab.AllFrames(motion))
                {
                    Box crop=frame.Crop;
                    Assert.That(crop.Left, Is.GreaterThanOrEqualTo(4));
                    Assert.That(crop.Top, Is.GreaterThanOrEqualTo(4));
                    Assert.That(crop.Right, Is.LessThanOrEqualTo(HeroLab.CellWidth-4));
                    Assert.That(crop.Bottom, Is.LessThanOrEqualTo(HeroLab.CellHeight-4));
                    Assert.That(frame.TopY, Is.LessThan(frame.HeadY), "head anchor remains above the chin");
                }
                Assert.That(HeroLab.Sample(motion,3.2f).Name, Is.EqualTo(HeroLab.Idle.Name), "a pause separates the loops");
            }
        }

        [Test]
        public void SelectingReplayingAndSlowMotion_UseALocalClockWithoutAccumulatingPose()
        {
            var player=new HeroLabPlayback();
            player.Select(1,100);
            Assert.That(player.At(100).Name, Is.EqualTo(HeroLab.Frame(1,0).Name));
            Assert.That(player.At(101).Name, Is.EqualTo(HeroLab.Frame(1,HeroLab.Fps).Name));
            player.ToggleSpeed(101);
            Assert.That(player.At(102).Name, Is.EqualTo(HeroLab.Frame(1,HeroLab.Fps/2).Name));
            player.Restart(120);
            Assert.That(player.At(120).Name, Is.EqualTo(HeroLab.Frame(1,0).Name));
            player.Select(3,121);
            Assert.That(player.At(123).Name, Is.EqualTo(HeroLab.Frame(3,HeroLab.Fps).Name));
            Assert.Throws<ArgumentOutOfRangeException>(()=>player.Select(4,0));
        }
    }
}

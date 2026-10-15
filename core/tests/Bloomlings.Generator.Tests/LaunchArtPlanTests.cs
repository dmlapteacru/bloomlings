using System.Collections.Generic;
using System.Linq;
using Bloomlings.Core.Definitions;
using Bloomlings.Core.Progression;
using NUnit.Framework;

namespace Bloomlings.Generator.Tests
{
    public class LaunchArtPlanTests
    {
        [TestCase(1UL)] [TestCase(2UL)] [TestCase(42UL)]
        public void FullLaunchHasExactQuotasReadableIntroductionsAndRelief(ulong seed)
        {
            var pins = new Dictionary<int,(int,int)> { [41]=(14,16),[71]=(18,20),[601]=(24,24),[651]=(22,28),[701]=(30,32),[751]=(36,36) };
            LaunchArtPlan plan = LaunchArtPlan.Build(seed,pins);
            Assert.That(plan.Slots.Select(s=>s.Level),Is.EqualTo(Enumerable.Range(1,2500)));
            foreach(var band in LaunchArtPlan.Bands)
            {
                int[] expected=LaunchArtPlan.Quotas(band.Last-band.First+1,band.Shares);
                var slots=plan.Slots.Where(s=>s.Level>=band.First&&s.Level<=band.Last).ToArray();
                Assert.That(LaunchArtPlan.Groups.Select(g=>slots.Count(s=>s.SizeGroup==g)),Is.EqualTo(expected),$"L{band.First}–{band.Last}");
            }
            foreach(var slot in plan.Slots)
            {
                if(slot.RequiredMechanic!=null) Assert.That(BoardMechanicReadability.Allows(slot.RequiredMechanic,slot.Width,slot.Height),Is.True,$"L{slot.Level}");
                if(slot.Level<=1)continue;
                var previous=plan.Slots[slot.Level-2];
                Assert.That(slot.SizeGroup=="xl"&&previous.SizeGroup=="xl",Is.False,$"XL sequence L{slot.Level}");
                if(previous.Difficulty==DifficultyClass.SuperHard)
                {
                    Assert.That(slot.Difficulty,Is.EqualTo(DifficultyClass.Normal));
                    Assert.That(slot.Pace,Is.EqualTo("relief"));
                    Assert.That(slot.Width*slot.Height,Is.LessThanOrEqualTo(576));
                }
            }
            Assert.That(plan.Slots.First(s=>s.Width==36).Level,Is.InRange(75,100));
            Assert.That(plan.Slots.Skip(300).All(s=>s.SizeGroup!="compact"),Is.True);
            Assert.That(plan.Slots.Count(s=>s.Width==36),Is.LessThan(100));
            foreach(var pin in pins) Assert.That((plan.Slots[pin.Key-1].Width,plan.Slots[pin.Key-1].Height),Is.EqualTo(pin.Value));
            TestContext.Out.WriteLine($"launch schedule seed: {plan.ScheduleSeed}");
        }
    }
}

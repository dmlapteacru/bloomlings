using System;
using Bloomlings.Client.Gameplay.Slots;
using Bloomlings.Core.Slots;
using Bloomlings.Core.Variants;
using NUnit.Framework;

namespace Bloomlings.Client.Tests
{
    /// <summary>
    /// Where the Unity client shows each pod in the Waiting Slots row (<see cref="SlotPlaces"/>; the owner's report of
    /// 2026-10-03): a committed pod never lands on a plate that still shows the last, finishing pod.
    /// </summary>
    public class SlotPlacesTests
    {
        /// <summary>The five slots of a level before Extra Slot (the sixth is absent).</summary>
        private static readonly Func<int, bool> FiveSlots = slot => slot < WaitingSlots.DefaultCount;

        [Test]
        public void Commit_ShowsInTheRulesSlot_WhenThatPlateShowsNoPod()
        {
            var places = new SlotPlaces();
            Assert.That(places.Commit(0, "a", VariantId.Leaf, 3, FiveSlots, out bool a), Is.EqualTo(0));
            Assert.That(places.Commit(2, "b", VariantId.Moss, 2, FiveSlots, out bool b), Is.EqualTo(2));
            Assert.That(a && b, Is.True);
            Assert.That(places[2].PodId, Is.EqualTo("b"));
            Assert.That(places[2].Variant, Is.EqualTo(VariantId.Moss));
            Assert.That(places[2].Count, Is.EqualTo(2));
        }

        [Test]
        public void Commit_WhileTheRulesSlotStillShowsAFinishingPod_ShowsOnAnotherPlate()
        {
            var places = new SlotPlaces();
            places.Commit(0, "a", VariantId.Leaf, 3, FiveSlots, out _);

            // The rules freed slot 0 as "a" finished; its last Bloomlings are still on their way, so it still shows.
            Assert.That(places.Complete("a"), Is.EqualTo(0));
            Assert.That(places[0].Leaving, Is.True);
            int b = places.Commit(0, "b", VariantId.Moss, 2, FiveSlots, out bool shown);
            Assert.That(b, Is.EqualTo(1));
            Assert.That(shown, Is.True);
            Assert.That(places.PlaceOf("a"), Is.EqualTo(0));

            Assert.That(places.FinishLeave(0), Is.False);
            Assert.That(places[0].PodId, Is.Null);
            Assert.That(places.PlaceOf("b"), Is.EqualTo(1), "a shown pod never moves");
        }

        [Test]
        public void Commit_SkipsPlatesThatAreLockedOrAbsent()
        {
            var places = new SlotPlaces();
            Func<int, bool> usable = slot => slot != 1 && slot != 3 && slot < WaitingSlots.DefaultCount;
            places.Commit(0, "a", VariantId.Leaf, 3, usable, out _);
            places.Complete("a");
            Assert.That(places.Commit(0, "b", VariantId.Moss, 2, usable, out _), Is.EqualTo(2), "slot 1 is locked");
            Assert.That(places.Commit(3, "c", VariantId.Dew, 1, usable, out _), Is.EqualTo(4), "slot 3 is still drawn locked");
            Assert.That(places.Commit(0, "d", VariantId.Wood, 1, usable, out bool shown), Is.EqualTo(0), "no usable plate is free: it waits in its slot");
            Assert.That(shown, Is.False);
            Assert.That(places[0].Waiting, Is.EqualTo(1));
            Assert.That(places.FreeOnScreen(usable), Is.EqualTo(0));
        }

        [Test]
        public void AWaitingPod_CountsDownWhereItWaits_AndTakesTheFirstPlateThatFrees()
        {
            SlotPlaces places = Full();
            places.Complete("p2");
            Assert.That(places.Commit(2, "f", null, 3, FiveSlots, out bool shown), Is.EqualTo(2));
            Assert.That(shown, Is.False, "every plate still shows a pod");
            Assert.That(places.Decrement("f"), Is.EqualTo(-1));
            Assert.That(places.Reveal("f", VariantId.Moss), Is.EqualTo(-1));

            places.Complete("p3");
            Assert.That(places.FinishLeave(3), Is.True, "it takes the waiting pod of another plate");
            Assert.That(places[3].PodId, Is.EqualTo("f"));
            Assert.That(places[3].Count, Is.EqualTo(2));
            Assert.That(places[3].Variant, Is.EqualTo(VariantId.Moss));
            Assert.That(places[2].PodId, Is.EqualTo("p2"));
            Assert.That(places[2].Waiting, Is.EqualTo(0));
            Assert.That(places.FinishLeave(2), Is.False);
        }

        [Test]
        public void AFreedPlate_TakesTheNextPodOfItsOwnQueueFirst()
        {
            SlotPlaces places = Full();
            places.Complete("p1");
            places.Complete("p3");
            places.Commit(1, "g", VariantId.Leaf, 1, FiveSlots, out _);
            places.Commit(3, "h", VariantId.Leaf, 1, FiveSlots, out _);
            Assert.That(places.FinishLeave(3), Is.True);
            Assert.That(places[3].PodId, Is.EqualTo("h"));
            Assert.That(places.FinishLeave(1), Is.True);
            Assert.That(places[1].PodId, Is.EqualTo("g"));
        }

        [Test]
        public void APodThatFinishesWhileWaiting_NeverShows()
        {
            SlotPlaces places = Full();
            places.Complete("p0");
            places.Commit(0, "f", VariantId.Leaf, 1, FiveSlots, out _);
            Assert.That(places.Complete("f"), Is.EqualTo(-1));
            Assert.That(places[0].Waiting, Is.EqualTo(0));
            Assert.That(places.FinishLeave(0), Is.False);
            Assert.That(places.PlaceOf("f"), Is.EqualTo(-1));
        }

        [Test]
        public void AnOpenedLock_LetsItsPlateTakeAWaitingPod()
        {
            var places = new SlotPlaces();
            Func<int, bool> beforeKey = slot => slot < 4;
            for (int i = 0; i < 4; i++)
            {
                places.Commit(i, "p" + i, VariantId.Leaf, 2, beforeKey, out _);
            }

            places.Complete("p0");
            places.Commit(0, "f", VariantId.Leaf, 1, beforeKey, out _);
            Assert.That(places.TakeWaiting(1), Is.False, "a plate showing a pod takes none");
            Assert.That(places.TakeWaiting(4), Is.True);
            Assert.That(places[4].PodId, Is.EqualTo("f"));
        }

        [Test]
        public void Rebuild_KeepsEachStayingPodsPlate_AndAPodNotShownTakesOne()
        {
            var places = new SlotPlaces();
            places.Commit(0, "a", VariantId.Leaf, 1, FiveSlots, out _);
            places.Complete("a");
            places.Commit(0, "b", VariantId.Moss, 5, FiveSlots, out _); // plate 1
            places.Commit(1, "c", VariantId.Dew, 2, FiveSlots, out _); // plate 2
            places.Commit(2, "d", VariantId.Wood, 1, FiveSlots, out _); // plate 3
            Assert.That(new[] { places.PlaceOf("b"), places.PlaceOf("c"), places.PlaceOf("d") }, Is.EqualTo(new[] { 1, 2, 3 }));

            // Return took "c" back (rules slot 1); "a" was still leaving when the timeline was flushed.
            places.Rebuild(new (string, int, VariantId?, int)[] { ("b", 0, VariantId.Moss, 4), ("d", 2, VariantId.Wood, 1) }, FiveSlots);
            Assert.That(places[0].PodId, Is.Null, "the leaving pod is gone");
            Assert.That(places[1].PodId, Is.EqualTo("b"));
            Assert.That(places[1].Count, Is.EqualTo(4));
            Assert.That(places[2].PodId, Is.Null, "the returned pod frees its plate");
            Assert.That(places[3].PodId, Is.EqualTo("d"));

            // A pod the rules hold but that is not shown yet takes a plate, as a committed one does.
            places.Rebuild(new (string, int, VariantId?, int)[] { ("b", 0, VariantId.Moss, 4), ("e", 1, VariantId.Leaf, 3), ("d", 2, VariantId.Wood, 1) }, FiveSlots);
            Assert.That(places.PlaceOf("e"), Is.EqualTo(0), "its rules slot shows another pod");
            Assert.That(places.PlaceOf("b"), Is.EqualTo(1));
            Assert.That(places.PlaceOf("d"), Is.EqualTo(3));
        }

        [Test]
        public void Rebuild_FromNothing_ShowsEachPodInItsRulesSlot()
        {
            var places = new SlotPlaces();
            places.Rebuild(new (string, int, VariantId?, int)[] { ("x", 3, VariantId.Leaf, 2), ("y", 0, VariantId.Moss, 1) }, FiveSlots);
            Assert.That(places.PlaceOf("x"), Is.EqualTo(3));
            Assert.That(places.PlaceOf("y"), Is.EqualTo(0));
        }

        [Test]
        public void FreeOnScreen_CountsTheUsablePlatesThatShowNoPod()
        {
            var places = new SlotPlaces();
            places.Commit(0, "a", VariantId.Leaf, 1, FiveSlots, out _);
            places.Complete("a");
            places.Commit(0, "b", VariantId.Moss, 1, FiveSlots, out _);

            // The rules hold one pod (four slots free), but two plates show one: three are free on screen.
            Assert.That(places.FreeOnScreen(FiveSlots), Is.EqualTo(3));
        }

        /// <summary>Five plates, each showing pod "p{slot}" in its rules' slot.</summary>
        private static SlotPlaces Full()
        {
            var places = new SlotPlaces();
            for (int i = 0; i < WaitingSlots.DefaultCount; i++)
            {
                places.Commit(i, "p" + i, VariantId.Leaf, 2, FiveSlots, out _);
            }

            return places;
        }
    }
}

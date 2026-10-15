using System.Collections.Generic;

namespace Bloomlings.Core.Progression
{
    public sealed record HeroMilestone(int Level, string Hero, string PictureId);

    /// <summary>The four unique hero mosaics in the launch, each on a maximum-sized board.</summary>
    public static class HeroMilestones
    {
        public static IReadOnlyList<HeroMilestone> All { get; } = new[]
        {
            new HeroMilestone(100, "sprig", "hero_sprig"),
            new HeroMilestone(500, "bloom", "hero_bloom"),
            new HeroMilestone(1000, "drop", "hero_drop"),
            new HeroMilestone(2000, "twig", "hero_twig"),
        };
    }
}

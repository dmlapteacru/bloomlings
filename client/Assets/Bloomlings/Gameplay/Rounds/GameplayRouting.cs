using Bloomlings.Client.Services.Content;

namespace Bloomlings.Client.Gameplay.Rounds
{
    /// <summary>What the Gameplay scene plays (<see cref="GameplayRouting.For"/>).</summary>
    public enum GameplayRoute
    {
        /// <summary>A round level on the round rules (spec 006 US5): <see cref="RoundGameplayController"/>.</summary>
        Round,

        /// <summary>A legacy level or the Daily Challenge on the legacy rules: <see cref="GameplayController"/>.</summary>
        Legacy,

        /// <summary>No attempt: the scene was played directly in the Editor, so the dev level picked in its menu plays.</summary>
        DevLevel,
    }

    /// <summary>
    /// Which rules the Gameplay scene plays (spec 006 T058): a round attempt always plays on the round rules and never falls
    /// back to a legacy or dev level; a legacy attempt plays as before; only a scene opened with no attempt at all plays the
    /// Editor's dev level. Engine-free.
    /// </summary>
    public static class GameplayRouting
    {
        public static GameplayRoute For(RoundAttempt? round, LevelAttempt? legacy) =>
            round != null ? GameplayRoute.Round : legacy != null ? GameplayRoute.Legacy : GameplayRoute.DevLevel;
    }
}

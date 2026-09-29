using Bloomlings.Core.Boards;
using Bloomlings.Core.Simulation;

namespace Bloomlings.Core.Mechanics
{
    /// <summary>
    /// Mystery tiles (FR-039, T103). A mystery tile hides its exact variant until it becomes reachable; it then reveals
    /// before allocation, so pods can claim it in the same round (<c>MysteryTileRevealed</c>). Tiles reachable at the
    /// start are revealed when the level loads. Hidden variants are fixed in the level data, count in the exact
    /// accounting, and are never shown by <see cref="LevelView"/> until revealed. Mystery pods reveal on commit
    /// (<see cref="LevelSession"/>).
    /// </summary>
    internal sealed class Mystery : IRoundHook
    {
        public static readonly Mystery Instance = new Mystery();

        private Mystery()
        {
        }

        public void OnStart(LevelState state)
        {
            ReachabilityResult reach = Reachability.Compute(state.Board);
            foreach (ReachableTarget target in reach.Targets)
            {
                state.RevealMysteryTile(target.Index);
            }
        }

        public void BeforeAllocation(RoundContext context)
        {
            LevelState state = context.State;
            foreach (ReachableTarget target in context.Reach.Targets)
            {
                if (state.Board.IsMysteryHidden(target.Index))
                {
                    state.RevealMysteryTile(target.Index);
                    context.Events.Add(new MysteryTileRevealed(context.Round, target.Cell, state.Board.TopLayer(target.Index)));
                    context.Changed = true;
                }
            }
        }

        public void OnLayerCleared(RoundContext context, int cell, LayerClearResult result, int pod)
        {
        }

        public void AfterClears(RoundContext context)
        {
        }

        public bool BlocksWin(LevelState state) => false;
    }
}

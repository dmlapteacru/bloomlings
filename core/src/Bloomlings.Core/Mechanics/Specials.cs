using System.Collections.Generic;
using Bloomlings.Core.Boards;
using Bloomlings.Core.Definitions;
using Bloomlings.Core.Simulation;

namespace Bloomlings.Core.Mechanics
{
    /// <summary>
    /// Special objects (FR-037, FR-038; T101): the Garden Gate / heavy blocker and the Fountain. Each has a visible
    /// condition with a counter:
    /// <list type="bullet">
    /// <item><c>key</c>: its key is collected (0/1);</item>
    /// <item><c>clear_count_adjacent</c>: layers cleared on the cells around it, of the exact variant when one is given
    /// (a Fountain always names one), e.g. "restore 6 Water around it";</item>
    /// <item><c>clear_region</c>: the region's cells restored.</item>
    /// </list>
    /// After the round's clears the counters update (<c>SpecialProgressed</c>, once per changed special) and a met
    /// condition triggers the effect (<c>SpecialTriggered</c>): <c>open_cells</c> opens the listed non-target cells,
    /// <c>remove_stones</c> turns the listed stones into open ground, and <c>reveal_layers</c> reveals the listed mystery
    /// tiles. A Gate also opens its own cells; a Fountain stays as a landmark. The next round sees the new routes. An
    /// untriggered special blocks the win (FR-025).
    /// </summary>
    internal sealed class Specials : IRoundHook
    {
        public static readonly Specials Instance = new Specials();

        private Specials()
        {
        }

        public void OnStart(LevelState state)
        {
        }

        public void BeforeAllocation(RoundContext context)
        {
            context.SpecialProgressAtStart = (int[])context.State.SpecialProgress.Clone();
        }

        public void OnLayerCleared(RoundContext context, int cell, LayerClearResult result, int pod)
        {
            LevelState state = context.State;
            foreach (int s in state.Mechanics.SpecialsNextTo(cell))
            {
                SpecialRule rule = state.Mechanics.Specials[s];
                if (state.SpecialTriggered[s] || state.SpecialProgress[s] >= rule.Total)
                {
                    continue;
                }

                if (rule.Def.Condition.Variant == null || rule.Def.Condition.Variant.Value == result.Cleared)
                {
                    state.SetSpecialProgress(s, state.SpecialProgress[s] + 1);
                }
            }
        }

        public void AfterClears(RoundContext context)
        {
            LevelState state = context.State;
            SpecialRule[] rules = state.Mechanics.Specials;
            int[] before = context.SpecialProgressAtStart ?? state.SpecialProgress;
            for (int s = 0; s < rules.Length; s++)
            {
                if (state.SpecialTriggered[s])
                {
                    continue;
                }

                SpecialRule rule = rules[s];
                int progress = state.SpecialProgress[s];
                switch (rule.Def.Condition.Kind)
                {
                    case SpecialConditionKind.Key:
                        progress = state.IsKeyCollected(rule.Def.Condition.KeyId!) ? 1 : 0;
                        break;
                    case SpecialConditionKind.ClearRegion:
                        progress = 0;
                        foreach (int cell in rule.RegionCells)
                        {
                            if (!state.Board.IsTarget(cell))
                            {
                                progress++;
                            }
                        }

                        break;
                }

                if (progress != state.SpecialProgress[s])
                {
                    state.SetSpecialProgress(s, progress);
                }

                if (progress != before[s])
                {
                    context.Events.Add(new SpecialProgressed(context.Round, rule.Def.Id, progress, rule.Total));
                }

                if (progress >= rule.Total)
                {
                    Trigger(context, s, rule);
                }
            }
        }

        public bool BlocksWin(LevelState state)
        {
            foreach (bool triggered in state.SpecialTriggered)
            {
                if (!triggered)
                {
                    return true;
                }
            }

            return false;
        }

        private static void Trigger(RoundContext context, int s, SpecialRule rule)
        {
            LevelState state = context.State;
            state.MarkSpecialTriggered(s);
            var changed = new List<CellPos>();
            if (rule.OpensOwnCells)
            {
                foreach (int cell in rule.OwnCells)
                {
                    Open(state, cell, changed);
                }
            }

            foreach (int cell in rule.EffectCells)
            {
                if (rule.Def.Effect.Kind == SpecialEffectKind.RevealLayers)
                {
                    if (state.Board.IsMysteryHidden(cell))
                    {
                        state.RevealMysteryTile(cell);
                        context.Events.Add(new MysteryTileRevealed(context.Round, state.Board.PosOf(cell), state.Board.TopLayer(cell)));
                        changed.Add(state.Board.PosOf(cell));
                    }
                }
                else
                {
                    Open(state, cell, changed);
                }
            }

            context.Events.Add(new SpecialTriggered(context.Round, rule.Def.Id, changed));
            context.Changed = true;
        }

        private static void Open(LevelState state, int cell, List<CellPos> changed)
        {
            if (state.Board.KindAt(cell) == CellKind.Stone || state.Board.KindAt(cell) == CellKind.Special)
            {
                state.OpenNonTargetCell(cell);
                changed.Add(state.Board.PosOf(cell));
            }
        }
    }
}

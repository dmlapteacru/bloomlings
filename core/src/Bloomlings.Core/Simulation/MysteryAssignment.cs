using System;
using System.Collections.Generic;
using Bloomlings.Core.Boards;
using Bloomlings.Core.Definitions;
using Bloomlings.Core.Variants;

namespace Bloomlings.Core.Simulation
{
    /// <summary>
    /// Variants for the hidden parts of a level: the top layer of mystery tiles and the variant of mystery pods. Used to
    /// load the worlds the player cannot tell apart (<see cref="LevelSession.LoadHypothesis"/>, R8).
    /// </summary>
    public sealed class MysteryAssignment
    {
        public MysteryAssignment(IReadOnlyDictionary<CellPos, VariantId> tiles, IReadOnlyDictionary<string, VariantId> pods)
        {
            Tiles = tiles ?? throw new ArgumentNullException(nameof(tiles));
            Pods = pods ?? throw new ArgumentNullException(nameof(pods));
        }

        public IReadOnlyDictionary<CellPos, VariantId> Tiles { get; }

        public IReadOnlyDictionary<string, VariantId> Pods { get; }

        /// <summary>Replaces the top layers on the board and returns the definition with the pods' variants replaced.</summary>
        internal LevelDefinition Apply(LevelDefinition definition, Board board)
        {
            foreach (KeyValuePair<CellPos, VariantId> tile in Tiles)
            {
                int index = board.IndexOf(tile.Key);
                if (!board.IsTarget(index) || !board.IsMysteryHidden(index))
                {
                    throw new InvalidLevelException($"Cell {tile.Key} is not a mystery tile.");
                }

                board.ReplaceTop(index, tile.Value);
            }

            if (Pods.Count == 0)
            {
                return definition;
            }

            var pods = new PodDef[definition.Pods.Count];
            int replaced = 0;
            for (int i = 0; i < pods.Length; i++)
            {
                PodDef pod = definition.Pods[i];
                if (Pods.TryGetValue(pod.Id, out VariantId variant))
                {
                    if (!pod.Mystery)
                    {
                        throw new InvalidLevelException($"Pod '{pod.Id}' is not a mystery pod.");
                    }

                    pod = pod with { Variant = variant };
                    replaced++;
                }

                pods[i] = pod;
            }

            if (replaced != Pods.Count)
            {
                throw new InvalidLevelException("The assignment names unknown pods.");
            }

            return definition with { Pods = pods };
        }
    }
}

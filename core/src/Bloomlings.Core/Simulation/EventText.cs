using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Bloomlings.Core.Boards;

namespace Bloomlings.Core.Simulation
{
    /// <summary>
    /// Canonical one-line text for events and a digest over an event stream. Golden replays compare digests, so the
    /// format is part of the determinism contract: change it only together with a reviewed golden regeneration.
    /// </summary>
    public static class EventText
    {
        public static string Format(GameEvent e)
        {
            var sb = new StringBuilder();
            sb.Append('r').Append(e.Round.ToString(CultureInfo.InvariantCulture)).Append(' ');
            switch (e)
            {
                case PodCommitted x:
                    sb.Append("PodCommitted ").Append(x.PodId).Append(" slot=").Append(Int(x.SlotIndex))
                        .Append(" stack=").Append(Int(x.Stack)).Append(" depth=").Append(Int(x.FromDepth));
                    break;
                case MysteryPodRevealed x:
                    sb.Append("MysteryPodRevealed ").Append(x.PodId).Append(' ').Append(x.Variant.Key);
                    break;
                case TileCleared x:
                    sb.Append("TileCleared ").Append(Cell(x.Cell)).Append(' ').Append(x.Variant.Key).Append(" by ").Append(x.PodId)
                        .Append(" route=").Append(Cells(x.RouteFromEntry));
                    break;
                case LayerRevealed x:
                    sb.Append("LayerRevealed ").Append(Cell(x.Cell)).Append(' ').Append(x.NewTopVariant.Key);
                    break;
                case CellOpened x:
                    sb.Append("CellOpened ").Append(Cell(x.Cell));
                    break;
                case MysteryTileRevealed x:
                    sb.Append("MysteryTileRevealed ").Append(Cell(x.Cell)).Append(' ').Append(x.Variant.Key);
                    break;
                case KeyCollected x:
                    sb.Append("KeyCollected ").Append(x.KeyId).Append(' ').Append(Cell(x.Cell));
                    break;
                case LockOpened x:
                    sb.Append("LockOpened ").Append(x.TargetKind.ToString()).Append(' ').Append(x.TargetId);
                    break;
                case SpecialProgressed x:
                    sb.Append("SpecialProgressed ").Append(x.SpecialId).Append(' ').Append(Int(x.Progress)).Append('/').Append(Int(x.Total));
                    break;
                case SpecialTriggered x:
                    sb.Append("SpecialTriggered ").Append(x.SpecialId).Append(" cells=").Append(Cells(x.EffectCells));
                    break;
                case PodCompleted x:
                    sb.Append("PodCompleted ").Append(x.PodId).Append(" slot=").Append(Int(x.SlotIndex));
                    break;
                case SlotFreed x:
                    sb.Append("SlotFreed ").Append(Int(x.SlotIndex));
                    break;
                case ExtraSlotAdded x:
                    sb.Append("ExtraSlotAdded ").Append(Int(x.SlotIndex));
                    break;
                case TrayShuffled x:
                    sb.Append("TrayShuffled");
                    foreach (IReadOnlyList<string> stack in x.Stacks)
                    {
                        sb.Append(" [").Append(string.Join(",", stack)).Append(']');
                    }

                    break;
                case PodReturned x:
                    sb.Append("PodReturned ").Append(x.PodId).Append(" stack=").Append(Int(x.Stack));
                    break;
                case VariantBurst x:
                    sb.Append("VariantBurst ").Append(x.Variant.Key).Append(" cells=").Append(Cells(x.Cells))
                        .Append(" pods=").Append(string.Join(",", x.PodIds));
                    break;
                case LevelWon x:
                    sb.Append("LevelWon boosters=").Append(Int(x.BoostersUsed));
                    break;
                case LevelJammed x:
                    sb.Append("LevelJammed recoveries=").Append(Recoveries(x.EligibleRecoveries));
                    break;
                case LevelStuck x:
                    sb.Append("LevelStuck recoveries=").Append(Recoveries(x.EligibleRecoveries));
                    break;
                default:
                    throw new NotSupportedException($"No canonical text for {e.GetType().Name}.");
            }

            return sb.ToString();
        }

        /// <summary>64-bit FNV-1a over the UTF-8 lines, each terminated by "\n", as 16 lowercase hex digits.</summary>
        public static string Digest(IEnumerable<string> lines)
        {
            const ulong offset = 14695981039346656037UL;
            const ulong prime = 1099511628211UL;
            ulong hash = offset;
            foreach (string line in lines)
            {
                foreach (byte b in Encoding.UTF8.GetBytes(line))
                {
                    hash = unchecked((hash ^ b) * prime);
                }

                hash = unchecked((hash ^ (byte)'\n') * prime);
            }

            return Hex(hash);
        }

        /// <summary>A 64-bit value as 16 lowercase hex digits (state hashes in golden files).</summary>
        public static string Hex(ulong value) => value.ToString("x16", CultureInfo.InvariantCulture);

        private static string Int(int value) => value.ToString(CultureInfo.InvariantCulture);

        private static string Cell(CellPos c) => Int(c.X) + "," + Int(c.Y);

        private static string Cells(IReadOnlyList<CellPos> cells)
        {
            var parts = new string[cells.Count];
            for (int i = 0; i < cells.Count; i++)
            {
                parts[i] = Cell(cells[i]);
            }

            return string.Join(">", parts);
        }

        private static string Recoveries(IReadOnlyList<Recovery> recoveries)
        {
            var parts = new string[recoveries.Count];
            for (int i = 0; i < recoveries.Count; i++)
            {
                parts[i] = recoveries[i].ToString();
            }

            return string.Join(",", parts);
        }
    }
}

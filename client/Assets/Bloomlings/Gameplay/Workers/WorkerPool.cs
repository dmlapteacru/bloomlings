using System.Collections.Generic;
using Bloomlings.Client.Art.Variants;
using Bloomlings.Client.Gameplay.Board;
using Bloomlings.Client.Gameplay.Timeline;
using Bloomlings.Client.Meta.Wardrobe;
using Bloomlings.Core.Boards;
using Bloomlings.Core.Definitions;
using Bloomlings.Core.Variants;
using UnityEngine;

namespace Bloomlings.Client.Gameplay.Workers
{
    /// <summary>
    /// A bounded pool of Bloomling workers (T046, R4). At most <see cref="Capacity"/> are active (60 on low-end
    /// devices); when the pool is saturated the timeline merges walkers, so one sprite may stand for several tiles.
    /// The worker's look comes from its variant's family, tinted with the variant color (doc 12 §2).
    /// </summary>
    public sealed class WorkerPool : MonoBehaviour
    {
        private readonly Stack<BloomlingWorker> _free = new Stack<BloomlingWorker>();
        private readonly List<BloomlingWorker> _all = new List<BloomlingWorker>();
        private BoardView _board = null!;
        private EventTimeline _timeline = null!;
        private VariantVisualCatalog? _visuals;
        private IReadOnlyList<EntryDef> _entries = new EntryDef[0];

        public int Capacity { get; private set; } = 60;

        public int Active => _all.Count - _free.Count;

        /// <summary>What each family wears (the Wardrobe, FR-063); presentation only.</summary>
        public System.Func<Family, Outfit>? Outfits { get; set; }

        public static WorkerPool Create(GameObject host, BoardView board, EventTimeline timeline, VariantVisualCatalog? visuals, int capacity)
        {
            var pool = host.AddComponent<WorkerPool>();
            pool._board = board;
            pool._timeline = timeline;
            pool._visuals = visuals;
            pool.Capacity = capacity;
            timeline.WorkerCapacity = capacity;
            return pool;
        }

        public void SetEntries(IReadOnlyList<EntryDef> entries) => _entries = entries;

        /// <summary>Sends one walker for a batch; it follows the route of the batch's farthest unit.</summary>
        public void Launch(IReadOnlyList<WorkUnit> batch, float travelSeconds)
        {
            if (batch.Count == 0)
            {
                return;
            }

            BloomlingWorker? worker = Acquire();
            if (worker == null)
            {
                return; // Saturated: the tiles still update on arrival; only the sprite is skipped.
            }

            WorkUnit lead = batch[batch.Count - 1];
            VariantVisual visual = _visuals != null ? _visuals.Get(lead.Clear.Variant) : VariantVisualCatalog.Default(lead.Clear.Variant);
            var path = new List<Vector2> { _board.EntryPoint(EntryFor(lead.Clear.RouteFromEntry[0])) };
            foreach (CellPos cell in lead.Clear.RouteFromEntry)
            {
                path.Add(_board.CellCenter(cell));
            }

            worker.Launch(visual, path, _board.CellSize * 0.62f, travelSeconds, Outfits?.Invoke(visual.Family));
        }

        public void Release(BloomlingWorker worker) => _free.Push(worker);

        public void RecallAll()
        {
            _free.Clear();
            foreach (BloomlingWorker worker in _all)
            {
                worker.Recall();
                _free.Push(worker);
            }
        }

        private BloomlingWorker? Acquire()
        {
            if (_free.Count > 0)
            {
                return _free.Pop();
            }

            if (_all.Count >= Capacity)
            {
                return null;
            }

            BloomlingWorker worker = BloomlingWorker.Create(_board.Grid, this, _timeline);
            _all.Add(worker);
            return worker;
        }

        private EntryDef EntryFor(CellPos entryCell)
        {
            foreach (EntryDef entry in _entries)
            {
                if (entry.Cell == entryCell)
                {
                    return entry;
                }
            }

            return _entries.Count > 0 ? _entries[0] : new EntryDef(entryCell, EntrySide.Bottom);
        }
    }
}

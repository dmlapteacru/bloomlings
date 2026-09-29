using System;
using System.Globalization;
using Bloomlings.Core.Boards;
using Bloomlings.Core.Definitions;
using Bloomlings.Core.Simulation;

namespace Bloomlings.Core.Mechanics
{
    /// <summary>
    /// Keys and locks (FR-033, FR-034, FR-039; T099). A key lies over a target tile and is collected, at no extra work,
    /// when that supporting top layer is cleared (<c>KeyCollected</c>). Its paired lock opens at once
    /// (<c>LockOpened</c>): a locked pod becomes selectable, a locked slot becomes free, and a special waiting for the key
    /// triggers in the same round (<see cref="Specials"/>).
    /// </summary>
    internal sealed class KeysAndLocks : IRoundHook
    {
        public static readonly KeysAndLocks Instance = new KeysAndLocks();

        private KeysAndLocks()
        {
        }

        public void OnStart(LevelState state)
        {
        }

        public void BeforeAllocation(RoundContext context)
        {
        }

        public void OnLayerCleared(RoundContext context, int cell, LayerClearResult result, int pod)
        {
            LevelState state = context.State;
            string? keyId = state.Board.KeyAt(cell);
            if (keyId == null)
            {
                return;
            }

            state.Board.RemoveKey(cell);
            state.CollectKey(keyId);
            context.Events.Add(new KeyCollected(context.Round, keyId, state.Board.PosOf(cell)));

            LockDef lockDef = state.Mechanics.LockByKey[Array.BinarySearch(state.KeyIds, keyId, StringComparer.Ordinal)]!;
            if (lockDef.TargetKind == LockTargetKind.Slot)
            {
                state.UnlockSlot(int.Parse(lockDef.TargetId, NumberStyles.None, CultureInfo.InvariantCulture));
            }

            context.Events.Add(new LockOpened(context.Round, lockDef.TargetKind, lockDef.TargetId));
        }

        public void AfterClears(RoundContext context)
        {
        }

        public bool BlocksWin(LevelState state) => false;
    }
}

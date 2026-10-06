using System;
using System.Collections.Generic;

namespace Bloomlings.Core.Tray
{
    /// <summary>
    /// The Source Tray: stacks of pods where only the top pod of each stack is exposed (FR-011). Pods are identified by
    /// their index in the level's pod list. Internally each stack is stored bottom-first so taking the top is O(1) and
    /// the other pods keep their depth from the bottom, which the state hash uses. Taking the top only shortens a
    /// stack, so clones share the stack arrays and copy one only before writing into it (Return, Bloom Burst, Shuffle).
    /// </summary>
    public sealed class SourceTray
    {
        public const int MinStacks = 2;
        public const int MaxStacks = 6;

        // Bottom-first pods of each stack in [0, _count[s]); every array has room for all pods of the level.
        private int[][] _stacks;
        private readonly int[] _count;
        private readonly int[] _stackOf;

        // Bit s: _stacks[s] belongs to this tray alone. _ownsStacks: the outer array does.
        private int _ownedStacks;
        private bool _ownsStacks;

        /// <param name="stacksTopFirst">Pod indexes per stack, top (exposed) first, as in the level definition.</param>
        /// <param name="podCount">Number of pods in the level.</param>
        public SourceTray(IReadOnlyList<IReadOnlyList<int>> stacksTopFirst, int podCount)
        {
            if (stacksTopFirst.Count < MinStacks || stacksTopFirst.Count > MaxStacks)
            {
                throw new ArgumentException($"The tray needs 2–6 stacks, got {stacksTopFirst.Count}.", nameof(stacksTopFirst));
            }

            _stacks = new int[stacksTopFirst.Count][];
            _count = new int[stacksTopFirst.Count];
            _stackOf = new int[podCount];
            for (int i = 0; i < podCount; i++)
            {
                _stackOf[i] = -1;
            }

            for (int s = 0; s < stacksTopFirst.Count; s++)
            {
                IReadOnlyList<int> stack = stacksTopFirst[s];
                var bottomFirst = new int[Math.Max(podCount, stack.Count)];
                int n = 0;
                for (int d = stack.Count - 1; d >= 0; d--)
                {
                    int pod = stack[d];
                    if (pod < 0 || pod >= podCount)
                    {
                        throw new ArgumentException($"Stack {s} holds unknown pod index {pod}.", nameof(stacksTopFirst));
                    }

                    if (_stackOf[pod] >= 0)
                    {
                        throw new ArgumentException($"Pod index {pod} appears twice in the tray.", nameof(stacksTopFirst));
                    }

                    _stackOf[pod] = s;
                    bottomFirst[n++] = pod;
                }

                _stacks[s] = bottomFirst;
                _count[s] = n;
            }

            _ownedStacks = (1 << _stacks.Length) - 1;
            _ownsStacks = true;
        }

        private SourceTray(SourceTray source)
        {
            _stacks = source._stacks;
            _count = (int[])source._count.Clone();
            _stackOf = (int[])source._stackOf.Clone();
            source._ownedStacks = 0;
            source._ownsStacks = false;
            _ownedStacks = 0;
            _ownsStacks = false;
        }

        public int StackCount => _stacks.Length;

        public int PodCount => _stackOf.Length;

        public SourceTray Clone() => new SourceTray(this);

        public int CountIn(int stack) => _count[stack];

        /// <summary>The exposed pod of a stack, or -1 when the stack is empty.</summary>
        public int TopOf(int stack)
        {
            int count = _count[stack];
            return count == 0 ? -1 : _stacks[stack][count - 1];
        }

        /// <summary>The stack currently holding the pod, or -1 when it is not in the tray.</summary>
        public int StackOf(int pod) => _stackOf[pod];

        public bool Contains(int pod) => _stackOf[pod] >= 0;

        public bool IsExposed(int pod)
        {
            int stack = _stackOf[pod];
            return stack >= 0 && TopOf(stack) == pod;
        }

        /// <summary>Position counted from the bottom of the stack (0 = bottom).</summary>
        public int DepthFromBottom(int pod)
        {
            int stack = _stackOf[pod];
            return stack < 0 ? -1 : IndexIn(stack, pod);
        }

        /// <summary>Position counted from the top of the stack (0 = exposed).</summary>
        public int DepthFromTop(int pod)
        {
            int stack = _stackOf[pod];
            return stack < 0 ? -1 : _count[stack] - 1 - IndexIn(stack, pod);
        }

        /// <summary>The pod at a depth of a stack, counted from the top (0 = exposed).</summary>
        internal int PodFromTop(int stack, int depth) => _stacks[stack][_count[stack] - 1 - depth];

        /// <summary>The exposed pods in stack order.</summary>
        public IReadOnlyList<int> Exposed()
        {
            var exposed = new List<int>(_stacks.Length);
            for (int s = 0; s < _stacks.Length; s++)
            {
                int top = TopOf(s);
                if (top >= 0)
                {
                    exposed.Add(top);
                }
            }

            return exposed;
        }

        /// <summary>Removes an exposed pod and returns its stack; the next pod of that stack becomes exposed.</summary>
        public int Take(int pod)
        {
            if (!IsExposed(pod))
            {
                throw new InvalidOperationException($"Pod index {pod} is not exposed.");
            }

            int stack = _stackOf[pod];
            _count[stack]--;
            _stackOf[pod] = -1;
            return stack;
        }

        /// <summary>Removes a pod from any depth of its stack (Bloom Burst, FR-050); the pods above it move down.</summary>
        public void Remove(int pod)
        {
            int stack = _stackOf[pod];
            if (stack < 0)
            {
                throw new InvalidOperationException($"Pod index {pod} is not in the tray.");
            }

            int[] pods = OwnStack(stack);
            int count = _count[stack];
            for (int i = IndexIn(stack, pod); i < count - 1; i++)
            {
                pods[i] = pods[i + 1];
            }

            _count[stack] = count - 1;
            _stackOf[pod] = -1;
        }

        /// <summary>Puts a pod on top of a stack (Return, FR-045).</summary>
        public void PushTop(int stack, int pod)
        {
            if (_stackOf[pod] >= 0)
            {
                throw new InvalidOperationException($"Pod index {pod} is already in the tray.");
            }

            int[] pods = OwnStack(stack);
            pods[_count[stack]++] = pod;
            _stackOf[pod] = stack;
        }

        /// <summary>The pods of a stack, top (exposed) first.</summary>
        public IReadOnlyList<int> StackTopFirst(int stack)
        {
            int[] pods = _stacks[stack];
            int count = _count[stack];
            var result = new int[count];
            for (int i = 0; i < count; i++)
            {
                result[i] = pods[count - 1 - i];
            }

            return result;
        }

        /// <summary>Replaces the whole arrangement (Shuffle, FR-044). Stacks are given top-first.</summary>
        public void Rearrange(IReadOnlyList<IReadOnlyList<int>> stacksTopFirst)
        {
            if (stacksTopFirst.Count != _stacks.Length)
            {
                throw new ArgumentException("Shuffle keeps the number of stacks.", nameof(stacksTopFirst));
            }

            var before = new List<int>();
            for (int s = 0; s < _stacks.Length; s++)
            {
                for (int i = 0; i < _count[s]; i++)
                {
                    before.Add(_stacks[s][i]);
                }

                _count[s] = 0;
            }

            foreach (int pod in before)
            {
                _stackOf[pod] = -1;
            }

            for (int s = 0; s < stacksTopFirst.Count; s++)
            {
                IReadOnlyList<int> stack = stacksTopFirst[s];
                for (int d = stack.Count - 1; d >= 0; d--)
                {
                    int pod = stack[d];
                    if (_stackOf[pod] >= 0 || !before.Contains(pod))
                    {
                        throw new ArgumentException($"Shuffle must place exactly the pods of the tray; pod index {pod} is wrong.", nameof(stacksTopFirst));
                    }

                    int[] pods = OwnStack(s);
                    pods[_count[s]++] = pod;
                    _stackOf[pod] = s;
                }
            }

            foreach (int pod in before)
            {
                if (_stackOf[pod] < 0)
                {
                    throw new ArgumentException($"Shuffle dropped pod index {pod}.", nameof(stacksTopFirst));
                }
            }
        }

        private int IndexIn(int stack, int pod)
        {
            int[] pods = _stacks[stack];
            int count = _count[stack];
            for (int i = 0; i < count; i++)
            {
                if (pods[i] == pod)
                {
                    return i;
                }
            }

            return -1;
        }

        /// <summary>This tray's own copy of a stack's array, made before the first write into it.</summary>
        private int[] OwnStack(int stack)
        {
            if ((_ownedStacks & (1 << stack)) == 0)
            {
                if (!_ownsStacks)
                {
                    _stacks = (int[][])_stacks.Clone();
                    _ownsStacks = true;
                }

                int[] shared = _stacks[stack];
                var own = new int[Math.Max(_stackOf.Length, shared.Length)];
                Array.Copy(shared, own, _count[stack]);
                _stacks[stack] = own;
                _ownedStacks |= 1 << stack;
            }

            return _stacks[stack];
        }
    }
}

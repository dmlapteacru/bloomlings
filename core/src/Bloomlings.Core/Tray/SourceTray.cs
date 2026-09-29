using System;
using System.Collections.Generic;

namespace Bloomlings.Core.Tray
{
    /// <summary>
    /// The Source Tray: stacks of pods where only the top pod of each stack is exposed (FR-011). Pods are identified by
    /// their index in the level's pod list. Internally each stack is stored bottom-first so taking the top is O(1) and
    /// the other pods keep their depth from the bottom, which the state hash uses.
    /// </summary>
    public sealed class SourceTray
    {
        public const int MinStacks = 2;
        public const int MaxStacks = 6;

        private readonly List<int>[] _stacks;
        private readonly int[] _stackOf;

        /// <param name="stacksTopFirst">Pod indexes per stack, top (exposed) first, as in the level definition.</param>
        /// <param name="podCount">Number of pods in the level.</param>
        public SourceTray(IReadOnlyList<IReadOnlyList<int>> stacksTopFirst, int podCount)
        {
            if (stacksTopFirst.Count < MinStacks || stacksTopFirst.Count > MaxStacks)
            {
                throw new ArgumentException($"The tray needs 2–6 stacks, got {stacksTopFirst.Count}.", nameof(stacksTopFirst));
            }

            _stacks = new List<int>[stacksTopFirst.Count];
            _stackOf = new int[podCount];
            for (int i = 0; i < podCount; i++)
            {
                _stackOf[i] = -1;
            }

            for (int s = 0; s < stacksTopFirst.Count; s++)
            {
                IReadOnlyList<int> stack = stacksTopFirst[s];
                var bottomFirst = new List<int>(stack.Count);
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
                    bottomFirst.Add(pod);
                }

                _stacks[s] = bottomFirst;
            }
        }

        private SourceTray(SourceTray source)
        {
            _stacks = new List<int>[source._stacks.Length];
            for (int s = 0; s < _stacks.Length; s++)
            {
                _stacks[s] = new List<int>(source._stacks[s]);
            }

            _stackOf = (int[])source._stackOf.Clone();
        }

        public int StackCount => _stacks.Length;

        public int PodCount => _stackOf.Length;

        public SourceTray Clone() => new SourceTray(this);

        public int CountIn(int stack) => _stacks[stack].Count;

        /// <summary>The exposed pod of a stack, or -1 when the stack is empty.</summary>
        public int TopOf(int stack)
        {
            List<int> list = _stacks[stack];
            return list.Count == 0 ? -1 : list[list.Count - 1];
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
            return stack < 0 ? -1 : _stacks[stack].IndexOf(pod);
        }

        /// <summary>Position counted from the top of the stack (0 = exposed).</summary>
        public int DepthFromTop(int pod)
        {
            int stack = _stackOf[pod];
            return stack < 0 ? -1 : _stacks[stack].Count - 1 - _stacks[stack].IndexOf(pod);
        }

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
            List<int> list = _stacks[stack];
            list.RemoveAt(list.Count - 1);
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

            _stacks[stack].Remove(pod);
            _stackOf[pod] = -1;
        }

        /// <summary>Puts a pod on top of a stack (Return, FR-045).</summary>
        public void PushTop(int stack, int pod)
        {
            if (_stackOf[pod] >= 0)
            {
                throw new InvalidOperationException($"Pod index {pod} is already in the tray.");
            }

            _stacks[stack].Add(pod);
            _stackOf[pod] = stack;
        }

        /// <summary>The pods of a stack, top (exposed) first.</summary>
        public IReadOnlyList<int> StackTopFirst(int stack)
        {
            List<int> list = _stacks[stack];
            var result = new int[list.Count];
            for (int i = 0; i < list.Count; i++)
            {
                result[i] = list[list.Count - 1 - i];
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
            foreach (List<int> stack in _stacks)
            {
                before.AddRange(stack);
                stack.Clear();
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

                    _stacks[s].Add(pod);
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
    }
}
